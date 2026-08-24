using Microsoft.Xna.Framework;
using FactoryGame.Engine;
using System.Diagnostics;

namespace NexaForge
{
    public enum BuildingType { 
        None,
        Miner, 
        Belt, 
        Storage 
    }

    public abstract class Building : GameObject
    {
        public int GridX { get; }
        public int GridZ { get; }

        public abstract string Name { get; }

        public abstract BuildingType Type { get; }
        public abstract Color Color { get; }
        public abstract string Status { get; set; }

        public abstract void Draw(Game1 game, bool isPreview);


        private string[] allStatus = { 
            "Idle",
            "Working",
            "Maintenance",
            "Mining",
            "Moving",
            "Storing"
        };

        protected Building(int gridX, int gridZ, Vector3 worldPosition)
        {
            GridX = gridX;
            GridZ = gridZ;
            Transform.Position = worldPosition;
        }

        public virtual void Tick(float deltaSeconds) { }

        public void setStatus(int index) { 
            Status = allStatus[index];
        }
    }
}
