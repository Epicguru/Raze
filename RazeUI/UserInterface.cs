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
        public SpriteBatch SpriteBatch { get; set; }
        public GraphicsDevice GraphicsDevice { get; set; }
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
                    this.keyProvider.OnKeyboardEvent -= OnKeyInput;

                this.keyProvider = value;
                if (this.keyProvider != null)
                    this.keyProvider.OnKeyboardEvent += OnKeyInput;
            }
        }
    
        public IScreenProvider ScreenProvider { get; set; }
        public IContentProvider ContentProvider { get; set; }

        public event Action<UserInterface> DrawUI;

        internal GameFont Font;
        internal float Scale = 1f; // Reason why this is internal is because it actually doesn't work for any methods in this class, it only affects small stuff like button-string margins.
        internal RenderTarget2D rt;
        internal bool soloRender = true;
        internal Queue<(SpecialKey special, Keys key, char character)> typedKeys = new Queue<(SpecialKey, Keys, char)>();

        private IKeyboardProvider keyProvider;
        private NinePatch lightPanel;
        private NinePatch solidPanel;
        private NinePatch inputBox, inputBoxSelected;
        private NinePatch flatSeparator;
        private UISprite checkBox, checkBoxSelected, checkBoxHighlighted, checkBoxSelectedHighlighted;
        private Texture2D pixel;

        public UserInterface(GraphicsDevice gd, IMouseProvider mouseProvider, IKeyboardProvider keyboardProvider, IScreenProvider screenProvider, IContentProvider contentProvider)
        {
            this.GraphicsDevice = gd;
            this.SpriteBatch = new SpriteBatch(gd);
            this.MouseProvider = mouseProvider;
            this.KeyboardProvider = keyboardProvider;
            this.ScreenProvider = screenProvider;
            this.ContentProvider = contentProvider;

            pixel = new Texture2D(Program.Graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new Color[] { Color.White });

            LoadContent();
        }

        private void LoadContent()
        {
            lightPanel = new NinePatch(ContentProvider.LoadSprite("Textures/Panels/Light.png"), new Point(4, 4), new Point(8, 8));
            solidPanel = new NinePatch(ContentProvider.LoadSprite("Textures/Panels/Solid.png"), new Point(5, 5), new Point(6, 6));
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

        public void Draw()
        {
            if (soloRender)
            {
                UpdateRenderTarget();
                GraphicsDevice.SetRenderTarget(this.rt);
                GraphicsDevice.Clear(Color.Transparent);
            }

            SpriteBatch.Begin();
            DrawUI?.Invoke(this);
            SpriteBatch.End();

            if (soloRender)
            {
                FinishDraw();
            }
        }

        internal void FinishDraw()
        {
            GraphicsDevice.SetRenderTarget(null);
            SpriteBatch.Begin();
            SpriteBatch.Draw(rt, Vector2.Zero, Color.White);
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
            Panel(bounds, Color.White, out leftClick, out rightClick, PanelType.Light);

            if (text == null)
                return;

            Point size = Font.MeasureString(text);
            Point pos = bounds.Location + new Point((bounds.Width - size.X) / 2, (bounds.Height - size.Y) / 2);

            SpriteBatch.DrawString(Font, text, pos.ToVector2(), Color.White);
        }

        #endregion

        #region Panels

        public void Panel(Rectangle bounds, Color tint, PanelType type = PanelType.Solid)
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

            sprite.Draw(SpriteBatch, bounds, Color.White);

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

        #region Text Input

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
                handle.IsSelected = handle.IsMouseOver;
            }

            // Change sprite based on selected state.
            (handle.IsSelected ? inputBoxSelected : inputBox).Draw(SpriteBatch, bounds, Color.White);

            // Take input if selected.
            if (handle.IsSelected)
            {
                while (typedKeys.Count > 0)
                {
                    var data = typedKeys.Dequeue();
                    if (data.special == SpecialKey.None)
                    {
                        handle.Text += data.character;
                    }
                    else
                    {
                        switch (data.special)
                        {
                            case SpecialKey.Tab:
                                // Add 4 spaces? Maybe? Ignore for now.
                                break;
                            case SpecialKey.Backspace:
                                // Delete the last character.
                                if (handle.Text.Length > 0)
                                {
                                    handle.Text = handle.Text[..^1];
                                }
                                break;
                            case SpecialKey.Delete:
                                // TODO implement here.
                                break;
                            case SpecialKey.NewLine:
                                // TODO check if new line is allowed.
                                handle.Text += '\n';
                                break;
                        }
                    }
                }
            }

            // Draw text, or text hint if there is no text.
            bool hasText = !string.IsNullOrEmpty(handle.Text);
            bool drawHint = !hasText && !string.IsNullOrWhiteSpace(handle.HintText);
            if (drawHint)
            {
                SpriteBatch.DrawString(Font, handle.HintText, textPos.ToVector2(), Color.Gray);
            }

            if (hasText)
            {
                SpriteBatch.DrawString(Font, handle.Text, textPos.ToVector2(), Color.Black);
            }
        }

        #endregion

        public virtual bool IsMouseOver(Rectangle bounds)
        {
            return bounds.Contains(MouseProvider.GetMousePos());
        }

        private NinePatch GetPanelPatch(PanelType type)
        {
            return type switch
            {
                PanelType.Light => lightPanel,
                PanelType.Solid => solidPanel,
                _ => throw new NotImplementedException()
            };
        }

        internal int ApplyScale(float input)
        {
            return (int)(input * this.Scale);
        }

        internal Point ApplyScale(Point input)
        {
            return new Point(ApplyScale(input.X), ApplyScale(input.Y));
        }

        internal MarginData ApplyScale(MarginData data)
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
            lightPanel = null;
            rt?.Dispose();
            rt = null;
        }
    }

    public enum PanelType
    {
        Light,
        Solid
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
