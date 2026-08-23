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

        // Förderrichtung, z.B. (1,0,0) = in Richtung +X (eine Rasterzelle weiter)
        public Vector3 Direction { get; set; } = new Vector3(1, 0, 0);
        public float ItemAmount { get; set; }
        public float Capacity { get; set; } = 5f;
        public float Speed { get; set; } = 2f; // Einheiten pro Sekunde

        public Belt(int gridX, int gridZ, Vector3 worldPosition)
            : base(gridX, gridZ, worldPosition) {
            
        }
    }
}
