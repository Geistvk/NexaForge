using System;
using Microsoft.Xna.Framework;

namespace NexaForge
{
    // Fördert automatisch Erz, das anschließend über Bänder transportiert wird
    public class Miner : Building
    {
        public override BuildingType Type => BuildingType.Miner;
        public override Color Color => Color.OrangeRed;
        public override String Name => "Miner";
        public override String Status { get; set; } = "Idle";

        public float OreBuffer { get; private set; }
        public float MineRatePerSecond { get; set; } = 1f;
        public float BufferCapacity { get; set; } = 10f;

        public Miner(int gridX, int gridZ, Vector3 worldPosition)
            : base(gridX, gridZ, worldPosition) {
            
        }

        public void AddOre(float amount)
        {
            OreBuffer = Math.Min(BufferCapacity, OreBuffer + amount);
            if (BufferCapacity > OreBuffer + amount)
                this.setStatus(3);
            else
                this.setStatus(0);
        }

        public float getCapacityLeft()
        {
            return BufferCapacity - OreBuffer;
        }

        public float Extract(float amount)
        {
            float taken = Math.Min(amount, OreBuffer);
            OreBuffer -= taken;
            return taken;
        }

        public float getRate(float dt, VoxelGrid grid) 
        {
            float wanted = Math.Min(MineRatePerSecond * dt, BufferCapacity - OreBuffer);

            if (OreBuffer < BufferCapacity &&
                grid.ExtractOre(GridX, GridZ, wanted, this) > 0)
                return MineRatePerSecond * 60f;
            else
                return 0.0f;
        }

        public override void Draw(Game1 game, bool isPreview)
        {
             
        }
    }
}
