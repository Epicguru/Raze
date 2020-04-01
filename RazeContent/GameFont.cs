using SpriteFontPlus;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RazeContent
{
    public class GameFont : IDisposable
    {
        /// <summary>
        /// Gets or sets the font size.
        /// The setter is equivalent to calling <see cref="SetSize(float)"/>.
        /// </summary>
        public float Size
        {
            get
            {
                return font.Size;
            }
            set
            {
                SetSize(value);
            }
        }

        internal DynamicSpriteFont font;
        internal Point drawOffset; // The offset required to get the top-left corner actually drawing where you want it to.

        internal GameFont()
        {

        }

        /// <summary>
        /// Measures the size of the string with no wrapping, using the current font size.
        /// </summary>
        /// <param name="text">The single line of text to measure.</param>
        /// <returns>The size, in pixels, that the font occupies.</returns>
        public Vector2 MeasureString(string text)
        {
            return font.MeasureString(text);
        }

        public Rectangle GetBounds(Vector2 pos, string text)
        {
            var rect = font.GetTextBounds(pos, text);
            rect.X += drawOffset.X;
            rect.Y += drawOffset.Y;
            return rect;
        }

        /// <summary>
        /// Changes the size of the font. Changing font size often requires regeneration of textures,
        /// which is very slow so should be avoided unless it's for a very good reason.
        /// </summary>
        /// <param name="size">The font size, in pixels.</param>
        public void SetSize(float size)
        {
            if (font.Size != size)
            {
                font.Size = size;

                var bounds = font.GetTextBounds(Vector2.Zero, "Example");
                drawOffset = bounds.Location;
                drawOffset.X = -drawOffset.X;
                drawOffset.Y = -drawOffset.Y;
            }
        }

        public void Dispose()
        {
            font?.Reset();
            font = null;
        }
    }

    /// <summary>
    /// Utility class that adds extensions to the spritebatch class to allow drawing strings using GameFont.
    /// </summary>
    public static class GFE
    {
        public static void DrawString(this SpriteBatch spr, GameFont gf, string text, Vector2 position, Color color)
        {
            if (gf == null)
                throw new ArgumentNullException(nameof(gf));

            // URGTODO adjust location based on bounds offset.

            spr.DrawString(gf.font, text, position + gf.drawOffset, color);
        }
    }
}
