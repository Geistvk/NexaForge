using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NexaForge
{
    // Eine Kamera, die sich um einen Zielpunkt dreht - typisch für Aufbau-/Strategiespiele
    public class OrbitCamera
    {
        public Vector3 Target = Vector3.Zero;
        public float Distance = 25f;
        public float Yaw = 0;
        public float Pitch = 0.9f;

        private const float MinDistance = 5f;
        private const float MaxDistance = 60f;
        private const float MinPitch = 0.2f;
        private const float MaxPitch = MathHelper.PiOver2 - 0.05f;

        private MouseState _prevMouse;

        public Vector3 EyePosition
        {
            get
            {
                var offset = new Vector3(
                    Distance * MathF.Cos(Pitch) * MathF.Sin(Yaw),
                    Distance * MathF.Sin(Pitch),
                    Distance * MathF.Cos(Pitch) * MathF.Cos(Yaw));
                return Target + offset;
            }
        }

        public Matrix View => Matrix.CreateLookAt(EyePosition, Target, Vector3.Up);

        public Matrix GetProjection(GraphicsDevice device) =>
            Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4,
                device.Viewport.AspectRatio,
                0.1f,
                2000f);

        // Steuerung: rechte Maustaste + Bewegung = drehen, Mausrad = zoomen, WASD = Zielpunkt verschieben
        public void HandleInput(GameTime gameTime)
        {
            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Pressed)
            {
                int dx = mouse.X - _prevMouse.X;
                int dy = mouse.Y - _prevMouse.Y;
                Yaw -= dx * 0.005f;
                Pitch = MathHelper.Clamp(Pitch + dy * 0.005f, MinPitch, MaxPitch);
            }

            int scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            if (scrollDelta != 0)
                Distance = MathHelper.Clamp(Distance - scrollDelta * 0.01f, MinDistance, MaxDistance);

            var move = Vector3.Zero;
            var forward = new Vector3(MathF.Sin(Yaw), 0, MathF.Cos(Yaw));
            var right = Vector3.Cross(Vector3.Up, forward);

            if (keyboard.IsKeyDown(Keys.W)) move += forward;
            if (keyboard.IsKeyDown(Keys.S)) move -= forward;
            if (keyboard.IsKeyDown(Keys.A)) move += right;
            if (keyboard.IsKeyDown(Keys.D)) move -= right;

            if (move != Vector3.Zero)
            {
                move.Normalize();
                Target -= move * dt * 15f * (Distance / 20f);
            }

            if (mouse.MiddleButton == ButtonState.Pressed && _prevMouse.MiddleButton == ButtonState.Pressed)
            {
                int dx = mouse.X - _prevMouse.X;
                int dz = mouse.Y - _prevMouse.Y;
                Target.X -= dx * 0.0625f;
                Target.Z -= dz * 0.0625f;
            }

            _prevMouse = mouse;
        }
    }
}
