using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RazeContent;
using System;
using System.Collections.Generic;
using System.Text;

namespace RazeUI
{
    public static class TextUtils
    {
        /// <summary>
        /// Gets the current culture-sensitive Default alignment. See <see cref="TextAlignment.Default"/>.
        /// </summary>
        public static TextAlignment DefaultAlignment { get { return TextAlignment.Left; } }

        public static void DrawLines(SpriteBatch spr, GameFont font, string paragraph, Vector2 topLeft, TextAlignment alignment, Color color)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
                return;

            string[] lines = paragraph.Split('\n');
            DrawLines(spr, font, lines, topLeft, alignment, color);
        }

        public static void DrawLines(SpriteBatch spr, GameFont font, IReadOnlyList<string> linesOfText, Vector2 topLeft, TextAlignment alignment, Color color)
        {
            if (spr == null)
                throw new ArgumentNullException(nameof(spr));
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            if (linesOfText == null || linesOfText.Count == 0)
                return; // Should this throw exception?

            if (alignment == TextAlignment.Default)
                alignment = DefaultAlignment;

            float spacing = GetSpacing(font);

            switch (alignment)
            {
                case TextAlignment.Left:
                    // Position is the top left corner.
                    for (int i = 0; i < linesOfText.Count; i++)
                    {
                        string line = linesOfText[i];
                        if (line == null)
                            continue;

                        Vector2 offset = new Vector2(0f, i * spacing);

                        spr.DrawString(font, line, topLeft + offset, color, 1f);                       
                    }

                    break;

                case TextAlignment.Right:
                    // Position is the top left corner.

                    var measured = MeasureLines(font, linesOfText, alignment);
                    float w = measured.X;

                    for (int i = 0; i < linesOfText.Count; i++)
                    {
                        string line = linesOfText[i];
                        if (line == null)
                            continue;

                        Point size = font.MeasureString(line);
                        Vector2 offset = new Vector2(w - size.X, i * spacing);

                        spr.DrawString(font, line, topLeft + offset, color, 1f);
                    }

                    break;

                case TextAlignment.Centered:
                    // Position is the top left corner

                    // Need to measure width to know where to draw.
                    measured = MeasureLines(font, linesOfText, alignment);
                    w = measured.X;

                    for (int i = 0; i < linesOfText.Count; i++)
                    {
                        string line = linesOfText[i];
                        if (line == null)
                            continue;

                        Point size = font.MeasureString(line);
                        Vector2 offset = new Vector2((w - size.X) * 0.5f, i * spacing);

                        spr.DrawString(font, line, topLeft + offset, color, 1f);
                    }

                    break;

                default:
                    return;
            }
        }

        public static Point MeasureLines(GameFont font, string paragraph, TextAlignment alignment)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
                return Point.Zero;

            string[] lines = paragraph.Split('\n');
            return MeasureLines(font, lines, alignment);
        }

        public static Point MeasureLines(GameFont font, IReadOnlyList<string> linesOfText, TextAlignment alignment)
        {
            if (alignment == TextAlignment.Default)
                alignment = DefaultAlignment;

            if (font == null)
                throw new ArgumentNullException(nameof(font));

            if (linesOfText == null || linesOfText.Count == 0)
                return Point.Zero; // Should this throw exception?

            float spacing = GetSpacing(font);

            switch (alignment)
            {
                case TextAlignment.Left:
                case TextAlignment.Right:
                case TextAlignment.Centered:

                    // Left right and centered are actually calculated the same way for now, but ill leave the parameter in there just in case.
                    // The width will be that of the longest line, and the height will be dictated by the number of lines and font size.

                    // There are two options: measure each string (slow) or only measure the longest (character wise) string, and use that for width.
                    // I have taken the first approach, because although it is slower it provides perfect results, whereas measuring the longest
                    // string will often give incorrect results because some characters are wider than others.

                    float width = 0f;
                    float lastHeight = 0f;
                    foreach (string line in linesOfText)
                    {
                        if (line == null)
                            continue;

                        Point size = font.MeasureString(line);
                        if (size.X > width)
                            width = size.X;

                        lastHeight = size.Y;
                    }

                    float height = (linesOfText.Count - 1) * spacing;
                    height += Math.Min(lastHeight, spacing);

                    return new Point((int) width, (int) height);

                default:
                    return Point.Zero;
            }
        }

        private static List<string> toWrap = new List<string>();
        private static StringBuilder str = new StringBuilder();
        public static IReadOnlyList<string> WrapLines(GameFont font, IReadOnlyList<string> longLines, float width, bool fast = false, TextWrapMode mode = TextWrapMode.KeepWords)
        {
            toWrap.Clear();

            if (longLines == null || longLines.Count == 0)
                return toWrap;

            if (width <= 0)
                return toWrap;

            switch (mode)
            {
                case TextWrapMode.KeepWords:

                    foreach (var longLine in longLines)
                    {
                        var words = SplitIntoWords(longLine);

                        if (words.Count == 0)
                        {
                            toWrap.Add("");
                            continue;
                        }

                        str.Clear();
                        int wordsInLine = 0;
                        float size = 0f;
                        for (int i = 0; i < words.Count; i++)
                        {
                            string word = words[i];
                            str.Append(word);
                            str.Append(' ');

                            if (!fast)
                            {
                                // Calculate size, the fancy (and slow) way.
                                size = font.MeasureString(str.ToString()).X;
                            }
                            else
                            {
                                // Calculate approximate size, the fast(er) way. Does not produce very accurate results, because kerning is not fully calculated.
                                size += font.MeasureString(word + " ").X;
                            }
                            if(size > width)
                            {
                                // Remove the last word, as it would go over the limit, unless there is only 1 word in line.
                                if(wordsInLine != 0)
                                    str.Remove(str.Length - (word.Length + 1), word.Length + 1);

                                // This line is complete!
                                toWrap.Add(str.ToString().TrimEnd());
                                str.Clear();
                                size = 0f;

                                // If we removed this last word, add it to the next line.
                                if (wordsInLine != 0)
                                {
                                    wordsInLine = 1;
                                    str.Append(word);
                                    str.Append(' ');

                                    if (fast)
                                        size = font.MeasureString(str.ToString()).X;
                                }
                                else
                                {
                                    // Reset for the next line.
                                    wordsInLine = 0;
                                }
                            }
                            else
                            {
                                wordsInLine++;
                            }

                            if (i == words.Count - 1)
                            {
                                // Add the existing line or it won't be included.
                                toWrap.Add(str.ToString().TrimEnd());
                                str.Clear();
                            }
                        }
                    }
                    return toWrap;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Mode not implemented yet.");
            }
        }

        //private static List<string> tempWords = new List<string>();
        private static readonly char[] splitChars = new char[] { ' ', '\t' };
        private static IReadOnlyList<string> SplitIntoWords(string line)
        {
            // Assumes that input data is valid (not null and has at least one word)
            return line.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
        }

        private static float GetSpacing(GameFont font)
        {
            return font.Size * font.VerticalSpacingMultiplier;
        }

        public enum TextWrapMode
        {
            /// <summary>
            /// Keeps words intact.
            /// </summary>
            KeepWords
        }
    }
}
