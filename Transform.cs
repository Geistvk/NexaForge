using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

    /*public class Util
    {
        protected BasicEffect _effect;
        protected VertexBuffer _cubeVertexBuffer;
        protected IndexBuffer _cubeIndexBuffer;

        protected SpriteBatch _spriteBatch;
        protected SpriteFont _font;
        protected Texture2D _pixel;
        public Util(
            BasicEffect effect,
            VertexBuffer cubeVertexBuffer,
            IndexBuffer cubeIndexBuffer,
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel
            
            )
        {
            _effect = effect;
            _cubeVertexBuffer = cubeVertexBuffer;
            _cubeIndexBuffer = cubeIndexBuffer;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
        }
        private static void DrawCube(
            Vector3 position,
            Vector3 size,
            Color color,
            float alpha = 1f)
        {
            _effect.World = Matrix.CreateScale(size) * Matrix.CreateTranslation(position);

            _effect.DiffuseColor = color.ToVector3();
            _effect.Alpha = alpha;

            GraphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
            GraphicsDevice.Indices = _cubeIndexBuffer;

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    0,
                    12
                );
            }
        }
    }*/
}
