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

        public float OreBuffer { get; private set; }
        public float MineRatePerSecond { get; set; } = 1f;
        public float BufferCapacity { get; set; } = 10f;

        public Miner(int gridX, int gridZ, Vector3 worldPosition)
            : base(gridX, gridZ, worldPosition) {
            
        }

        // Füllt den Puffer mit tatsächlich aus dem Boden gefördertem Erz (siehe Game1.SimulateFactory).
        // Es gibt hier keine automatische, unendliche Produktion mehr - das Erz kommt aus VoxelGrid.
        public void AddOre(float amount)
        {
            OreBuffer = Math.Min(BufferCapacity, OreBuffer + amount);
        }

        public float getCapacityLeft()
        {
            return BufferCapacity - OreBuffer;
        }

        // Nimmt bis zu 'amount' Erz aus dem Puffer, gibt zurück, wie viel tatsächlich entnommen wurde
        public float Extract(float amount)
        {
            float taken = Math.Min(amount, OreBuffer);
            OreBuffer -= taken;
            return taken;
        }
    }
}
