using FactoryGame.Engine;
using Microsoft.Xna.Framework;

namespace NexaForge
{
    // Basisklasse für alle Objekte in der Engine (sehr simples Entity-System)
    public abstract class GameObject
    {
        public Transform Transform { get; } = new Transform();
        public bool IsActive { get; set; } = true;

        public virtual void Update(GameTime gameTime) { }
    }
}
