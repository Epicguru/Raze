using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RazeContent;
using RazeUI.Handles;
using RazeUI.Providers;
using RazeUI.UISprites;
using System;
using System.Collections.Generic;

namespace RazeUI
{
    public class UserInterface : IDisposable
    {
        internal static UserInterface Instance { get; private set; }

        public SpriteBatch SpriteBatch { get; set; }
        public GraphicsDevice GraphicsDevice { get; set; }
        public Color GlobalTint { get; set; } = Color.White;
        public IMouseProvider MouseProvider { get; set; }
        public IKeyboardProvider KeyboardProvider
        {
            get
            {
                return keyProvider;
            }
            set
            {
                if (this.keyProvider != null)
                {
                    this.keyProvider.OnKeyTyped -= OnKeyInput;
                    this.keyProvider.OnKeyDown -= OnKeyDown;
                    this.keyProvider.OnKeyUp -= OnKeyUp;

                }

                this.keyProvider = value;
                if (this.keyProvider != null)
                {
                    this.keyProvider.OnKeyTyped += OnKeyInput;
                    this.keyProvider.OnKeyDown += OnKeyDown;
                    this.keyProvider.OnKeyUp += OnKeyUp;
                }
            }
        }
        public IScreenProvider ScreenProvider { get; set; }
        public IContentProvider ContentProvider { get; set; }
        public Color ActiveTint { get; set; } = Color.White;

        public event Action<UserInterface> DrawUI;

        internal GameFont Font;
        internal float Scale = 1f; // Reason why this is internal is because it actually doesn't work for any methods in this class, it only affects small stuff like button-string margins.
        internal RenderTarget2D rt;
        internal bool soloRender = true;
        internal Queue<(SpecialKey special, Keys key, char character)> typedKeys = new Queue<(SpecialKey, Keys, char)>();
        internal Queue<Keys> keysDown = new Queue<Keys>();
        internal Queue<Keys> keysUp = new Queue<Keys>();
        internal RasterizerState rasterizerState = new RasterizerState() { ScissorTestEnable = true };

        private IKeyboardProvider keyProvider;
        private NinePatch defaultPanel;
        private NinePatch specialPanel;
        private NinePatch transparentPanel;
        private NinePatch inputBox, inputBoxSelected;
        private NinePatch flatSeparator;
        private UISprite checkBox, checkBoxSelected, checkBoxHighlighted, checkBoxSelectedHighlighted;
        private Texture2D pixel;
        private List<string> tempFormattedLines = new List<string>();

        public UserInterface(GraphicsDevice gd, IMouseProvider mouseProvider, IKeyboardProvider keyboardProvider, IScreenProvider screenProvider, IContentProvider contentProvider)
        {
            this.GraphicsDevice = gd;
            this.SpriteBatch = new SpriteBatch(gd);
            this.MouseProvider = mouseProvider;
            this.KeyboardProvider = keyboardProvider;
            this.ScreenProvider = screenProvider;
            this.ContentProvider = contentProvider;

            pixel = new Texture2D(gd, 1, 1);
            pixel.SetData(new Color[] { Color.White });

            if (Instance == null)
                Instance = this;

            LoadContent();
        }

        private void LoadContent()
        {
            defaultPanel = new NinePatch(ContentProvider.LoadSprite("Textures/Panels/Default.png"), new Point(4, 4), new Point(8, 8));
            transparentPanel = new NinePatch(ContentProvider.LoadSprite("Textures/Panels/Transparent.png"), new Point(4, 4), new Point(8, 8));
            specialPanel = new NinePatch(ContentProvider.LoadSprite("Textures/Panels/Special.png"), new Point(5, 5), new Point(6, 6));
            flatSeparator = new NinePatch(ContentProvider.LoadSprite("Textures/Separators/Flat.png"), new Point(12, 1), new Point(40, 1));
            inputBox = new NinePatch(ContentProvider.LoadSprite("Textures/Input Boxes/InputBox.png"), new Point(10, 10), new Point(12, 12));
            inputBoxSelected = new NinePatch(ContentProvider.LoadSprite("Textures/Input Boxes/InputBox-Selected.png"), new Point(10, 10), new Point(12, 12));

            checkBox = ContentProvider.LoadSprite("Textures/Checkboxes/Checkbox.png");
            checkBoxSelected = ContentProvider.LoadSprite("Textures/Checkboxes/Checkbox-Selected.png");
            checkBoxHighlighted = ContentProvider.LoadSprite("Textures/Checkboxes/Checkbox-Highlighted.png");
            checkBoxSelectedHighlighted = ContentProvider.LoadSprite("Textures/Checkboxes/Checkbox-Selected-Highlighted.png");

            Font = ContentProvider.LoadFont("Fonts/Oxygen-Regular.ttf");
            Font.DefaultCharacterCode = '?';
        }

        internal void UpdateRenderTarget()
        {
            int tw = ScreenProvider.GetWidth();
            int th = ScreenProvider.GetHeight();
            if (rt == null || rt.Width != tw || rt.Height != th)
            {
                rt = new RenderTarget2D(GraphicsDevice, tw, th, false, SurfaceFormat.Color, DepthFormat.None);
            }
        }

        private void OnKeyInput(Keys key, char character)
        {
            SpecialKey s = GetSpecialKey(character);

            Console.WriteLine($"{key}: ({(int)character}) [{s}]'{character}'");
            typedKeys.Enqueue((s, key, character));

            static SpecialKey GetSpecialKey(char character)
            {
                int code = character;
                return code switch
                {
                    13 => SpecialKey.NewLine,
                    8 => SpecialKey.Backspace,
                    127 => SpecialKey.Delete,
                    9 => SpecialKey.Tab,
                    _ => SpecialKey.None
                };
            }
        }

        private void OnKeyDown(Keys key)
        {
            keysDown.Enqueue(key);
        }

        private void OnKeyUp(Keys key)
        {
            keysUp.Enqueue(key);
        }

        public void Draw()
        {
            if (soloRender)
            {
                StartDraw();
            }

            SpriteBatch.Begin();
            DrawUI?.Invoke(this);
            SpriteBatch.End();

            if (soloRender)
            {
                FinishDraw();
            }
        }

        internal void StartDraw()
        {
            MouseProvider?.PreDraw();
            UpdateRenderTarget();
            GraphicsDevice.SetRenderTarget(this.rt);
            GraphicsDevice.Clear(Color.Transparent);
            ActiveTint = Color.White;
        }

        internal void FinishDraw()
        {
            GraphicsDevice.SetRenderTarget(null);
            SpriteBatch.Begin(blendState: BlendState.NonPremultiplied);
            SpriteBatch.Draw(rt, Vector2.Zero, GlobalTint);
            SpriteBatch.End();
        }

        internal Point GetButtonSize(string text)
        {
            Point size = new Point(20, 20);
            Point margins = ApplyScale(new Point(10, 10));

            if (text != null)
                size = Font.MeasureString(text) + margins;

            return size;
        }

        /// <summary>
        /// Measures the size of text if rendered using the current font size. It does support measuring text with multiple lines (i.e. with the \n character),
        /// but for that purpose it is better to use <see cref="MeasureText(string, TextAlignment)"/>.
        /// </summary>
        /// <param name="text">The input text. Must not be null.</param>
        /// <returns>The size, in pixels, of the text if it were rendered on the screen using the current font settings.</returns>
        public Point MeasureString(string text)
        {
            return Font.MeasureString(text);
        }

        /// <summary>
        /// Measures the size of text if rendered using the current font size and a given alignment.
        /// This call is rather expensive, so if it is needed repeatedly, consider using <see cref="MeasureText(IReadOnlyList{string}, TextAlignment)"/> instead.
        /// </summary>
        /// <param name="text">The input text. Must not be null.</param>
        /// <param name="alignment">The text alignment that it would be rendered using. Normally does not actually change text bounds.</param>
        /// <returns>The size, in pixels, of the text if it were rendered on the screen using the current font and alignment settings.</returns>
        public Point MeasureText(string text, TextAlignment alignment = TextAlignment.Default)
        {
            return TextUtils.MeasureLines(Font, text, alignment);
        }

        /// <summary>
        /// Measures the size of text if rendered using the current font size and a given alignment.
        /// </summary>
        /// <param name="lines">The lines of input text. Must not be null.</param>
        /// <param name="alignment">The text alignment that it would be rendered using. Normally does not actually change text bounds.</param>
        /// <returns>The size, in pixels, of the text if it were rendered on the screen using the current font and alignment settings.</returns>
        public Point MeasureText(IReadOnlyList<string> lines, TextAlignment alignment = TextAlignment.Default)
        {
            return TextUtils.MeasureLines(Font, lines, alignment);
        }

        #region Buttons

        public bool Button(Point pos, string text)
        {
            this.Button(pos, text, out bool left, out bool _, out Rectangle _);
            return left;
        }

        public void Button(Point pos, string text, out bool leftClick, out bool rightClick, out Rectangle bounds)
        {
            var size = GetButtonSize(text);

            bounds = new Rectangle(pos, size);
            this.Button(bounds, text, out leftClick, out rightClick);
        }

        public bool Button(Rectangle bounds, string text)
        {
            this.Button(bounds, text, out bool left, out bool _);
            return left;
        }

        public void Button(Rectangle bounds, string text, out bool leftClick, out bool rightClick)
        {
            Panel(bounds, ActiveTint, out leftClick, out rightClick, PanelType.Transparent);

            if (text == null)
                return;

            Point size = Font.MeasureString(text);
            Point pos = bounds.Location + new Point((bounds.Width - size.X) / 2, (bounds.Height - size.Y) / 2);

            SpriteBatch.DrawString(Font, text, pos.ToVector2(), Color.White);
        }

        #endregion

        #region Panels

        public void Panel(Rectangle bounds, Color tint, PanelType type = PanelType.Special)
        {
            this.Panel(bounds, tint, out bool _, out bool _, type);
        }

        public void Panel(Rectangle bounds, Color tint, out bool leftClick, out bool rightClick, PanelType type, bool respondToMouseOver = true)
        {
            Color c = tint;

            leftClick = false;
            rightClick = false;
            if (IsMouseOver(bounds))
            {
                if (respondToMouseOver)
                    c = Color.Lerp(c, Color.Black, 0.4f);
                leftClick = MouseProvider.IsLeftMouseClick();
                rightClick = MouseProvider.IsRightMouseClick();
            }

            GetPanelPatch(type).Draw(SpriteBatch, bounds, c);
        }

        #endregion

        #region Checkboxes

        public bool Checkbox(Point pos, ref bool isChecked)
        {
            return this.Checkbox(pos, 32, ref isChecked);
        }

        public bool Checkbox(Point pos, int size, ref bool isChecked)
        {
            return this.Checkbox(pos, size, ref isChecked, out Rectangle _, false);
        }

        public bool Checkbox(Point pos, int size, ref bool isChecked, out Rectangle bounds)
        {
            return this.Checkbox(pos, size, ref isChecked, out bounds, false);
        }

        internal bool Checkbox(Point pos, int size, ref bool isChecked, out Rectangle bounds, bool forceHighlight)
        {
            bool changed = false;
            bounds = new Rectangle(pos, new Point(size, size));
            UISprite sprite = isChecked ? checkBoxSelected : checkBox;

            if (IsMouseOver(bounds))
            {
                if (MouseProvider.IsLeftMouseClick())
                {
                    changed = true;
                    isChecked = !isChecked;
                }
                forceHighlight = true;
            }

            if (forceHighlight)
                sprite = isChecked ? checkBoxSelectedHighlighted : checkBoxHighlighted;

            sprite.Draw(SpriteBatch, bounds, ActiveTint);

            return changed;
        }

        #endregion

        #region Seprarators

        public void FlatSeparator(Point pos, int width, Color color, int spaceBefore = 5, int spaceAfter = 5)
        {
            this.FlatSeparator(pos, width, color, out Rectangle _, spaceBefore, spaceAfter);
        }

        public void FlatSeparator(Point pos, int width, Color color, out Rectangle bounds, int spaceBefore = 5, int spaceAfter = 5)
        {
            Rectangle lineBounds = new Rectangle(pos + new Point(0, spaceBefore), new Point(width, 3));

            flatSeparator.Draw(SpriteBatch, lineBounds, color);

            bounds = new Rectangle(pos, new Point(width, 4 + spaceBefore + spaceAfter));
        }

        #endregion

        #region Labels

        public void Label(Point pos, string text, Color color)
        {
            this.Label(pos, text, color, out Rectangle _);
        }


        public void Label(Point pos, string text, Color color, out Rectangle bounds)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                bounds = Rectangle.Empty;
                return;
            }

            Point size = Font.MeasureString(text);
            SpriteBatch.DrawString(Font, text, pos.ToVector2(), color);

            bounds = new Rectangle(pos, size);
        }

        #endregion

        #region Paragraphs

        public Rectangle Paragraph(string text, Vector2 position, float width, TextAlignment alignment, Color color)
        {
            if (string.IsNullOrWhiteSpace(text))
                return default;

            return Paragraph(text.Split('\n'), position, width, alignment, color);
        }

        public Rectangle Paragraph(IReadOnlyList<string> rawLines, Vector2 position, float width, TextAlignment alignment, Color color)
        {
            if (rawLines == null)
                return default;

            TextUtils.WrapLines(Font, rawLines, tempFormattedLines, width);
            Point size = TextUtils.MeasureLines(Font, tempFormattedLines, alignment);

            return Paragraph(tempFormattedLines, position, alignment, color, size);
        }

        public Rectangle Paragraph(IReadOnlyList<string> formattedLines, Vector2 position, TextAlignment alignment, Color color, Point? linesSize = null)
        {
            if (formattedLines == null)
                return default;

            return TextUtils.DrawLines(SpriteBatch, Font, formattedLines, position, alignment, color, linesSize);
        }

        #endregion

        #region Text Input

        public void TextBoxMasked(Rectangle bounds, TextBoxHandle handle)
        {
            this.RenderRegion(bounds, () =>
            {
                this.TextBox(bounds, handle);
            });
        }

        public void TextBox(Rectangle bounds, TextBoxHandle handle)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            Point margins = ApplyScale(new Point(5, 5));
            Point textPos = bounds.Location + margins;

            // Handle selecting and deselecting
            bool click = MouseProvider.IsLeftMouseClick() || MouseProvider.IsRightMouseClick();
            handle.IsMouseOver = IsMouseOver(bounds);
            if (click)
            {
                bool wasSelected = handle.IsSelected;
                handle.IsSelected = handle.IsMouseOver;
                if (handle.IsSelected && !wasSelected)
                {
                    handle.CaretPosition = handle.Text.Length;
                }
            }

            // Change sprite based on selected state.
            (handle.IsSelected ? inputBoxSelected : inputBox).Draw(SpriteBatch, bounds, ActiveTint);

            // Take input if selected.
            if (handle.IsSelected)
            {
                while (typedKeys.Count > 0)
                {
                    var data = typedKeys.Dequeue();
                    bool canAddAnotherCharacter = handle.MaxCharacters <= 0 || handle.Text.Length < handle.MaxCharacters;
                    if (data.special == SpecialKey.None)
                    {
                        if (canAddAnotherCharacter && handle.IsValid(data.character))
                        {
                            string newText = handle.Text.Insert(handle.CaretPosition, data.character.ToString());
                            if (handle.IsValid(ref newText))
                            {
                                handle.Text = newText;
                                handle.CaretPosition++;
                            }
                        }
                    }
                    else
                    {
                        switch (data.special)
                        {
                            case SpecialKey.Tab:
                                // Add 4 spaces? Maybe? Ignore for now.
                                break;

                            case SpecialKey.Backspace:
                                // Delete the character before caret
                                if (handle.Text.Length > 0 && !handle.IsCaretAtStart)
                                {
                                    string newText = handle.Text.Remove(handle.CaretPosition - 1, 1);
                                    if (handle.IsValid(ref newText))
                                    {
                                        bool moveBack = !handle.IsCaretAtEnd;
                                        handle.Text = newText;
                                        if(moveBack)
                                            handle.CaretPosition--;
                                    }
                                }
                                break;

                            case SpecialKey.Delete:
                                // Delete the character after caret.
                                if (handle.Text.Length > 0 && !handle.IsCaretAtEnd)
                                {
                                    string newText = handle.Text.Remove(handle.CaretPosition, 1);
                                    if (handle.IsValid(ref newText))
                                    {
                                        handle.Text = newText;
                                    }
                                }
                                break;

                            case SpecialKey.NewLine:
                                if (handle.AllowMultiLine && canAddAnotherCharacter && handle.IsValid('\n'))
                                {
                                    string newText = handle.Text.Insert(handle.CaretPosition, "\n");
                                    if (handle.IsValid(ref newText))
                                    {
                                        handle.Text = newText;
                                        handle.CaretPosition++;
                                    }
                                }
                                    
                                break;
                        }
                    }
                }

                while (keysDown.Count > 0)
                {
                    var key = keysDown.Dequeue();
                    switch (key)
                    {
                        case Keys.Left:
                            handle.CaretPosition--;
                            break;
                        case Keys.Right:
                            handle.CaretPosition++;
                            break;
                    }
                }
            }

            // Draw text, or text hint if there is no text.
            bool hasText = !string.IsNullOrEmpty(handle.Text);
            bool drawHint = !hasText && !string.IsNullOrWhiteSpace(handle.HintText);
            if (drawHint)
            {
                var oldSize = Font.Size;
                Font.Size = (int)(Font.Size * 0.7f);
                SpriteBatch.DrawString(Font, handle.HintText, textPos.ToVector2() + new Vector2(0, bounds.Height * 0.15f), Color.Gray);
                Font.Size = oldSize;
            }

            if (hasText)
            {
                TextUtils.DrawLines(SpriteBatch, Font, handle.GetLines(), textPos.ToVector2(), TextAlignment.Default, Color.Black);
                //SpriteBatch.DrawString(Font, handle.Text, textPos.ToVector2(), Color.Red);
            }

            // Draw caret
            if (handle.IsSelected)
            {
                int caretPos = handle.CaretPosition;
                int caretLine = handle.GetCaretLine(out int lineStartIndex);
                string caretTextLine = handle.GetLines()[caretLine];
                string lineUpToCaret = caretTextLine.Substring(0, caretPos - lineStartIndex);

                float offset = Font.MeasureString(lineUpToCaret).X;
                Rectangle caretBounds = new Rectangle(bounds.X + 4 + (int)offset, bounds.Y + Font.Size * caretLine, 2, Font.Size);
                SpriteBatch.Draw(pixel, caretBounds, null, Color.Black);
            }
        }

        #endregion

        internal void RenderRegion(Rectangle screenBounds, Action render, Color? c = null)
        {
            if (render == null)
                return;
            if (screenBounds.Width <= 0 || screenBounds.Height <= 0)
                return;

            SpriteBatch.End();

            //Set up the spritebatch to draw using scissoring (for text cropping)
            SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                null, null, rasterizerState);

            //Copy the current scissor rect so we can restore it after
            Rectangle currentRect = SpriteBatch.GraphicsDevice.ScissorRectangle;

            //Set the current scissor rectangle
            SpriteBatch.GraphicsDevice.ScissorRectangle = screenBounds;

            // Draw!
            render.Invoke();

            //End the spritebatch
            SpriteBatch.End();

            //Reset scissor rectangle to the saved value
            SpriteBatch.GraphicsDevice.ScissorRectangle = currentRect;

            SpriteBatch.Begin();
        }

        public virtual bool IsMouseOver(Rectangle bounds)
        {
            return bounds.Contains(MouseProvider.GetMousePos());
        }

        private NinePatch GetPanelPatch(PanelType type)
        {
            return type switch
            {
                PanelType.Default => defaultPanel,
                PanelType.Special => specialPanel,
                PanelType.Transparent => transparentPanel,
                _ => throw new NotImplementedException()
            };
        }

        public int ApplyScale(float input)
        {
            return (int)(input * this.Scale);
        }

        public Point ApplyScale(Point input)
        {
            return new Point(ApplyScale(input.X), ApplyScale(input.Y));
        }

        public Vector2 ApplyScale(Vector2 input)
        {
            return new Vector2(ApplyScale(input.X), ApplyScale(input.Y));
        }

        public MarginData ApplyScale(MarginData data)
        {
            return new MarginData(ApplyScale(data.Left), ApplyScale(data.Right), ApplyScale(data.Top), ApplyScale(data.Bottom));
        }

        public void Dispose()
        {
            // Anything that isn't disposed here should be disposed by the loader.
            pixel?.Dispose();
            pixel = null;
            SpriteBatch = null;
            GraphicsDevice = null;
            MouseProvider = null;
            ScreenProvider = null;
            ContentProvider = null;
            Font = null;
            defaultPanel = null;
            if (Instance == this)
                Instance = null;
            rt?.Dispose();
            rt = null;
        }
    }

    public enum PanelType
    {
        Default,
        Special,
        Transparent
    }

    internal enum SpecialKey
    {
        None,
        NewLine,
        Backspace,
        Tab,
        Delete
    }
}
