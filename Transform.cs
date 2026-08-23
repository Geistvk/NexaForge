using Microsoft.Xna.Framework;

namespace FactoryGame.Engine
{
    // Position, Rotation und Skalierung eines Objekts in der Welt
    public class Transform
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation = Vector3.Zero; // in Radiant (Yaw, Pitch, Roll)
        public Vector3 Scale = Vector3.One;

        public Matrix WorldMatrix =>
            Matrix.CreateScale(Scale) *
            Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
            Matrix.CreateTranslation(Position);
    }
}
