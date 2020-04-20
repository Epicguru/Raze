using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using RazeContent;
using RazeUI.Handles;
using RazeUI.Windows;

namespace RazeUI
{
    public class LayoutUserInterface : IDisposable
    {
        internal static LayoutUserInterface Instance { get; private set; }

        public event Action<LayoutUserInterface> DrawUI;

        public UserInterface IMGUI { get; private set; }
        /// <summary>
        /// Shortcut to <c>IMGUI.ActiveTint</c>
        /// </summary>
        public Color ActiveTint
        {
            get
            {
                return IMGUI.ActiveTint;
            }
            set
            {
                IMGUI.ActiveTint = value;
            }
        }
        public Anchor Anchor { get; set; }
        public Rectangle LastDrawnBounds
        {
            get
            {
                return contexts.Count == 0 ? default : contexts[^1].LastDrawn;
            }
        }

        public int VerticalPadding
        {
            get
            {
                return (int)(verticalPadding * Scale);
            }
            set
            {
                verticalPadding = (int) (value / scale);
            }
        }
        public int HorizontalPadding
        {
            get
            {
                return (int)(horizontalPadding * Scale);
            }
            set
            {
                horizontalPadding = (int)(value / scale);
            }
        }
        public float Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = Math.Max(value, 0.05f);
                IMGUI.Scale = scale;
            }
        }
        public int DefaultFontSize { get; set; } = 32;
        public int FontSize
        {
            get
            {
                return (int)Math.Round(IMGUI.Font.Size / Scale);
            }
            set
            {
                IMGUI.Font.Size = (int)Math.Round(value * Scale);
            }
        }
        public bool ExpandWidth { get; set; }

        private float scale = 1f;
        private int horizontalPadding = 10;
        private int verticalPadding = 10;
        private List<UIContext> contexts = new List<UIContext>();
        private List<Window> windowStack = new List<Window>();

        public LayoutUserInterface(UserInterface ui)
        {
            IMGUI = ui;
            ui.soloRender = false;
            if(Instance == null)
                Instance = this;
        }

        public void Draw()
        {
            if(IMGUI.GraphicsDevice.PresentationParameters.RenderTargetUsage != RenderTargetUsage.PreserveContents)
            {
                IMGUI.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
                //throw new Exception("In order to render UI, GraphicsDevice.PresentationParameters.RenderTargetUsage must be RenderTargetUsage.PreserveContents.");
            }

            FontSize = DefaultFontSize;

            Rectangle screenBounds = new Rectangle(0, 0, IMGUI.ScreenProvider.GetWidth(), IMGUI.ScreenProvider.GetHeight());
            MarginData margins = new MarginData(10, 10, 10, 10);
            contexts.Clear();
            NewContext(screenBounds, margins, Anchor.Vertical, false);

            // Start frame.
            IMGUI.StartDraw();

            // Draw self.
            IMGUI.SpriteBatch.Begin();
            DrawUI?.Invoke(this);
            DrawWindows();
            IMGUI.SpriteBatch.End();

            // Draw base UI, in case someone is using basic UI mode.
            IMGUI.Draw();

            // End frame.
            IMGUI.FinishDraw();

            // Clear typed keys in base.
            IMGUI.typedKeys.Clear();
            IMGUI.keysDown.Clear();
            IMGUI.keysUp.Clear();
        }

        public void AddWindow(Window w)
        {
            if (w == null)
                return;
            if (w.wantsToClose)
            {
                w.IsOpen = false;
                return;
            }

            if (!windowStack.Contains(w))
            {
                windowStack.Add(w);
                w.IsOpen = true;
            }
        }

        private void DrawWindows()
        {
            for (int i = 0; i < windowStack.Count; i++)
            {
                var window = windowStack[i];
                if (window.wantsToClose)
                {
                    window.wantsToClose = false;
                    window.IsOpen = false;
                    window.OnClose();
                    windowStack.RemoveAt(i);
                    i--;
                    continue;
                }

                window.InternalDraw(this);
            }
        }

        private Point GetDrawPos(Point size)
        {
            var context = contexts[^1]; // C# is pretty nice.
            Point basePos = context.Bounds.Location;

            switch (Anchor)
            {
                case Anchor.Vertical:
                    // Go to the height of below the lowest drawn item, but align to the left margin.
                    return basePos + new Point(context.Margins.Left, context.LowestY + (context.LastDrawn == Rectangle.Empty ? 0 : VerticalPadding));

                case Anchor.Horizontal:
                    // Go to the height of the last drawn item, and to it's right edge.
                    return basePos + new Point(context.LastDrawn.X + context.LastDrawn.Width + HorizontalPadding, context.LastDrawn.Y);

                case Anchor.Centered:
                    // Go to the bottom of the last drawn item, but center relative to the parent bounds.
                    int betweenMargins = context.Bounds.Width - context.Margins.Left - context.Margins.Right;
                    if(ExpandWidth)
                        return basePos + new Point(context.Margins.Left, context.LastDrawn.Y + context.LastDrawn.Height + VerticalPadding);

                    return basePos + new Point(context.Margins.Left + betweenMargins / 2 - size.X / 2, context.LastDrawn.Y + context.LastDrawn.Height + VerticalPadding);

                default:
                    throw new ArgumentOutOfRangeException(nameof(Anchor), Anchor, "Something went very, very wrong.");
            }
        }

        private int GetExpandedWidth(Point position)
        {
            var innerBounds = GetContextInnerBounds();
            int leftOffset = position.X - innerBounds.X;
            switch (Anchor)
            {
                case Anchor.Vertical:
                    return innerBounds.Width;
                case Anchor.Horizontal:
                    return innerBounds.Width - leftOffset;
                case Anchor.Centered:
                    return innerBounds.Width;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AdvanceContext(Rectangle screenBounds)
        {
            var context = contexts[^1];

            // Convert screen bounds to local bounds.
            Rectangle localBounds = new Rectangle(screenBounds.Location - context.Bounds.Location, screenBounds.Size);

            context.LastDrawn = localBounds;

            contexts[^1] = context;
        }

        /// <summary>
        /// Starts a new UI layout context. A context can be thought of as a container that UI elements are placed within.
        /// The default context occupies the entire screen, but a new context could be a panel in which buttons and labels are placed, for example.
        /// Normally you won't have to call this method directly, because other methods such as <see cref="PanelContext(Point, PanelType, bool)"/> do this automatically.
        /// However, you will need to call <see cref="EndContext"/> manually because the context is not ended until you tell it to.
        /// </summary>
        /// <param name="screenBounds">The screen bounds of this new context.</param>
        /// <param name="margins">The inner margins to use in this context. Elements placed inside this context should respect these margins.</param>
        /// <param name="anchor">The new anchor to use within this context. Can be changed at any time using <see cref="Anchor"/>.</param>
        /// <param name="expandWidth">When true the context is initialized with ExpandWidth enabled. Can be disabled and re-enabled at any time.</param>
        public void NewContext(Rectangle screenBounds, MarginData margins, Anchor anchor, bool expandWidth)
        {
            if (contexts.Count > 0)
            {
                var old = contexts[^1];
                old.LastAnchor = this.Anchor;
                old.LastFontSize = this.FontSize;
                old.LastExpandWidth = ExpandWidth;
                contexts[^1] = old;
            }

            var context = new UIContext();
            context.Bounds = screenBounds;
            context.Margins = IMGUI.ApplyScale(margins);
            context.LowestY = context.Margins.Top;
            contexts.Add(context);

            this.Anchor = anchor;
            this.FontSize = DefaultFontSize;
            this.ExpandWidth = expandWidth;
        }

        /// <summary>
        /// Ends the current context and goes back to the previous one. Once a context has been ended like this, it cannot be returned to.
        /// See <see cref="NewContext"/>.
        /// </summary>
        public void EndContext()
        {
            if (contexts.Count <= 1)
                return;

            contexts.RemoveAt(contexts.Count - 1);

            // Restore old context settings.
            var restored = contexts[^1];
            this.Anchor = restored.LastAnchor;
            this.FontSize = restored.LastFontSize;
            this.ExpandWidth = restored.LastExpandWidth;
        }

        /// <summary>
        /// Gets the screen bounds of the currently active context. Does not account
        /// for margins, to do that use <see cref="GetContextInnerBounds"/>.
        /// See <see cref="NewContext"/> and <see cref="EndContext"/>.
        /// </summary>
        public Rectangle GetContextBounds()
        {
            return contexts.Count > 0 ? contexts[^1].Bounds : default;
        }

        /// <summary>
        /// Gets the screen-space inner bounds of the currently active context.
        /// This is the area that it is safe to draw in, because it accounts for margins.
        /// See <see cref="NewContext"/> and <see cref="EndContext"/>.
        /// </summary>
        public Rectangle GetContextInnerBounds()
        {
            if (contexts.Count == 0)
                return default;

            var bounds = contexts[^1].Bounds;
            var margins = contexts[^1].Margins;

            return new Rectangle(bounds.Location + new Point(margins.Left, margins.Top), bounds.Size - new Point(margins.Left + margins.Right, margins.Top + margins.Bottom));
        }

        /// <summary>
        /// Gets the margin data for the currently active context. The measurements are in logical pixels, which may not correspond to physical pixels
        /// if the UI scale is not 1.
        /// </summary>
        public MarginData GetContextMargins()
        {
            return contexts.Count > 0 ? contexts[^1].Margins : default;
        }

        /// <summary>
        /// Measures the size of text if rendered using the current font size. It does support measuring text with multiple lines (i.e. with the \n character),
        /// but for that purpose it is better to use <see cref="MeasureText(string, TextAlignment)"/>.
        /// Same as calling <see cref="UserInterface.MeasureString(string)"/>.
        /// </summary>
        /// <param name="text">The input text. Must not be null.</param>
        /// <returns>The size, in pixels, of the text if it were rendered on the screen using the current font settings.</returns>
        public Point MeasureString(string text)
        {
            return IMGUI.MeasureString(text);
        }

        /// <summary>
        /// Measures the size of text if rendered using the current font size and a given alignment.
        /// This call is rather expensive, so if it is needed repeatedly, consider using <see cref="MeasureText(IReadOnlyList{string}, TextAlignment)"/> instead.
        /// Same as calling <see cref="UserInterface.MeasureText(string, TextAlignment)"/>.
        /// </summary>
        /// <param name="text">The input text. Must not be null.</param>
        /// <param name="alignment">The text alignment that it would be rendered using. Normally does not actually change text bounds.</param>
        /// <returns>The size, in pixels, of the text if it were rendered on the screen using the current font and alignment settings.</returns>
        public Point MeasureText(string text, TextAlignment alignment = TextAlignment.Default)
        { 
            return IMGUI.MeasureText(text, alignment);
        }

        /// <summary>
        /// Measures the size of text if rendered using the current font size and a given alignment.
        /// Same as calling <see cref="UserInterface.MeasureText(IReadOnlyList{string}, TextAlignment)"/>.
        /// </summary>
        /// <param name="lines">The lines of input text. Must not be null.</param>
        /// <param name="alignment">The text alignment that it would be rendered using. Normally does not actually change text bounds.</param>
        /// <returns>The size, in pixels, of the text if it were rendered on the screen using the current font and alignment settings.</returns>
        public Point MeasureText(IReadOnlyList<string> lines, TextAlignment alignment = TextAlignment.Default)
        {
            return IMGUI.MeasureText(lines, alignment);
        }

        #region Buttons

        /// <summary>
        /// Places a new button in the current context, with a text label. The size of the label depends on the font size and the text length.
        /// Returns true when left clicked.
        /// </summary>
        /// <param name="text">The text to draw as the button label. If null, a 20x20 button is drawn.</param>
        /// <param name="sizeOffset">The optional size offset to use. By default, the button size will match the text size, but it can be adjusted by specifying a value here.</param>
        /// <returns>True if left clicked this frame.</returns>
        public bool Button(string text, Point? sizeOffset = null)
        {
            this.Button(text, out bool clickLeft, out bool _, sizeOffset);
            return clickLeft;
        }

        /// <summary>
        /// Places a new button in the current context, with a text label. The size of the label depends on the font size and the text length, and
        /// can be changed using the <c>sizeOffset</c> parameter.
        /// </summary>
        /// <param name="text">The text to draw as the button label. If null, a 20x20 button is drawn.</param>
        /// <param name="leftClick">True if left clicked this frame.</param>
        /// <param name="rightClick">True if right clicked this frame.</param>
        /// <param name="sizeOffset">The optional size offset to use. By default, the button size will match the text size, but it can be adjusted by specifying a value here.</param>
        public void Button(string text, out bool leftClick, out bool rightClick, Point? sizeOffset = null)
        {
            Point size = IMGUI.GetButtonSize(text);
            if (sizeOffset != null)
            {
                sizeOffset = IMGUI.ApplyScale(sizeOffset.Value);
                size += sizeOffset.Value;
            }
            
            Point pos = GetDrawPos(size);
            if (ExpandWidth)
            {
                size.X = GetExpandedWidth(pos);
                size.X += sizeOffset?.X ?? 0;
            }
            
            Rectangle bounds = new Rectangle(pos, size);

            IMGUI.Button(bounds, text, out leftClick, out rightClick);

            AdvanceContext(bounds);
        }

        #endregion

        #region Panels

        /// <summary>
        /// Places a new panel and switches context to it. The panel is placed within the current context.
        /// </summary>
        /// <param name="size">The size, in pixels, of the panel.</param>
        /// <param name="type">The style of panel to draw.</param>
        /// <param name="expandWidth">When true the panel expands it's width to match it's parent context, and the <c>size.X</c> component acts as a size offset.</param>
        /// <param name="margins">The optional custom interior margins to use for the new context. If null, margins will be (10, 10, 10, 10).</param>
        public void PanelContext(Point size, PanelType type = PanelType.Default, bool expandWidth = false, MarginData? margins = null)
        {
            size = IMGUI.ApplyScale(size);
            var pos = GetDrawPos(size);
            if (expandWidth)
            {
                int w = GetExpandedWidth(pos);
                w += IMGUI.ApplyScale(size.X);
                size.X = w;
            }
            
            var bounds = new Rectangle(pos, size);

            IMGUI.Panel(bounds, Color.White, out bool _, out bool _, type, false);

            AdvanceContext(bounds);

            if (margins == null)
                margins = new MarginData(10, 10, 10, 10);

            NewContext(bounds, margins.Value, Anchor.Vertical, true);
        }

        /// <summary>
        /// Places a new panel and switches context to it. The panel does NOT affect the current context.
        /// </summary>
        /// <param name="bounds">The screen bounds of the panel.</param>
        /// <param name="type">The style of panel to draw.</param>
        /// <param name="margins">The optional custom interior margins to use for the new context. If null, margins will be (10, 10, 10, 10).</param>
        public void PanelContext(Rectangle bounds, PanelType type = PanelType.Default, MarginData ? margins = null)
        {
            IMGUI.Panel(bounds, Color.White, out bool _, out bool _, type, false);

            if (margins == null)
                margins = new MarginData(10, 10, 10, 10);

            NewContext(bounds, margins.Value, Anchor.Vertical, true);
        }

        #endregion

        #region Checkboxes

        /// <summary>
        /// Draws a checkbox, also know as a toggle.
        /// A checkbox can be either checked or unchecked, and by clicking on it the user changes the checked state.
        /// The checked state must be stored by the calling code via the <c>isChecked</c> ref parameter.
        /// </summary>
        /// <param name="label">The text to draw next to the toggle box. Can be null or blank to just draw the box.</param>
        /// <param name="isChecked">Stores and controls the checked state of this toggle.</param>
        /// <returns>True of the frame that the user changes the checked state by clicking on this toggle.</returns>
        public bool Checkbox(string label, ref bool isChecked)
        {
            // Ignores expand width, because it doesn't make any sense here.
            Point labelSize = string.IsNullOrWhiteSpace(label) ? Point.Zero : IMGUI.Font.MeasureString(label);
            int sep = IMGUI.ApplyScale(10);
            Point boxSize = IMGUI.ApplyScale(new Point(32, 32));

            Point totalSize = new Point(boxSize.X + sep + labelSize.X, labelSize.Y);
            Point pos = GetDrawPos(totalSize);
            Point labelPos = pos + new Point(boxSize.X + sep, boxSize.Y / 2 - labelSize.Y / 2);

            Rectangle labelBounds = new Rectangle(labelPos, labelSize);
            bool mouseOver = IMGUI.IsMouseOver(labelBounds);
            if (mouseOver && IMGUI.MouseProvider.IsLeftMouseClick())
                isChecked = !isChecked;

            Rectangle bounds = new Rectangle(pos, totalSize);

            bool toReturn = IMGUI.Checkbox(pos, boxSize.X, ref isChecked, out Rectangle _, mouseOver);

            if(!string.IsNullOrWhiteSpace(label))
                IMGUI.SpriteBatch.DrawString(IMGUI.Font, label, labelPos.ToVector2(), Color.White);

            AdvanceContext(bounds);

            return toReturn;
        }

        #endregion

        #region Separators

        public void FlatSeparator(int? width = null, int spaceBefore = 5, int spaceAfter = 5)
        {
            bool changedAnchor = false;
            Anchor oldAnchor = default;
            if (Anchor != Anchor.Vertical)
            {
                // TODO issue warning here.
                oldAnchor = Anchor;
                Anchor = Anchor.Vertical;
                changedAnchor = true;
            }

            if (width != null)
                width = IMGUI.ApplyScale(width.Value);

            int boundsWidth = GetContextInnerBounds().Width;
            int realWidth = width ?? boundsWidth;
            Point drawPos = GetDrawPos(new Point(realWidth, 0));
            if (width != null)
            {
                drawPos.X += (boundsWidth - width.Value) / 2;
            }

            IMGUI.FlatSeparator(drawPos, realWidth, Color.White, out Rectangle bounds, spaceBefore, spaceAfter);

            AdvanceContext(bounds);

            if (changedAnchor)
            {
                Anchor = oldAnchor;
            }
        }

        public void FlatSeparator(int? width, Color color, int spaceBefore = 5, int spaceAfter = 5)
        {
            bool changedAnchor = false;
            Anchor oldAnchor = default;
            if(Anchor != Anchor.Vertical)
            {
                oldAnchor = Anchor;
                Anchor = Anchor.Vertical;
                changedAnchor = true;
            }

            if (width != null)
                width = IMGUI.ApplyScale(width.Value);

            int boundsWidth = GetContextInnerBounds().Width;
            int realWidth = width ?? boundsWidth;
            Point drawPos = GetDrawPos(new Point(realWidth, 0));
            if (width != null)
            {
                drawPos.X += (boundsWidth - width.Value) / 2;
            }

            IMGUI.FlatSeparator(drawPos, realWidth, color, out Rectangle bounds, spaceBefore, spaceAfter);

            AdvanceContext(bounds);

            if (changedAnchor)
            {
                Anchor = oldAnchor;
            }
        }

        #endregion

        #region Labels

        public void Label(string text)
        {
            this.Label(text, Color.White);
        }

        public void Label(string text, Color color)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            Point size = IMGUI.Font.MeasureString(text);
            Point pos = GetDrawPos(size);
            IMGUI.Label(pos, text, color, out Rectangle bounds);

            AdvanceContext(bounds);
        }

        #endregion

        #region Text Input

        public void TextBox(TextBoxHandle textBox, Point size)
        {
            if (textBox == null)
                return;

            if (size.X <= 0 || size.Y <= 0)
                return;

            size = IMGUI.ApplyScale(size);
            Point pos = GetDrawPos(size);
            if (ExpandWidth)
            {
                size.X = GetExpandedWidth(pos);
            }

            Rectangle bounds = new Rectangle(pos, size);
            IMGUI.TextBoxMasked(bounds, textBox);

            AdvanceContext(bounds);
        }

        #endregion

        #region Paragraphs

        public Rectangle Paragraph(string text, Color color)
        {
            return Paragraph(text, null, TextAlignment.Default, color);
        }

        public Rectangle Paragraph(string text, float? width, Color color)
        {
            return Paragraph(text, width, TextAlignment.Default, color);
        }

        public Rectangle Paragraph(string text, TextAlignment alignment, Color color)
        {
            return Paragraph(text, null, alignment, color);
        }

        public Rectangle Paragraph(string text, float? width, TextAlignment alignment, Color color)
        {
            if (string.IsNullOrWhiteSpace(text))
                return default;

            if (width != null)
                width = IMGUI.ApplyScale(width.Value);

            if (width != null && width <= 0)
                return default;

            int boundsWidth = GetContextInnerBounds().Width;
            float realWidth = width ?? boundsWidth;

            var formattedLines = TextUtils.WrapLines(IMGUI.Font, text.Split('\n'), realWidth);
            Point size = TextUtils.MeasureLines(IMGUI.Font, formattedLines, alignment);

            bool expand = this.ExpandWidth;
            ExpandWidth = false;
            var pos = GetDrawPos(size);
            ExpandWidth = expand;

            var bounds = IMGUI.Paragraph(formattedLines, pos.ToVector2(), alignment, color, size);

            AdvanceContext(bounds);

            return bounds;
        }

        #endregion

        public void Dispose()
        {
            if (Instance == this)
                Instance = null;
            IMGUI?.Dispose();
            IMGUI = null;
        }
    }
}
