using System;
using Microsoft.Xna.Framework;

namespace NexaForge
{
    // Sammelt Ressourcen, die über Bänder angeliefert werden
    public class Storage : Building
    {
        public override BuildingType Type => BuildingType.Storage;
        public override Color Color => Color.SaddleBrown;
        public override String Name => "Storage";
        public override String Status { get; set; } = "Idle";

        public float Stored { get; private set; }
        public float Capacity { get; set; } = 200f;

        public Storage(int gridX, int gridZ, Vector3 worldPosition)
            : base(gridX, gridZ, worldPosition) {
            
        }

        public float Deposit(float amount)
        {
            float space = Capacity - Stored;
            float accepted = Math.Min(space, amount);
            Stored += accepted;
            if (accepted > 0)
                this.setStatus(5);
            else
                this.setStatus(0);
            return accepted;
        }

        public void mineItem(float dt, Miner miner)
        {
            float space = Capacity - Stored;
            float moved = miner.Extract(Math.Min(space, miner.MineRatePerSecond * dt * 2f));
            Stored += moved;
            if (moved > 0)
                this.setStatus(5);
            else
                this.setStatus(0);
        }

        public override void Draw(Game1 game, bool isPreview = false)
        {
            Vector3 p = Transform.Position;
            p.Y -= 1f;

            float s = 0.7f;
            float alpha = isPreview ? 0.6f : 1f;

            // Hauptkörper
            game.DrawCube(
                p + new Vector3(0f, 1.0f * s, 0f),
                new Vector3(2.4f * s, 2.0f * s, 2.0f * s),
                Color.DarkGray,
                alpha
            );

            // Oberer Aufbau
            game.DrawCube(
                p + new Vector3(0f, 2.3f * s, 0f),
                new Vector3(1.6f * s, 0.6f * s, 1.6f * s),
                Color.Gray,
                alpha
            );

            // Dach
            game.DrawCube(
                p + new Vector3(0f, 2.75f * s, 0f),
                new Vector3(2.7f * s, 0.25f * s, 2.3f * s),
                Color.DarkGray,
                alpha
            );

            // Linker Speicher
            game.DrawCube(
                p + new Vector3(-1.4f * s, 1.2f * s, 0f),
                new Vector3(0.5f * s, 2.4f * s, 1.4f * s),
                Color.Gray,
                alpha
            );

            // Rechter Speicher
            game.DrawCube(
                p + new Vector3(1.4f * s, 1.2f * s, 0f),
                new Vector3(0.5f * s, 2.4f * s, 1.4f * s),
                Color.Gray,
                alpha
            );

            // Vorderer Eingang
            game.DrawCube(
                p + new Vector3(0f, 0.7f * s, -1.05f * s),
                new Vector3(0.9f * s, 1.4f * s, 0.15f * s),
                Color.Black,
                alpha
            );

            // Türrahmen links
            game.DrawCube(
                p + new Vector3(-0.55f * s, 0.8f * s, -1.15f * s),
                new Vector3(0.15f * s, 1.6f * s, 0.15f * s),
                Color.Gray,
                alpha
            );

            // Türrahmen rechts
            game.DrawCube(
                p + new Vector3(0.55f * s, 0.8f * s, -1.15f * s),
                new Vector3(0.15f * s, 1.6f * s, 0.15f * s),
                Color.Gray,
                alpha
            );

            // Oberer Lichtstreifen
            game.DrawCube(
                p + new Vector3(0f, 2.0f * s, -1.08f * s),
                new Vector3(1.5f * s, 0.15f * s, 0.12f * s),
                Color.LightGray,
                alpha
            );

            // Seitlicher Tank
            game.DrawCube(
                p + new Vector3(1.55f * s, 1.0f * s, 0.75f * s),
                new Vector3(0.45f * s, 1.8f * s, 0.45f * s),
                Color.Gray,
                alpha
            );

            // Tank oben
            game.DrawCube(
                p + new Vector3(1.55f * s, 2.0f * s, 0.75f * s),
                new Vector3(0.65f * s, 0.2f * s, 0.65f * s),
                Color.DarkGray,
                alpha
            );
        }
    }
}
