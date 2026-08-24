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

        private const int lineHeight = 30;
        private const int startY = 70;
        private const int padding = 20;
        private const int valueOffset = 140;
        private const int border = 2;

        // Abstände
        private const int titleTopPadding = 18;
        private const int separatorOffset = 50;
        private const int bottomPadding = 20;
        private const int noticeSpacing = 15;

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
            if (!IsOpen ||
                _building == null ||
                _grid.Get(_building.GridX, _building.GridZ) != _building)
            {
                Close();
                return;
            }

            _spriteBatch.Begin();

            Building building = _building switch
            {
                Miner => _building as Miner,
                Belt => _building as Belt,
                Storage => _building as Storage,
                _ => null
            };

            float stored = building switch
            {
                Miner miner => miner.OreBuffer,
                Belt belt => belt.ItemAmount,
                Storage storage => storage.Stored,
                _ => 0f
            };

            string title = _building.Type.ToString();

            string[,] infos = {
                { "Building", _building.Type.ToString() },
                { "Content", $"{stored:0.0} Ore" },
                { "Status", _building.Status },
                { "Grid X", _building.GridX.ToString() },
                { "Grid Z", _building.GridZ.ToString() },
                {
                    "Position",
                    $"{_building.Transform.Position.X:0.0} | " +
                    $"{_building.Transform.Position.Y:0.0} | " +
                    $"{_building.Transform.Position.Z:0.0}"
                }
            };

            string notice = "Press E to Close this GUI";

            // ------------------------------------------------------------
            // Dynamische Größe berechnen
            // ------------------------------------------------------------

            int infoCount = infos.GetLength(0);

            Vector2 titleSize = _font.MeasureString(title);
            Vector2 noticeSize = _font.MeasureString(notice);

            float maxNameWidth = 0;
            float maxValueWidth = 0;

            for (int i = 0; i < infoCount; i++)
            {
                Vector2 nameSize = _font.MeasureString(infos[i, 0]);
                Vector2 valueSize = _font.MeasureString(infos[i, 1]);

                if (nameSize.X > maxNameWidth)
                    maxNameWidth = nameSize.X;

                if (valueSize.X > maxValueWidth)
                    maxValueWidth = valueSize.X;
            }

            // Breite der beiden Spalten
            int columnGap = 25;

            int contentWidth =
                padding +
                (int)Math.Max(titleSize.X, maxNameWidth + columnGap + maxValueWidth) +
                padding;

            // Notice muss ebenfalls hineinpassen
            int noticeWidth = padding + (int)noticeSize.X + padding;

            int width = Math.Max(contentWidth, noticeWidth);

            // Mindestbreite
            width = Math.Max(width, 300);

            // ------------------------------------------------------------
            // Dynamische Höhe
            // ------------------------------------------------------------

            int titleHeight = (int)titleSize.Y;

            int titleAreaHeight =
                titleTopPadding +
                titleHeight +
                12;

            int separatorHeight = 1;

            int infoAreaHeight =
                infoCount * lineHeight;

            int noticeHeight = (int)noticeSize.Y;

            int height =
                titleAreaHeight +
                separatorHeight +
                noticeSpacing +
                infoAreaHeight +
                noticeSpacing +
                noticeHeight +
                bottomPadding;

            // ------------------------------------------------------------
            // Position
            // ------------------------------------------------------------

            int x = graphicsDevice.Viewport.Width - width - 20;
            int y = 60;

            Rectangle window = new Rectangle(
                x,
                y,
                width,
                height
            );

            // ------------------------------------------------------------
            // Background
            // ------------------------------------------------------------

            _spriteBatch.Draw(
                _pixel,
                window,
                Color.Black
            );

            // ------------------------------------------------------------
            // Border
            // ------------------------------------------------------------

            DrawBorder(
                x,
                y,
                width,
                height,
                border,
                Color.Purple
            );

            // ------------------------------------------------------------
            // Title
            // ------------------------------------------------------------

            int titleY = y + titleTopPadding;

            _spriteBatch.DrawString(
                _font,
                title,
                new Vector2(
                    x + padding,
                    titleY
                ),
                Color.White
            );

            // ------------------------------------------------------------
            // Separator
            // ------------------------------------------------------------

            int separatorY = y + separatorOffset;

            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x + padding,
                    separatorY,
                    width - padding * 2,
                    separatorHeight
                ),
                Color.Gray
            );

            // ------------------------------------------------------------
            // Information
            // ------------------------------------------------------------

            int infoStartY =
                separatorY +
                separatorHeight +
                noticeSpacing;

            for (int i = 0; i < infoCount; i++)
            {
                DrawInfo(
                    x,
                    infoStartY,
                    infos[i, 0],
                    infos[i, 1],
                    i
                );
            }

            // ------------------------------------------------------------
            // Notice
            // ------------------------------------------------------------

            int noticeY =
                infoStartY +
                infoAreaHeight +
                noticeSpacing;

            _spriteBatch.DrawString(
                _font,
                notice,
                new Vector2(
                    x + padding,
                    noticeY
                ),
                Color.Gray
            );

            _spriteBatch.End();

            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.BlendState = BlendState.Opaque;
        }

        private void DrawInfo(
            int x,
            int startY,
            string name,
            string value,
            int index)
        {
            int y = startY + index * lineHeight;

            _spriteBatch.DrawString(
                _font,
                name,
                new Vector2(
                    x + padding,
                    y
                ),
                Color.LightGray
            );

            _spriteBatch.DrawString(
                _font,
                value,
                new Vector2(
                    x + valueOffset,
                    y
                ),
                Color.White
            );
        }

        private void DrawBorder(
            int x,
            int y,
            int width,
            int height,
            int thickness,
            Color color)
        {
            // Oben
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x,
                    y,
                    width,
                    thickness
                ),
                color
            );

            // Unten
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x,
                    y + height - thickness,
                    width,
                    thickness
                ),
                color
            );

            // Links
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x,
                    y,
                    thickness,
                    height
                ),
                color
            );

            // Rechts
            _spriteBatch.Draw(
                _pixel,
                new Rectangle(
                    x + width - thickness,
                    y,
                    thickness,
                    height
                ),
                color
            );
        }
    }
}
