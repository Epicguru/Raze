using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Raze.Sprites
{
    public static class SpriteUtils
    {
        public static void Draw(this SpriteBatch spr, Sprite s, Vector2 pos, Color color, float depth, float rotation = 0f, float scale = 1f, SpriteEffects effects = SpriteEffects.None)
        {
            Draw(spr, s, new Rectangle((int)pos.X, (int)pos.Y, s.Region.Width, s.Region.Height), color, depth, rotation, scale, effects);
        }

        public static void Draw(this SpriteBatch spr, Sprite s, Rectangle destination, Color color, float depth, float rotation = 0f, float scale = 1f, SpriteEffects effects = SpriteEffects.None)
        {
            bool useTex = s.Texture != null && !s.Texture.IsDisposed;
            if (!useTex)
            {
                spr.Draw(Main.MissingTextureSprite, destination, color, depth, rotation, scale, effects);
                return;
            }

            Rectangle dest = destination;
            dest.Width = (int) (dest.Width * s.DrawScale * scale);
            dest.Height = (int) (dest.Height * s.DrawScale * scale);
            spr.Draw(s.Texture, dest, s.Region, color, rotation, s.Region.Size.ToVector2() * s.Pivot, effects, depth);
        }
    }
}
