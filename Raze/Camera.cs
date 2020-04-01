using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS
{
    public class Camera
    {
        public Vector2 Position;
        public float Rotation { get; set; }
        public float Zoom
        {
            get
            {
                return this.zoom;
            }
            set
            {
                this.zoom = Math.Max(Math.Min(value, 10), 0.005f);
            }
        }
        public Rectangle WorldViewBounds { get; private set; }
        public bool UpdateViewBounds { get; set; } = true;

        private float zoom = 1f;
        private Matrix matrix;
        private Matrix inverted;

        public void UpdateMatrix(GraphicsDevice graphicsDevice)
        {
            this.matrix =
              Matrix.CreateTranslation(new Vector3(-(int)Position.X, -(int)Position.Y, 0)) *
                                         Matrix.CreateRotationZ(MathHelper.ToRadians(-Rotation)) *
                                         Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
                                         Matrix.CreateTranslation(new Vector3(graphicsDevice.Viewport.Width * 0.5f, graphicsDevice.Viewport.Height * 0.5f, 0));

            var m = this.GetMatrix();
            inverted = Matrix.Invert(m);

            if (!UpdateViewBounds)
                return;

            var topLeft = Vector2.Transform(new Vector2(0, 0), inverted);
            var bottomRight = Vector2.Transform(new Vector2(Screen.Width, Screen.Height), inverted);

            var r = WorldViewBounds;
            r.X = (int)topLeft.X;
            r.Y = (int)topLeft.Y;
            r.Width = (int)Math.Ceiling(bottomRight.X - topLeft.X);
            r.Height = (int)Math.Ceiling(bottomRight.Y - topLeft.Y);
            WorldViewBounds = r;
        }

        public Vector2 ScreenToWorldPosition(Vector2 screenPos)
        {
            return Vector2.Transform(screenPos, inverted);
        }

        public Vector2 WorldToScreenPosition(Vector2 worldPos)
        {
            return Vector2.Transform(worldPos, matrix);
        }

        public Matrix GetMatrix()
        {
            return matrix;
        }
    }
}
