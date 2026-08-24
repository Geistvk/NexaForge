using Microsoft.Xna.Framework;
using System;

namespace NexaForge
{
    // Transportiert eine Ressourcenmenge in eine Richtung zur nächsten Zelle
    public class Belt : Building
    {
        public override BuildingType Type => BuildingType.Belt;
        public override Color Color => Color.SlateGray;
        public override String Name => "Belt";
        public override String Status { get; set; } = "Idle";

        // Förderrichtung, z.B. (1,0,0) = in Richtung +X (eine Rasterzelle weiter)
        public Vector3 Direction { get; set; } = new Vector3(1, 0, 0);
        public float ItemAmount { get; set; }
        public float Capacity { get; set; } = 5f;
        public float Speed { get; set; } = 2f; // Einheiten pro Sekunde

        public Belt(int gridX, int gridZ, Vector3 worldPosition)
            : base(gridX, gridZ, worldPosition) {
            
        }

        public void mineItem(float dt, Miner source) {
            float space = Capacity - ItemAmount;
            float moved = source.Extract(Math.Min(space, source.MineRatePerSecond * dt * 2f));
            ItemAmount += moved;
            if (moved > 0)
                this.setStatus(5);
            else 
                this.setStatus(0);
        }

        public void moveItem(float dt, Belt nextBelt)
        {
            float space = nextBelt.Capacity - nextBelt.ItemAmount;
            float moved = Math.Min(space, ItemAmount);
            moved = Math.Min(moved, Speed * dt);
            ItemAmount -= moved;
            nextBelt.ItemAmount += moved;
            if (moved > 0)
                this.setStatus(4);
            else
                this.setStatus(0);
        }

        public void storeItem(float dt, Storage storage)
        {
            float space = storage.Capacity - storage.Stored;
            float moved = Math.Min(space, ItemAmount);
            moved = Math.Min(moved, Speed * dt);
            ItemAmount -= moved;
            storage.Deposit(moved);
            if (moved > 0)
                this.setStatus(4);
            else
                this.setStatus(0);
        }

        public override void Draw(Game1 game, bool isPreview)
        {

        }
    }
}
