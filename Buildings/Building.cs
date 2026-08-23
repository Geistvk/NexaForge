using Microsoft.Xna.Framework;
using FactoryGame.Engine;

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

        protected Building(int gridX, int gridZ, Vector3 worldPosition)
        {
            GridX = gridX;
            GridZ = gridZ;
            Transform.Position = worldPosition;
        }

        // Wird jeden Frame für die Fabriklogik aufgerufen (Fördern, Produzieren, ...)
        public virtual void Tick(float deltaSeconds) { }
    }
}
