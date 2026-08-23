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

        public float Stored { get; private set; }
        public float Capacity { get; set; } = 200f;

        public Storage(int gridX, int gridZ, Vector3 worldPosition)
            : base(gridX, gridZ, worldPosition) {
            
        }

        // Liefert Ressourcen an, gibt zurück, wie viel tatsächlich angenommen wurde
        public float Deposit(float amount)
        {
            float space = Capacity - Stored;
            float accepted = Math.Min(space, amount);
            Stored += accepted;
            return accepted;
        }
    }
}
