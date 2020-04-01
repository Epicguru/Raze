using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GVS.Sprites
{
    public class Sprite
    {
        public string Name { get; set; }
        public Texture2D Texture { get; private set; }
        public Rectangle Region { get; set; }
        public float DrawScale { get; } = 1; // In a future version, with multi-resolution tile support, this will allow sprites to be draw automatically scaled when they don't match size.
        public Vector2 Pivot { get; set; } = Vector2.Zero;

        /// <summary>
        /// Constructs a sprite from a texture and a region.
        /// The texture should not be null, but if it is then no error or exception will be thrown.
        /// If the region is (0, 0, 0, 0) the the region will be automatically set to the size of the texture,
        /// assuming that the texture itself isn't null.
        /// </summary>
        public Sprite(Texture2D texture, Rectangle region)
        {
            this.Texture = texture;
            this.Region = region;
            this.Name = texture?.Name;

            if (region == Rectangle.Empty && texture != null)
            {
                this.Region = new Rectangle(0, 0, texture.Width, texture.Height);
            }
        }

        public virtual void SetTexture(Texture2D t)
        {
            this.Texture = t;
        }

        public override string ToString()
        {
            return Name ?? "no-name";
        }
    }
}
