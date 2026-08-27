using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FactoryGame.Engine;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace NexaForge
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        protected BasicEffect _effect;
        protected VertexBuffer _cubeVertexBuffer;
        protected IndexBuffer _cubeIndexBuffer;

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
        protected float _oreInMiner;
        protected float _oreInStorage;
        protected float _oreInBelt;
        protected float _totalOreProduction;
        private float _previousOreAmount;
        private double _productionTimer;

        private MouseState _previousMouseState;

        private Building _previewBuilding;
        private int _previewGridX = -1;
        private int _previewGridZ = -1;

        private int _hoveredGridX = -1;
        private int _hoveredGridZ = -1;
        private bool _hasHoveredCell;
        private Building _hoveredBuilding;
        private Building _highlightedBuilding;

        private BuildingsGui _buildingsGui;

        private float _buildRot = 0f;

        private Dictionary<string, Keys> _Keys = new();

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

            _grid.GenerateOreDeposits(seed: 12345);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _Keys = new Dictionary<string, Keys> {
                { "Close", Keys.Escape},
                { "SelNone", Keys.D0 },
                { "SelMiner", Keys.D1 },
                { "SelBelt", Keys.D2 },
                { "SelStorage", Keys.D3 },
                { "RotClock", Keys.R },
                { "RotCountClock", Keys.F },
                { "ToggleHover", Keys.LeftShift },
                { "OpenBuildInfo", Keys.E }
            };

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
                0,1,2, 0,2,3, // Front
                1,5,6, 1,6,2, // Right
                5,4,7, 5,7,6, // Back
                4,0,3, 4,3,7, // Left
                3,2,6, 3,6,7, // Top
                4,5,1, 4,1,0  // Down
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

            if (TryGetHoveredBuilding(mouse.X, mouse.Y, out Building building))
            {
                _hoveredBuilding = building;

                _hoveredGridX = building.GridX;
                _hoveredGridZ = building.GridZ;
                _hasHoveredCell = true;

                return;
            }

            if (TryGetGroundCell(mouse.X, mouse.Y, out int gx, out int gz))
            {
                _hoveredGridX = gx;
                _hoveredGridZ = gz;
                _hasHoveredCell = true;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(_Keys["Close"]))
                Exit();

            _camera.HandleInput(gameTime);
            HandleBuildingSelection();
            HandleMouseHover();
            HandlePlacement();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach (var b in _buildings) b.Tick(dt);
            SimulateFactory(dt);

            UpdateStatsDropdown();
            UpdateBuildingPreview();

            base.Update(gameTime);
        }

        private void UpdateStatsDropdown()
        {
            UpdateStatsOptions();
            MouseState mouse = Mouse.GetState();

            bool clicked =
                mouse.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released;

            string selectedText = _statsOptions[_statsSelectedIndex];
            Vector2 textSize = _font.MeasureString(selectedText);

            Rectangle button = new Rectangle(
                10,
                8,
                (int)textSize.X + 45,
                (int)textSize.Y + 14
            );

            if (clicked && button.Contains(mouse.Position))
            {
                _statsDropdownOpen = !_statsDropdownOpen;
            }

            if (_statsDropdownOpen)
            {
                int maxTextWidth = 0;
                int itemHeight = 0;

                for (int i = 0; i < _statsOptions.Length; i++)
                {
                    Vector2 size =
                        _font.MeasureString(
                            _statsOptions[i]
                        );

                    maxTextWidth = Math.Max(
                        maxTextWidth,
                        (int)size.X
                    );

                    itemHeight = Math.Max(
                        itemHeight,
                        (int)size.Y + 16
                    );
                }

                int menuWidth =
                    maxTextWidth + 24;

                int menuHeight =
                    _statsOptions.Length * itemHeight +
                    (_statsOptions.Length - 1) * 2;

                int menuX = button.X;
                int menuY = button.Bottom + 4;

                if (menuX + menuWidth >
                    GraphicsDevice.Viewport.Width)
                {
                    menuX =
                        GraphicsDevice.Viewport.Width -
                        menuWidth - 5;
                }

                if (menuY + menuHeight >
                    GraphicsDevice.Viewport.Height)
                {
                    menuY =
                        button.Y -
                        menuHeight - 4;
                }

                for (int i = 0; i < _statsOptions.Length; i++)
                {
                    Rectangle item = new Rectangle(
                        menuX,
                        menuY + i * (itemHeight + 2),
                        menuWidth,
                        itemHeight
                    );

                    if (clicked && item.Contains(mouse.Position))
                    {
                        _statsSelectedIndex = i;
                        _statsDropdownOpen = false;
                        break;
                    }
                }

                Rectangle menu = new Rectangle(
                    menuX,
                    menuY,
                    menuWidth,
                    menuHeight
                );

                if (clicked &&
                    !button.Contains(mouse.Position) &&
                    !menu.Contains(mouse.Position))
                {
                    _statsDropdownOpen = false;
                }
            }

            _previousMouseState = mouse;
        }

        private void UpdateBuildingPreview()
        {
            var mouse = Mouse.GetState();

            if (_selectedType == BuildingType.None ||
                IsMouseOverUI(mouse.X, mouse.Y))
            {
                _previewBuilding = null;
                _previewGridX = -1;
                _previewGridZ = -1;
                return;
            }

            if (!TryGetGroundCell(
                mouse.X,
                mouse.Y,
                out int gx,
                out int gz))
            {
                _previewBuilding = null;
                return;
            }

            if (_grid.IsOccupied(gx, gz))
            {
                _previewBuilding = null;
                return;
            }

            if (_previewBuilding == null ||
                _previewGridX != gx ||
                _previewGridZ != gz ||
                _previewBuilding.Type != _selectedType)
            {
                var worldPos = _grid.CellToWorld(gx, gz);

                _previewBuilding = _selectedType switch
                {
                    BuildingType.Miner => new Miner(gx, gz, worldPos),
                    BuildingType.Belt => new Belt(gx, gz, worldPos),
                    BuildingType.Storage => new Storage(gx, gz, worldPos),
                    _ => null
                };

                _previewGridX = gx;
                _previewGridZ = gz;
            }
        }

        private void toggleBuildingGui(Building existing) {
            if (!_buildingsGui.IsOpen && _highlightedBuilding != existing)
            {
                _buildingsGui.Open(existing);
            }
            else if (_buildingsGui.IsOpen && _highlightedBuilding != existing)
            {
                _buildingsGui.Close();
                _buildingsGui.Open(existing);
            }
            else if (_highlightedBuilding != existing)
                _buildingsGui.Close();

            ToggleHighlight(existing);
        }

        private void HandleBuildingSelection()
        {
            var b = _previewBuilding;
            var keyboard = Keyboard.GetState();
            var mouse = Mouse.GetState();

            if (IsPressed(keyboard, _Keys["SelNone"]))    _selectedType = BuildingType.None;
            if (IsPressed(keyboard, _Keys["SelMiner"]))   _selectedType = BuildingType.Miner;
            if (IsPressed(keyboard, _Keys["SelBelt"]))    _selectedType = BuildingType.Belt;
            if (IsPressed(keyboard, _Keys["SelStorage"])) _selectedType = BuildingType.Storage;

            if (IsPressed(keyboard, _Keys["SelNone"]) ||
                IsPressed(keyboard, _Keys["SelMiner"]) ||
                IsPressed(keyboard, _Keys["SelBelt"]) ||
                IsPressed(keyboard, _Keys["SelStorage"]))
            {
                _buildRot = 0;
            }

            if (IsPressed(keyboard, _Keys["RotClock"]) ||
                IsPressed(keyboard, _Keys["RotCountClock"]))
            {
                _buildRot += IsPressed(keyboard, _Keys["RotCountClock"]) ? 90f : -90f;
                
                if (b != null)
                    b.setRot(_buildRot);
            }

            if (keyboard.IsKeyDown(_Keys["ToggleHover"]))
            {
                //Debug.WriteLine($"Highlight: {(_highlightedBuilding == null ? "None" : _highlightedBuilding.Type.ToString())}"); 

                if (TryGetGroundCell(mouse.X, mouse.Y, out int gx, out int gz) &&
                    _grid.Get(gx, gz) is Building existing)
                {
                    toggleBuildingGui(existing);
                }
                else if (_highlightedBuilding != null)
                {
                    _buildingsGui.Close();
                    ToggleHighlight(_highlightedBuilding);
                }
                else {
                    _buildingsGui.Close();
                    ToggleHighlight(_highlightedBuilding);
                }
            }

            if (IsPressed(keyboard, _Keys["OpenBuildInfo"]))
            {
                if (TryGetGroundCell(mouse.X, mouse.Y, out int gx, out int gz) &&
                    _grid.Get(gx, gz) is Building existing)
                {
                    toggleBuildingGui(existing);
                } 
                else if (_highlightedBuilding != null)
                {
                    _highlightedBuilding = null;
                    _buildingsGui.Close();
                }
                else
                {
                    _buildingsGui.Close();
                    ToggleHighlight(_highlightedBuilding);
                }
            }

            _prevKeyboard = keyboard;
        }

        private void DrawBuildingPreview()
        {
            if (_previewBuilding == null)
                return;

            var b = _previewBuilding;

            var size = new Vector3(
                _grid.CellSize * 0.9f,
                _grid.CellSize * 0.9f,
                _grid.CellSize * 0.9f
            );

            /*var size = new Vector3(
                _grid.CellSize * 0.9f,
                GetHeight(b),
                _grid.CellSize * 0.9f
            );

            DrawCube(b.Transform.Position, size, b.Color);*/

            b.setRot(_buildRot);

            DrawModel(
                Content.Load<Model>(b.Model),
                size,
                b,
                true
            );
        }

        private void DrawCubePreview(
            Vector3 position,
            Vector3 size,
            Color color)
        {
            var effect = new BasicEffect(GraphicsDevice);

            effect.World =
                Matrix.CreateScale(size) *
                Matrix.CreateTranslation(position);

            effect.View = _camera.View;
            effect.Projection = _camera.GetProjection(GraphicsDevice);
            effect.DiffuseColor = color.ToVector3();
            effect.Alpha = 0.4f;
            effect.LightingEnabled = true;
            effect.EnableDefaultLighting();

            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                DrawCube(position, size, color);
            }

            effect.Dispose();
        }

        private bool IsPressed(KeyboardState current, Keys key) =>
            current.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);

        private void HandlePlacement()
        {
            var mouse = Mouse.GetState();

            bool leftClicked =
                mouse.LeftButton == ButtonState.Pressed &&
                _prevMouse.LeftButton == ButtonState.Released;

            bool rightClicked =
                mouse.RightButton == ButtonState.Pressed &&
                _prevMouse.RightButton == ButtonState.Released;

            if (leftClicked && TryHandleToolbarClick(mouse.X, mouse.Y))
            {
                _prevMouse = mouse;
                return;
            }

            if (IsMouseOverUI(mouse.X, mouse.Y))
            {
                _prevMouse = mouse;
                return;
            }

            UpdateBuildingPreview();

            // Left Click = Build Building
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
                            building.setRot(_buildRot);
                            _buildings.Add(building);
                            _grid.Place(gx, gz, building);

                            _previewBuilding = null;
                            _previewGridX = -1;
                            _previewGridZ = -1;
                        }
                    }
                    else if (_grid.Get(gx, gz) is Building existing)
                    {
                        toggleBuildingGui(existing);
                    }
                }
            }

            // Right Click = Delete Building
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

        private void UpdateOreStatistics(float dt)
        {
            _oreInBelt = _buildings.OfType<Belt>().Sum(b => b.ItemAmount);
            _oreInStorage = _buildings.OfType<Storage>().Sum(s => s.Stored);
            _oreInMiner = _buildings.OfType<Miner>().Sum(m => m.OreBuffer);
            _totalOreInStorage = _oreInBelt + _oreInStorage + _oreInMiner;

            _productionTimer += dt;

            if (_productionTimer >= 60.0)
            {
                float currentOre = _totalOreInStorage;

                _totalOreProduction = currentOre - _previousOreAmount;

                _previousOreAmount = currentOre;
                _productionTimer = 0;
            }

            _totalOreProduction = _buildings.OfType<Miner>().Sum(m => m.getRate(dt, _grid));
        }

        private void SimulateFactory(float dt)
        {
            foreach (var building in _buildings)
            {
                if (building is Miner miner)
                {
                    float wanted = Math.Min(miner.MineRatePerSecond * dt, miner.BufferCapacity - miner.OreBuffer);
                    float mined = _grid.ExtractOre(miner.GridX, miner.GridZ, wanted, miner);
                    miner.AddOre(mined);

                    if (TryGetNeighbor(miner.GridX, miner.GridZ, new Vector3(1, 0, 0), out Building target) && target is Belt belt)
                    {
                        belt.mineItem(dt, miner);
                    }
                    else if (TryGetNeighbor(miner.GridX, miner.GridZ, new Vector3(1, 0, 0), out Building neighbor) && neighbor is Storage storage)
                    {
                        storage.mineItem(dt, miner);
                    }
                }
                else if (building is Belt sourceBelt && sourceBelt.ItemAmount > 0)
                {
                    if (TryGetNeighbor(sourceBelt.GridX, sourceBelt.GridZ, sourceBelt.Direction, out Building target))
                    {
                        if (target is Belt nextBelt)
                        {
                            sourceBelt.moveItem(dt, nextBelt);
                        }
                        else if (target is Storage storage)
                        {
                            sourceBelt.storeItem(dt, storage);
                        }
                    }
                }
            }

            UpdateOreStatistics(dt);
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

            float s = (_hoveredBuilding is Storage storage) ? _grid.CellSize * 2f : _grid.CellSize * 0.94f;
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
                /*var size = new Vector3(
                    _grid.CellSize * 0.9f,
                    GetHeight(b),
                    _grid.CellSize * 0.9f
                );

                DrawCube(b.Transform.Position, size, b.Color);*/
                var size = new Vector3(
                    _grid.CellSize * 0.9f,
                    _grid.CellSize * 0.9f,
                    _grid.CellSize * 0.9f
                );

                //Debug.WriteLine($"Model: {(Content.Load<Model>(b.Model) == null ? "Null" : "Found")}");

                DrawModel(
                    Content.Load<Model>(b.Model),
                    size,
                    b
                );
            }

            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            DrawBuildingPreview();
            GraphicsDevice.BlendState = BlendState.Opaque;

            DrawHoveredCell();

            DrawUI();

            _buildingsGui.Draw(GraphicsDevice);

            base.Draw(gameTime);
        }

        private void DrawModel(
            Model model,
            Vector3 targetSize,
            Building b,
            bool isPreview = false)
        {
            Vector3 position = b.Transform.Position;
            Color color = b.Color;
            float rotY = b.buildRot;

            if (isPreview)
                color *= 0.5f;

            float rotationY = MathHelper.ToRadians(rotY);
            position.Y += b.offset.pos;
            targetSize *= b.offset.size;

            BoundingBox bounds = GetModelBounds(model);

            Vector3 modelSize = bounds.Max - bounds.Min;

            if (modelSize.X <= 0 ||
                modelSize.Y <= 0 ||
                modelSize.Z <= 0)
                return;

            float scaleX = targetSize.X / modelSize.X;
            float scaleY = targetSize.Y / modelSize.Y;
            float scaleZ = targetSize.Z / modelSize.Z;

            float scale = Math.Min(
                scaleX,
                Math.Min(scaleY, scaleZ)
            );

            Vector3 center = (bounds.Min + bounds.Max) * 0.5f;

            Matrix world =
                Matrix.CreateTranslation(-center) *
                Matrix.CreateScale(scale) *
                Matrix.CreateRotationY(rotationY) *
                Matrix.CreateTranslation(position);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = world;
                    effect.View = _camera.View;
                    effect.Projection = _camera.GetProjection(GraphicsDevice);

                    effect.EnableDefaultLighting();
                    //effect.DiffuseColor = color.ToVector3();
                    effect.Alpha = color.A / 255f;
                }

                mesh.Draw();
            }
        }

        private BoundingBox GetModelBounds(Model model)
        {
            BoundingBox? totalBounds = null;

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    VertexBuffer vertexBuffer =
                        part.VertexBuffer;

                    VertexDeclaration declaration =
                        vertexBuffer.VertexDeclaration;

                    VertexPositionNormalTexture[] vertices =
                        new VertexPositionNormalTexture[
                            vertexBuffer.VertexCount
                        ];

                    vertexBuffer.GetData(vertices);

                    foreach (VertexPositionNormalTexture vertex in vertices)
                    {
                        Vector3 position =
                            Vector3.Transform(
                                vertex.Position,
                                mesh.ParentBone?.Transform ??
                                Matrix.Identity
                            );

                        if (totalBounds == null)
                        {
                            totalBounds =
                                new BoundingBox(
                                    position,
                                    position
                                );
                        }
                        else
                        {
                            BoundingBox bounds =
                                totalBounds.Value;

                            bounds.Min =
                                Vector3.Min(
                                    bounds.Min,
                                    position
                                );

                            bounds.Max =
                                Vector3.Max(
                                    bounds.Max,
                                    position
                                );

                            totalBounds = bounds;
                        }
                    }
                }
            }

            return totalBounds ??
                   new BoundingBox(
                       Vector3.Zero,
                       Vector3.Zero
                   );
        }

        // Draw Ores
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

        private bool _statsDropdownOpen = false;

        private int _statsSelectedIndex = 0;

        private string[] _statsOptions = { };

        private const int StatsButtonX = 10;
        private const int StatsButtonY = 8;

        private const int StatsButtonPaddingX = 12;
        private const int StatsButtonPaddingY = 7;

        private const int StatsMenuSpacing = 2;
        private const int StatsItemPaddingX = 12;
        private const int StatsItemPaddingY = 8;

        private readonly Color _statsBackground = Color.Black * 0.7f;
        private readonly Color _statsHover = Color.DarkGray;
        //private readonly Color _statsHover = Color.Gray;

        private readonly Color _statsBorder = Color.Purple;

        private void UpdateStatsOptions()
        {
            _statsOptions = new[]
            {
                $"Total: {_totalOreInStorage:0.0} Ores",
                $"Storages: {_oreInStorage:0.0} Ores",
                $"Miners: {_oreInMiner:0.0} Ores",
                $"Belts: {_oreInBelt:0.0} Ores",
                $"Production: {_totalOreProduction:0.0} Ore/Min",
                $"Selected: {_selectedType}",
                $"Buildings: {_buildings.Count}"
            };
        }
        private void DrawUI()
        {
            string[] allInfo = {
                "Right Click = Delete Building",
                "E = View Building Info",
                "ESC = exit"
            };

            _spriteBatch.Begin();
            UpdateStatsOptions();

            string selectedText = _statsOptions[_statsSelectedIndex];

            Vector2 buttonTextSize = _font.MeasureString(selectedText);

            int buttonWidth = (int)buttonTextSize.X + 45;
            int buttonHeight = (int)buttonTextSize.Y + 14;

            Rectangle statsButton = new Rectangle(
                10,
                8,
                buttonWidth,
                buttonHeight
            );

            MouseState mouse = Mouse.GetState();

            bool buttonHovered = statsButton.Contains(mouse.Position);

            _spriteBatch.Draw(
                _pixel,
                statsButton,
                buttonHovered
                    ? Color.DarkGray
                    : Color.Black
            );

            DrawRectangleBorder(
                statsButton,
                1,
                Color.Purple
            );

            _spriteBatch.DrawString(
                _font,
                selectedText,
                new Vector2(
                    statsButton.X + 12,
                    statsButton.Y + 7
                ),
                Color.White
            );

            DrawDropdownArrow(
                statsButton,
                _statsDropdownOpen
            );

            // Other Information
            string info = "";

            foreach (string item in allInfo)
            {
                info += item;
                if (item != allInfo.Last())
                    info += "       |       ";
            }

            int infoX = statsButton.Right + 10;
            int infoWidth =
                GraphicsDevice.Viewport.Width -
                infoX -
                10;

            Rectangle infoBox = new Rectangle(
                infoX,
                8,
                infoWidth,
                buttonHeight
            );

            bool infoHovered = infoBox.Contains(mouse.Position);

            _spriteBatch.Draw(
                _pixel,
                infoBox,
                Color.Black
            );

            DrawRectangleBorder(
                infoBox,
                1,
                Color.Purple
            );

            Vector2 infoSize = _font.MeasureString(info);

            _spriteBatch.DrawString(
                _font,
                info,
                new Vector2(
                    infoBox.X + 12,
                    infoBox.Y +
                    (infoBox.Height - infoSize.Y) / 2f
                ),
                Color.LightGray
            );


            if (_statsDropdownOpen)
            {
                DrawStatsDropdown(
                    statsButton,
                    GraphicsDevice.Viewport
                );
            }

            //Building Btns
            DrawToolbarButton(0, BuildingType.Miner, Color.OrangeRed, "1");
            DrawToolbarButton(1, BuildingType.Belt, Color.SlateGray, "2");
            DrawToolbarButton(2, BuildingType.Storage, Color.SaddleBrown, "3");

            _spriteBatch.End();

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
        }

        private void DrawDropdownArrow(
            Rectangle button,
            bool open)
        {
            int arrowX = button.Right - 17;
            int arrowY = button.Y + button.Height / 2;

            if (open)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        arrowX,
                        arrowY - 1,
                        10,
                        2
                    ),
                    Color.LightGray
                );

                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        arrowX + 2,
                        arrowY - 3,
                        6,
                        2
                    ),
                    Color.LightGray
                );

                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        arrowX + 4,
                        arrowY - 5,
                        2,
                        2
                    ),
                    Color.LightGray
                );
            }
            else
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        arrowX,
                        arrowY,
                        10,
                        2
                    ),
                    Color.LightGray
                );

                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        arrowX + 2,
                        arrowY + 2,
                        6,
                        2
                    ),
                    Color.LightGray
                );

                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(
                        arrowX + 4,
                        arrowY + 4,
                        2,
                        2
                    ),
                    Color.LightGray
                );
            }
        }

        private void DrawStatsDropdown(
            Rectangle button,
            Viewport viewport)
        {
            int maxTextWidth = 0;
            int itemHeight = 0;

            for (int i = 0; i < _statsOptions.Length; i++)
            {
                Vector2 size =
                    _font.MeasureString(_statsOptions[i]);

                maxTextWidth = Math.Max(
                    maxTextWidth,
                    (int)size.X
                );

                itemHeight = Math.Max(
                    itemHeight,
                    (int)size.Y + 16
                );
            }

            int menuWidth = maxTextWidth + 24;

            int menuHeight =
                _statsOptions.Length * itemHeight +
                (_statsOptions.Length - 1) * 2;

            int menuX = button.X;
            int menuY = button.Bottom + 4;

            if (menuX + menuWidth > viewport.Width)
            {
                menuX = viewport.Width - menuWidth - 5;
            }

            if (menuY + menuHeight > viewport.Height)
            {
                menuY = button.Y - menuHeight - 4;
            }

            Rectangle menu = new Rectangle(
                menuX,
                menuY,
                menuWidth,
                menuHeight
            );

            _spriteBatch.Draw(
                _pixel,
                menu,
                Color.Black
            );

            DrawRectangleBorder(
                menu,
                1,
                Color.Purple
            );

            for (int i = 0; i < _statsOptions.Length; i++)
            {
                Rectangle item = new Rectangle(
                    menuX,
                    menuY + i * (itemHeight + 2),
                    menuWidth,
                    itemHeight
                );

                bool hovered =
                    item.Contains(Mouse.GetState().Position);

                if (hovered)
                {
                    _spriteBatch.Draw(
                        _pixel,
                        item,
                        Color.DarkGray
                    );
                }

                Vector2 textSize =
                    _font.MeasureString(
                        _statsOptions[i]
                    );

                _spriteBatch.DrawString(
                    _font,
                    _statsOptions[i],
                    new Vector2(
                        item.X + 12,
                        item.Y +
                        (item.Height - textSize.Y) / 2f
                    ),
                    Color.White
                );
            }
        }

        private void DrawRectangleBorder(
            Rectangle rectangle,
            int thickness,
            Color color)
        {
            // Top
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    rectangle.X,
                    rectangle.Y,
                    rectangle.Width,
                    thickness
                ),
                color
            );

            // Down
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    rectangle.X,
                    rectangle.Bottom - thickness,
                    rectangle.Width,
                    thickness
                ),
                color
            );

            // Left
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    rectangle.X,
                    rectangle.Y,
                    thickness,
                    rectangle.Height
                ),
                color
            );

            // Right
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    rectangle.Right - thickness,
                    rectangle.Y,
                    thickness,
                    rectangle.Height
                ),
                color
            );
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

        public void DrawCube(
            Vector3 position,
            Vector3 size,
            Color color,
            float alpha = 1f)
        {
            _effect.World = Matrix.CreateScale(size) * Matrix.CreateTranslation(position);

            _effect.DiffuseColor = color.ToVector3();
            _effect.Alpha = alpha;

            GraphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
            GraphicsDevice.Indices = _cubeIndexBuffer;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    0,
                    12
                );
            }
        }
    }
}
