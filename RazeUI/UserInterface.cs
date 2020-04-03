using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RazeContent;
using RazeUI.Providers;
using RazeUI.UISprites;
using System;

namespace RazeUI
{
    public class UserInterface : IDisposable
    {
        public SpriteBatch SpriteBatch { get; set; }
        public IMouseProvider MouseProvider { get; set; }
        public IScreenProvider ScreenProvider { get; set; }
        public IContentProvider ContentProvider { get; set; }

        internal GameFont Font;
        internal float Scale = 1f; // Reason why this is internal is because it actually doesn't work for any methods in this class, it only affects small stuff like button-string margins.

        private NinePatch lightPanel;
        private NinePatch solidPanel;

        private Texture2D pixel;

        public UserInterface(SpriteBatch spr, IMouseProvider mouseProvider, IScreenProvider screenProvider, IContentProvider contentProvider)
        {
            this.SpriteBatch = spr;
            this.MouseProvider = mouseProvider;
            this.ScreenProvider = screenProvider;
            this.ContentProvider = contentProvider;

            pixel = new Texture2D(Program.Graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new Color[] { Color.White });

            LoadContent();
        }

        private void LoadContent()
        {
            lightPanel = new NinePatch(new UISprite(ContentProvider.LoadTexture("Textures/Panels/Light.png")), new Point(4, 4), new Point(8, 8));
            solidPanel = new NinePatch(new UISprite(ContentProvider.LoadTexture("Textures/Panels/Solid.png")), new Point(5, 5), new Point(6, 6));

            Font = ContentProvider.LoadFont("Fonts/Oxygen-Regular.ttf");
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

        public void Checkbox(Point pos, int size, ref bool isChecked)
        {

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
            pixel?.Dispose();
            pixel = null;
            SpriteBatch = null;
            MouseProvider = null;
            ScreenProvider = null;
            ContentProvider = null;
            Font = null;
            lightPanel = null;
        }
    }

    public enum PanelType
    {
        Light,
        Solid
    }
}
