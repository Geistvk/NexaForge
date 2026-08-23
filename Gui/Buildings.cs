using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FactoryGame.Engine;
using System.Diagnostics;

namespace NexaForge
{
    public class BuildingsGui
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        private Building _building;
        private readonly VoxelGrid _grid;

        public bool IsOpen { get; private set; }

        public BuildingsGui(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            VoxelGrid grid)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _grid = grid;
        }

        public void Open(Building building)
        {
            if (building == null)
                return;

            _building = building;
            IsOpen = true;
        }

        public void Close()
        {
            _building = null;
            IsOpen = false;
        }

        public void Draw(GraphicsDevice graphicsDevice)
        {
            if (!IsOpen || _building == null ||
                _grid.Get(_building.GridX, _building.GridZ) != _building ||
                _grid.Get(_building.GridX, _building.GridZ) == null)
            {
                Close();
                return;
            }

            _spriteBatch.Begin();

            int width = 360;
            int height = 230;

            int x = graphicsDevice.Viewport.Width - width - 20;
            int y = 60;

            Rectangle window = new Rectangle(
                x,
                y,
                width,
                height
            );

            // Background
            _spriteBatch.Draw(
                _pixel,
                window,
                Color.Black
            );

            // Border
            const int border = 2;

            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x,
                    y,
                    width,
                    border
                ),
                Color.Purple
            );

            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x,
                    y + height - border,
                    width,
                    border
                ),
                Color.Purple
            );

            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x,
                    y,
                    border,
                    height
                ),
                Color.Purple
            );

            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x + width - border,
                    y,
                    border,
                    height
                ),
                Color.Purple
            );

            // Title
            _spriteBatch.DrawString(
                _font,
                _building.Type.ToString(),
                new Vector2(
                    x + 20,
                    y + 18
                ),
                Color.White
            );

            // Separator
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x + 20,
                    y + 50,
                    width - 40,
                    1
                ),
                Color.Gray
            );

            // Information
            DrawInfo(
                x + 20,
                y + 70,
                "Building",
                _building.Type.ToString()
            );

            DrawInfo(
                x + 20,
                y + 100,
                "Grid X",
                _building.GridX.ToString()
            );

            DrawInfo(
                x + 20,
                y + 130,
                "Grid Z",
                _building.GridZ.ToString()
            );

            DrawInfo(
                x + 20,
                y + 160,
                "Position",
                $"{_building.Transform.Position.X:0.0} | " +
                $"{_building.Transform.Position.Y:0.0} | " +
                $"{_building.Transform.Position.Z:0.0}"
            );

            // Notice
            _spriteBatch.DrawString(
                _font,
                "Press E to Close this GUI",
                new Vector2(
                    x + 20,
                    y + 195
                ),
                Color.Gray
            );

            _spriteBatch.End();

            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.BlendState = BlendState.Opaque;
        }

        private void DrawInfo(
            int x,
            int y,
            string name,
            string value)
        {
            _spriteBatch.DrawString(
                _font,
                name,
                new Vector2(x, y),
                Color.LightGray
            );

            _spriteBatch.DrawString(
                _font,
                value,
                new Vector2(x + 120, y),
                Color.White
            );
        }
    }
}
