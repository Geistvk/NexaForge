using Microsoft.Xna.Framework;
using System;

namespace NexaForge
{
    public class Belt : Building
    {
        public override BuildingType Type => BuildingType.Belt;
        public override Color Color => Color.SlateGray;
        public override String Name => "Belt";
        public override String Status { get; set; } = "Idle";
        public override buildingOffset offset => new buildingOffset(0.3f, 0.4f);

        public Vector3 Direction { get; set; } = new Vector3(1, 0, 0);
        public float ItemAmount { get; set; }
        public float Capacity { get; set; } = 5f;
        public float Speed { get; set; } = 2f;

        public Belt(int gridX, int gridZ, Vector3 worldPosition)
            : base(gridX, gridZ, worldPosition) {
            upgradeLevel = 0;
            modelPath = "Models/WoodHouse";
            upgradeModel();
        }

        public override void upgradeModel()
        {
            upgradeLevel++;
            Model = modelPath + "_" + upgradeLevel.ToString();
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
    }
}
