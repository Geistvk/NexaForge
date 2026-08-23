using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FactoryGame.Engine;
using System.Diagnostics;

namespace NexaForge
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        private BasicEffect _effect;
        private VertexBuffer _cubeVertexBuffer;
        private IndexBuffer _cubeIndexBuffer;

        protected SpriteBatch _spriteBatch;
        protected SpriteFont _font;
        protected Texture2D _pixel;

        private const int ToolbarButtonSize = 48;
        private const int ToolbarMargin = 10;
        protected const int TopBarHeight = 40;

        private readonly OrbitCamera _camera = new();
        private readonly VoxelGrid _grid = new(24, 24, 2f);
        private readonly List<Building> _buildings = new();

        protected BuildingType _selectedType = BuildingType.Miner;
        private MouseState _prevMouse;
        private KeyboardState _prevKeyboard;

        protected float _totalOreInStorage;

        private int _hoveredGridX = -1;
        private int _hoveredGridZ = -1;
        private bool _hasHoveredCell;
        private Building _hoveredBuilding;
        private Building _highlightedBuilding;

        private BuildingsGui _buildingsGui;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "NexaForge";
        }

        protected override void Initialize()
        {
            _camera.Target = Vector3.Zero;

            // Prozedurale Weltgenerierung: Seed fest oder z.B. Environment.TickCount für
            // jedes Mal eine andere Welt
            _grid.GenerateOreDeposits(seed: 12345);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _effect = new BasicEffect(GraphicsDevice)
            {
                VertexColorEnabled = false,
                LightingEnabled = false
            };

            CreateCubeGeometry();

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("DefaultFont");

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _buildingsGui = new BuildingsGui(_spriteBatch, _font, _pixel, _grid);
        }

        private void CreateCubeGeometry()
        {
            // Ein Einheitswürfel (Grundfläche 1x1, Höhe 1), wird beim Zeichnen skaliert/eingefärbt
            var positions = new[]
            {
                new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f),
                new Vector3(0.5f, 1, -0.5f),  new Vector3(-0.5f, 1, -0.5f),
                new Vector3(-0.5f, 0, 0.5f),  new Vector3(0.5f, 0, 0.5f),
                new Vector3(0.5f, 1, 0.5f),   new Vector3(-0.5f, 1, 0.5f)
            };

            var vertices = positions.Select(p => new VertexPositionColor(p, Color.White)).ToArray();

            short[] indices =
            {
                0,1,2, 0,2,3, // vorne
                1,5,6, 1,6,2, // rechts
                5,4,7, 5,7,6, // hinten
                4,0,3, 4,3,7, // links
                3,2,6, 3,6,7, // oben
                4,5,1, 4,1,0  // unten
            };

            _cubeVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
            _cubeVertexBuffer.SetData(vertices);

            _cubeIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
            _cubeIndexBuffer.SetData(indices);
        }

        private void ToggleHighlight(Building building)
        {
            if (building == null)
                return;

            if (_highlightedBuilding == building)
            {
                _highlightedBuilding = null;
            }
            else
            {
                _highlightedBuilding = building;
            }
        }

        private void HandleMouseHover()
        {
            var mouse = Mouse.GetState();

            _hasHoveredCell = false;
            _hoveredGridX = -1;
            _hoveredGridZ = -1;
            _hoveredBuilding = null;

            if (IsMouseOverUI(mouse.X, mouse.Y))
                return;

            // Zuerst prüfen, ob ein Gebäude unter der Maus liegt
            if (TryGetHoveredBuilding(mouse.X, mouse.Y, out Building building))
            {
                _hoveredBuilding = building;

                // Die Grid-Zelle des Gebäudes ebenfalls speichern
                _hoveredGridX = building.GridX;
                _hoveredGridZ = building.GridZ;
                _hasHoveredCell = true;

                return;
            }

            // Wenn kein Gebäude getroffen wurde, Boden prüfen
            if (TryGetGroundCell(mouse.X, mouse.Y, out int gx, out int gz))
            {
                _hoveredGridX = gx;
                _hoveredGridZ = gz;
                _hasHoveredCell = true;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _camera.HandleInput(gameTime);
            HandleBuildingSelection();
            HandleMouseHover();
            HandlePlacement();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach (var b in _buildings) b.Tick(dt);
            SimulateFactory(dt);

            base.Update(gameTime);
        }

        private bool canRotateBuilding(BuildingType buildingType)
        {
            return buildingType == BuildingType.Belt;
        }

        private void HandleBuildingSelection()
        {
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();
            if (IsPressed(keyboard, Keys.D0)) _selectedType = BuildingType.None;
            if (IsPressed(keyboard, Keys.D1)) _selectedType = BuildingType.Miner;
            if (IsPressed(keyboard, Keys.D2)) _selectedType = BuildingType.Belt;
            if (IsPressed(keyboard, Keys.D3)) _selectedType = BuildingType.Storage;

            if (IsPressed(keyboard, Keys.R) &&
                canRotateBuilding(_selectedType))
            { 
                Debug.WriteLine("Rotating the belt 90 degrees clockwise");
            }

            if (IsPressed(keyboard, Keys.E))
            {
                if (_highlightedBuilding != null)
                    Debug.WriteLine($"Highlighting building at ({_highlightedBuilding.GridX}, {_highlightedBuilding.GridZ})");

                if (TryGetGroundCell(mouse.X, mouse.Y, out int gx, out int gz) &&
                    _grid.Get(gx, gz) is Building existing)
                {
                    ToggleHighlight(existing);
                    if (!_buildingsGui.IsOpen /*&& _highlightedBuilding == existing*/)
                    {
                        _buildingsGui.Open(existing);
                    }
                    /*else if (_buildingsGui.IsOpen && _highlightedBuilding != existing)
                    {
                        _buildingsGui.Close();
                        _buildingsGui.Open(existing);
                        _highlightedBuilding = existing;
                    }*/
                    else
                        _buildingsGui.Close();
                } 
                else if (_highlightedBuilding != null)
                {
                    _highlightedBuilding = null;
                    _buildingsGui.Close();
                }
            }

            _prevKeyboard = keyboard;
        }

        private bool IsPressed(KeyboardState current, Keys key) =>
            current.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);

        private void HandlePlacement()
        {
            var mouse = Mouse.GetState();
            bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            bool rightClicked = mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;

            // Klick auf einen der Auswahl-Buttons unten links -> nur Auswahl ändern, nicht in der Welt bauen
            if (leftClicked && TryHandleToolbarClick(mouse.X, mouse.Y))
            {
                _prevMouse = mouse;
                return;
            }

            // Maus ist über der GUI (Infoleiste/Buttons) -> keine Platzierung/Entfernung auslösen
            if (IsMouseOverUI(mouse.X, mouse.Y))
            {
                _prevMouse = mouse;
                return;
            }

            // Linksklick = platzieren
            if (leftClicked)
            {
                if (TryGetGroundCell(mouse.X, mouse.Y, out int gx, out int gz))
                {
                    if (!_grid.IsOccupied(gx, gz))
                    {
                        var worldPos = _grid.CellToWorld(gx, gz);
                        Building building = _selectedType switch
                        {
                            BuildingType.Miner => new Miner(gx, gz, worldPos),
                            BuildingType.Belt => new Belt(gx, gz, worldPos),
                            BuildingType.Storage => new Storage(gx, gz, worldPos),
                            BuildingType.None => null,
                            _ => null
                        };

                        if (building != null)
                        {
                            _buildings.Add(building);
                            _grid.Place(gx, gz, building);
                        }
                    }
                    else if (_grid.Get(gx, gz) is Building existing)
                    {
                        ToggleHighlight(existing);
                        if (!_buildingsGui.IsOpen)
                            _buildingsGui.Open(existing);
                        else
                            _buildingsGui.Close();
                    }
                }
            }

            // Rechtsklick = entfernen
            if (rightClicked)
            {
                if (TryGetGroundCell(mouse.X, mouse.Y, out int gx, out int gz) && _grid.Get(gx, gz) is Building existing)
                {
                    _highlightedBuilding = null;
                    _buildings.Remove(existing);
                    _grid.Remove(gx, gz);
                }
            }

            _prevMouse = mouse;
        }

        private bool IsMouseOverUI(int mouseX, int mouseY)
        {
            if (mouseY <= TopBarHeight) return true;

            int y = GraphicsDevice.Viewport.Height - ToolbarButtonSize - ToolbarMargin;
            int width = 3 * (ToolbarButtonSize + ToolbarMargin);
            if (mouseY >= y && mouseY <= y + ToolbarButtonSize && mouseX >= ToolbarMargin && mouseX <= ToolbarMargin + width)
                return true;

            return false;
        }

        private bool TryHandleToolbarClick(int mouseX, int mouseY)
        {
            int y = GraphicsDevice.Viewport.Height - ToolbarButtonSize - ToolbarMargin;
            if (mouseY < y || mouseY > y + ToolbarButtonSize) return false;

            for (int i = 0; i < 3; i++)
            {
                int x = ToolbarMargin + i * (ToolbarButtonSize + ToolbarMargin);
                if (mouseX >= x && mouseX <= x + ToolbarButtonSize)
                {
                    _selectedType = i switch { 0 => BuildingType.Miner, 1 => BuildingType.Belt, _ => BuildingType.Storage };
                    return true;
                }
            }

            return false;
        }

        // Schießt einen Strahl von der Maus in die Szene und schneidet ihn mit der Bodenebene (y=0)
        private bool TryGetGroundCell(int screenX, int screenY, out int gx, out int gz)
        {
            gx = gz = 0;
            var viewport = GraphicsDevice.Viewport;
            var projection = _camera.GetProjection(GraphicsDevice);

            var near = viewport.Unproject(new Vector3(screenX, screenY, 0f), projection, _camera.View, Matrix.Identity);
            var far = viewport.Unproject(new Vector3(screenX, screenY, 1f), projection, _camera.View, Matrix.Identity);

            var direction = far - near;
            if (Math.Abs(direction.Y) < 1e-6f) return false;

            float t = -near.Y / direction.Y;
            if (t < 0) return false;

            var hit = near + direction * t;
            return _grid.WorldToCell(hit, out gx, out gz);
        }

        private bool TryGetHoveredBuilding(
            int screenX,
            int screenY,
            out Building hoveredBuilding)
        {
            hoveredBuilding = null;

            var viewport = GraphicsDevice.Viewport;
            var projection = _camera.GetProjection(GraphicsDevice);

            Vector3 near = viewport.Unproject(
                new Vector3(screenX, screenY, 0f),
                projection,
                _camera.View,
                Matrix.Identity);

            Vector3 far = viewport.Unproject(
                new Vector3(screenX, screenY, 1f),
                projection,
                _camera.View,
                Matrix.Identity);

            Vector3 direction = Vector3.Normalize(far - near);

            float closestDistance = float.MaxValue;

            foreach (var building in _buildings)
            {
                float height = GetHeight(building);

                Vector3 center = building.Transform.Position;

                float halfX = _grid.CellSize * 0.45f;
                float halfZ = _grid.CellSize * 0.45f;

                Vector3 min = new Vector3(
                    center.X - halfX,
                    center.Y,
                    center.Z - halfZ);

                Vector3 max = new Vector3(
                    center.X + halfX,
                    center.Y + height,
                    center.Z + halfZ);

                if (RayIntersectsBox(
                    near,
                    direction,
                    min,
                    max,
                    out float distance))
                {
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        hoveredBuilding = building;
                    }
                }
            }

            return hoveredBuilding != null;
        }

        private bool RayIntersectsBox(
            Vector3 origin,
            Vector3 direction,
            Vector3 min,
            Vector3 max,
            out float distance)
        {
            distance = 0f;

            float tMin = 0f;
            float tMax = float.MaxValue;

            // X
            if (Math.Abs(direction.X) < 0.000001f)
            {
                if (origin.X < min.X || origin.X > max.X)
                    return false;
            }
            else
            {
                float invD = 1f / direction.X;

                float t1 = (min.X - origin.X) * invD;
                float t2 = (max.X - origin.X) * invD;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                tMin = Math.Max(tMin, t1);
                tMax = Math.Min(tMax, t2);

                if (tMin > tMax)
                    return false;
            }

            // Y
            if (Math.Abs(direction.Y) < 0.000001f)
            {
                if (origin.Y < min.Y || origin.Y > max.Y)
                    return false;
            }
            else
            {
                float invD = 1f / direction.Y;

                float t1 = (min.Y - origin.Y) * invD;
                float t2 = (max.Y - origin.Y) * invD;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                tMin = Math.Max(tMin, t1);
                tMax = Math.Min(tMax, t2);

                if (tMin > tMax)
                    return false;
            }

            // Z
            if (Math.Abs(direction.Z) < 0.000001f)
            {
                if (origin.Z < min.Z || origin.Z > max.Z)
                    return false;
            }
            else
            {
                float invD = 1f / direction.Z;

                float t1 = (min.Z - origin.Z) * invD;
                float t2 = (max.Z - origin.Z) * invD;

                if (t1 > t2)
                {
                    float temp = t1;
                    t1 = t2;
                    t2 = temp;
                }

                tMin = Math.Max(tMin, t1);
                tMax = Math.Min(tMax, t2);

                if (tMin > tMax)
                    return false;
            }

            distance = tMin;
            return true;
        }

        // Vereinfachte Fabriksimulation: Miner -> angrenzendes Band (Richtung +X) -> weitere Bänder -> Lager
        private void SimulateFactory(float dt)
        {
            foreach (var building in _buildings)
            {
                if (building is Miner miner)
                {
                    // Erz aus dem prozeduralen Vorkommen unter dem Miner fördern (endliche Ressource!)
                    float wanted = Math.Min(miner.MineRatePerSecond * dt, miner.BufferCapacity - miner.OreBuffer);
                    float mined = _grid.ExtractOre(miner.GridX, miner.GridZ, wanted, miner);
                    miner.AddOre(mined);

                    if (TryGetNeighbor(miner.GridX, miner.GridZ, new Vector3(1, 0, 0), out Building target) && target is Belt belt)
                    {
                        float space = belt.Capacity - belt.ItemAmount;
                        float moved = miner.Extract(Math.Min(space, miner.MineRatePerSecond * dt * 2f));
                        belt.ItemAmount += moved;
                    }
                }
                else if (building is Belt sourceBelt && sourceBelt.ItemAmount > 0)
                {
                    if (TryGetNeighbor(sourceBelt.GridX, sourceBelt.GridZ, sourceBelt.Direction, out Building target))
                    {
                        float amount = Math.Min(sourceBelt.ItemAmount, sourceBelt.Speed * dt);

                        if (target is Belt nextBelt)
                        {
                            float space = nextBelt.Capacity - nextBelt.ItemAmount;
                            float moved = Math.Min(amount, space);
                            nextBelt.ItemAmount += moved;
                            sourceBelt.ItemAmount -= moved;
                        }
                        else if (target is Storage storage)
                        {
                            float accepted = storage.Deposit(amount);
                            sourceBelt.ItemAmount -= accepted;
                        }
                    }
                }
            }

            _totalOreInStorage = _buildings.OfType<Storage>().Sum(s => s.Stored);
        }

        private bool TryGetNeighbor(int x, int z, Vector3 direction, out Building neighbor)
        {
            int nx = x + (int)Math.Round(direction.X);
            int nz = z + (int)Math.Round(direction.Z);
            neighbor = _grid.Get(nx, nz) as Building;
            return neighbor != null;
        }

        private void DrawHoveredCell()
        {
            if (_highlightedBuilding != null)
            {
                DrawBuildingHighlight(_highlightedBuilding);
            }

            if (!_hasHoveredCell)
                return;

            // Building
            if (_hoveredBuilding != null)
            {
                DrawBuildingHighlight(_hoveredBuilding);
                return;
            }

            // No Building
            Vector3 center = _grid.CellToWorld(
                _hoveredGridX,
                _hoveredGridZ);

            float s = _grid.CellSize * 0.94f;
            float thickness = 0.06f;
            float height = 0.04f;

            Color highlight = Color.Aqua;

            DrawCube(
                center + new Vector3(0, height, -s / 2f),
                new Vector3(s, height, thickness),
                highlight);

            DrawCube(
                center + new Vector3(0, height, s / 2f),
                new Vector3(s, height, thickness),
                highlight);

            DrawCube(
                center + new Vector3(-s / 2f, height, 0),
                new Vector3(thickness, height, s),
                highlight);

            DrawCube(
                center + new Vector3(s / 2f, height, 0),
                new Vector3(thickness, height, s),
                highlight);
        }

        private void DrawBuildingHighlight(Building building)
        {
            Vector3 center = building.Transform.Position;

            float s = _grid.CellSize * 0.94f;
            float height = GetHeight(building);

            float thickness = 0.06f;

            Color highlight = Color.Aqua;

            // Bottom
            DrawCube(
                center + new Vector3(0f, thickness / 2f, -s / 2f),
                new Vector3(s, thickness, thickness),
                highlight);

            DrawCube(
                center + new Vector3(0f, thickness / 2f, s / 2f),
                new Vector3(s, thickness, thickness),
                highlight);

            DrawCube(
                center + new Vector3(-s / 2f, thickness / 2f, 0f),
                new Vector3(thickness, thickness, s),
                highlight);

            DrawCube(
                center + new Vector3(s / 2f, thickness / 2f, 0f),
                new Vector3(thickness, thickness, s),
                highlight);


            // Top
            DrawCube(
                center + new Vector3(0f, height, -s / 2f),
                new Vector3(s, thickness, thickness),
                highlight);

            DrawCube(
                center + new Vector3(0f, height, s / 2f),
                new Vector3(s, thickness, thickness),
                highlight);

            DrawCube(
                center + new Vector3(-s / 2f, height, 0f),
                new Vector3(thickness, thickness, s),
                highlight);

            DrawCube(
                center + new Vector3(s / 2f, height, 0f),
                new Vector3(thickness, thickness, s),
                highlight);


            // Vertical
            DrawCube(
                center + new Vector3(-s / 2f, 0, -s / 2f),
                new Vector3(thickness, height, thickness),
                highlight);

            DrawCube(
                center + new Vector3(s / 2f, 0, -s / 2f),
                new Vector3(thickness, height, thickness),
                highlight);

            DrawCube(
                center + new Vector3(-s / 2f, 0, s / 2f),
                new Vector3(thickness, height, thickness),
                highlight);

            DrawCube(
                center + new Vector3(s / 2f, 0, s / 2f),
                new Vector3(thickness, height, thickness),
                highlight);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            _effect.View = _camera.View;
            _effect.Projection = _camera.GetProjection(GraphicsDevice);

            DrawGridFloor();
            DrawOreDeposits();

            foreach (var b in _buildings)
            {
                var size = new Vector3(
                    _grid.CellSize * 0.9f,
                    GetHeight(b),
                    _grid.CellSize * 0.9f
                );

                DrawCube(b.Transform.Position, size, b.Color);
            }

            DrawHoveredCell();

            DrawUI();

            _buildingsGui.Draw(GraphicsDevice);

            base.Draw(gameTime);
        }

        // Zeichnet die prozedural erzeugten Erzflecken als flache, eingefärbte Flächen auf dem Boden
        private void DrawOreDeposits()
        {
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int z = 0; z < _grid.Depth; z++)
                {
                    float ore = _grid.GetOre(x, z);
                    if (ore <= 0f) continue;

                    float intensity = MathHelper.Clamp(ore / 500f, 0.15f, 1f);
                    var color = Color.Lerp(Color.DarkGreen, Color.DarkOrange, intensity);
                    var pos = _grid.CellToWorld(x, z);
                    var size = new Vector3(_grid.CellSize * 0.95f, 0.06f, _grid.CellSize * 0.95f);
                    DrawCube(pos, size, color);
                }
            }
        }

        private void DrawUI()
        {
            _spriteBatch.Begin();

            //Topbar
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, TopBarHeight), Color.Black * 0.6f);
            string info = $"Lager: {_totalOreInStorage:0.0} Erz    |    Auswahl: {_selectedType}    |    Rechtsklick = entfernen, ESC = beenden";
            _spriteBatch.DrawString(_font, info, new Vector2(10, 10), Color.White);

            //Building Btns
            DrawToolbarButton(0, BuildingType.Miner, Color.OrangeRed, "1");
            DrawToolbarButton(1, BuildingType.Belt, Color.SlateGray, "2");
            DrawToolbarButton(2, BuildingType.Storage, Color.SaddleBrown, "3");

            _spriteBatch.End();

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
        }

        private void DrawToolbarButton(int index, BuildingType type, Color color, string key)
        {
            int x = ToolbarMargin + index * (ToolbarButtonSize + ToolbarMargin);
            int y = GraphicsDevice.Viewport.Height - ToolbarButtonSize - ToolbarMargin;
            var rect = new Rectangle(x, y, ToolbarButtonSize, ToolbarButtonSize);

            _spriteBatch.Draw(_pixel, rect, color);

            if (_selectedType == type)
            {
                const int border = 3;
                _spriteBatch.Draw(_pixel, new Rectangle(rect.X - border, rect.Y - border, rect.Width + border * 2, border), Color.White);
                _spriteBatch.Draw(_pixel, new Rectangle(rect.X - border, rect.Bottom, rect.Width + border * 2, border), Color.White);
                _spriteBatch.Draw(_pixel, new Rectangle(rect.X - border, rect.Y - border, border, rect.Height + border * 2), Color.White);
                _spriteBatch.Draw(_pixel, new Rectangle(rect.Right, rect.Y - border, border, rect.Height + border * 2), Color.White);
            }

            _spriteBatch.DrawString(_font, key, new Vector2(x + 4, y + 4), Color.White);
        }

        private float GetHeight(Building b) => b is Belt ? 0.3f : 1.5f;

        private void DrawGridFloor()
        {
            var size = new Vector3(_grid.Width * _grid.CellSize, 0.05f, _grid.Depth * _grid.CellSize);
            DrawCube(new Vector3(0, -0.05f, 0), size, Color.DarkGreen);
        }

        private void DrawCube(Vector3 position, Vector3 size, Color color)
        {
            _effect.World = Matrix.CreateScale(size) * Matrix.CreateTranslation(position);
            _effect.DiffuseColor = color.ToVector3();

            GraphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
            GraphicsDevice.Indices = _cubeIndexBuffer;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
            }
        }
    }
}
