using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RazeUI.UISprites
{
    public class UISprite
    {
        public Texture2D Texture { get; set; }
        public Rectangle Region { get; set; }

        public UISprite(Texture2D texture) : this(texture, Rectangle.Empty)
        {

        }

        public UISprite(Texture2D texture, Rectangle region)
        {
            this.Texture = texture;
            this.Region = (region == Rectangle.Empty && texture != null) ? new Rectangle(0, 0, texture.Width, texture.Height) : region;
        }

        public void Draw(SpriteBatch spr, Point position, Color color)
        {
            this.Draw(spr, position.ToVector2(), color);
        }

        public void Draw(SpriteBatch spr, Vector2 position, Color color)
        {
            this.Draw(spr, new Rectangle((int) position.X, (int) position.Y, Region.Width, Region.Height), color);
        }

        public void Draw(SpriteBatch spr, Rectangle dest, Color color)
        {
            if (this.Texture == null)
                return;

            spr.Draw(this.Texture, dest, Region, color);
        }
    }
}
