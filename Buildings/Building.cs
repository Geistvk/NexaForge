using FactoryGame.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace NexaForge
{
    public enum BuildingType { 
        None,
        Miner, 
        Belt, 
        Storage 
    }

    public struct buildingOffset {
        public float pos;
        public float size;

        public buildingOffset(float pos, float size)
        {
            this.pos = pos;
            this.size = size;
        }
    }

    public abstract class Building : GameObject
    {
        public int GridX { get; }
        public int GridZ { get; }

        public abstract string Name { get; }

        public abstract BuildingType Type { get; }
        public abstract Color Color { get; }
        public abstract string Status { get; set; }
        public abstract buildingOffset offset { get; }

        public int upgradeLevel { get; set; }
        public string modelPath { get; set; }
        public string Model { get; set; }
        public abstract void upgradeModel();

        public float buildRot { get; set; }


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
            buildRot = 0;
        }

        public virtual void Tick(float deltaSeconds) { }

        public void setStatus(int index) { 
            Status = allStatus[index];
        }

        public void setRot(float rot) {
            buildRot = rot;
        }
    }
}
