using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RazeContent
{
    public class GameFont : IDisposable
    {
        /// <summary>
        /// Gets or sets the font size.
        /// The setter is equivalent to calling <see cref="SetSize(int)"/>.
        /// </summary>
        public int Size
        {
            get
            {
                return font?.Size ?? 0;
            }
            set
            {
                const int MIN_SIZE = 1;
                SetSize(value >= MIN_SIZE ? value : MIN_SIZE);
            }
        }

        /// <summary>
        /// Should this font use kerning data? It looks better :) You really should. Don't be like Monogame.
        /// </summary>
        public bool UseKerning
        {
            get
            {
                return font?.UseKernings ?? false;
            }
            set
            {
                if (font != null)
                    font.UseKernings = value;
            }
        }

        /// <summary>
        /// The default character code to use when a character is not available in this font.
        /// If null, and a character is unable to be rendered or measured, an exception will be thrown.
        /// </summary>
        public int? DefaultCharacterCode
        {
            get
            {
                return font?.DefaultCharacter;
            }
            set
            {
                if (font != null)
                    font.DefaultCharacter = value;
            }
        }

        /// <summary>
        /// The additional spacing between characters to use when drawing.
        /// Normally defaults to zero, but can be changed to squash characters closer together or move
        /// them further apart.
        /// </summary>
        public float Spacing
        {
            get
            {
                return font?.Spacing ?? 0f;
            }
            set
            {
                if (font != null)
                    font.Spacing = value;
            }
        }

        /// <summary>
        /// The multiplier to use for vertical separation when drawing multiple lines of text. Defaults to 1,
        /// but must be adjusted manually (for now) for each font to achieve the desired look.
        /// </summary>
        public float VerticalSpacingMultiplier { get; set; } = 1f;

        /// <summary>
        /// Gets the number of textures that the font has in memory at the moment.
        /// </summary>
        public int TextureAtlasCount { get { return font?.Textures.Count() ?? 0; } }

        internal DynamicSpriteFont font;
        internal Point drawOffset; // The offset required to get the top-left corner actually drawing where you want it to.

        internal GameFont(DynamicSpriteFont font)
        {
            this.font = font;
            UpdateOffset();
        }

        /// <summary>
        /// Measures the size of the string with no wrapping, using the current font size.
        /// </summary>
        /// <param name="text">The single line of text to measure.</param>
        /// <returns>The size, in pixels, that the font occupies.</returns>
        public Point MeasureString(string text)
        {
            if (text == null)
                return Point.Zero;

            return font.GetTextBounds(Vector2.Zero, text).Size;
        }

        /// <summary>
        /// Changes the size of the font. Changing font size often requires regeneration of textures,
        /// which is very slow so should be avoided unless it's for a very good reason.
        /// </summary>
        /// <param name="size">The font size, in pixels.</param>
        public void SetSize(int size)
        {
            if (font != null && font.Size != size)
            {
                font.Size = size;

                UpdateOffset();
            }
        }

        /// <summary>
        /// Adds a ttf file's data to this font, allowing multiple font files to be merged into one renderable font.
        /// </summary>
        /// <param name="data">The ttf file data. Could be loaded using <c>File.ReadAllBytes(path);</c> for example.</param>
        public void AddTtf(byte[] data)
        {
            font.AddTtf(data);
        }

        public IEnumerable<Texture2D> EnumerateTextureAtlases()
        {
            foreach (var tex in font.Textures)
            {
                if (tex != null && !tex.IsDisposed)
                    yield return tex;
            }
        }

        private void UpdateOffset()
        {
            var bounds = font.GetTextBounds(Vector2.Zero, "TILAWlweyYgG");
            drawOffset = bounds.Location;
            drawOffset.X = -drawOffset.X;
            drawOffset.Y = -drawOffset.Y;
        }

        public void Clean()
        {
            font.Reset();
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
        public static void DrawString(this SpriteBatch spr, GameFont gf, string text, Vector2 position, Color color, float scale = 1f)
        {
            if (gf == null)
                throw new ArgumentNullException(nameof(gf));

            spr.DrawString(gf.font, text, position + gf.drawOffset.ToVector2(), color, Vector2.One * scale);
        }
    }
}
