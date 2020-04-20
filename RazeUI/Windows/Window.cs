using Microsoft.Xna.Framework;
using System;

namespace RazeUI.Windows
{
    // TODO make windows draggable.
    // URGTODO make UI system detect windows above the current window or above UI elements to detect clicking buttons better.
    /// <summary>
    /// A window is a self-contained panel that can be filled with UI elements. Once windows are registered with a <see cref="LayoutUserInterface"/>,
    /// they are drawn automatically every frame until they are closed.
    /// </summary>
    public abstract class Window
    {
        public event Action<Window> OnWindowClose;
        public bool IsOpen { get; internal set; }
        public Point Size { get; set; } = new Point(400, 500);
        public PanelType PanelType { get; set; } = PanelType.Default;
        public bool Centered { get; set; } = true;
        public Point Offset { get; set; } = Point.Zero;
        public string Title { get; set; }
        public bool DrawCloseButton { get; set; } = true;
        public Rectangle ScreenBounds
        {
            get
            {
                Point pos;
                if (Centered)
                {
                    int sw = UserInterface.Instance?.ScreenProvider?.GetWidth() ?? 0;
                    int sh = UserInterface.Instance?.ScreenProvider?.GetHeight() ?? 0;
                    pos = new Point((sw - Size.X) / 2, (sh - Size.Y) / 2);
                    pos += Offset;
                }
                else
                {
                    pos = Offset;
                }
                return new Rectangle(pos, Size);
            }
        }

        internal bool wantsToClose;

        internal void InternalDraw(LayoutUserInterface ui)
        {
            var bounds = ScreenBounds;

            MarginData? margins = null;
            int closeButtonHeight = 0;
            if (DrawCloseButton)
            {
                closeButtonHeight = ui.IMGUI.ApplyScale(ui.DefaultFontSize + 3);
                margins = new MarginData(10, 10, 10, 20 + closeButtonHeight);
            }

            ui.PanelContext(bounds, PanelType, margins);

            if (!string.IsNullOrWhiteSpace(Title))
            {
                ui.Anchor = Anchor.Centered;
                ui.FontSize = 56;
                ui.Label(Title, Color.LightSkyBlue);
                ui.FontSize = ui.DefaultFontSize;
                ui.Anchor = Anchor.Vertical;
            }

            Draw(ui);

            if (DrawCloseButton)
            {
                var exteriorBounds = ui.GetContextBounds(); // Should be same as Window.ScreenBounds.
                var mar = ui.GetContextMargins();
                if(ui.IMGUI.Button(new Rectangle(exteriorBounds.X + mar.Left, exteriorBounds.Bottom - 10 - closeButtonHeight, exteriorBounds.Width - mar.Left - mar.Right, closeButtonHeight), "Close"))
                    Close();
            }

            ui.EndContext();
        }

        public abstract void Draw(LayoutUserInterface ui);

        public virtual void Close()
        {
            wantsToClose = true;
        }

        public virtual void OnClose()
        {
            OnWindowClose?.Invoke(this);
        }

        public override string ToString()
        {
            return $"UI Window [{GetType().FullName}] ({(IsOpen ? "open" : "not open")})";
        }
    }
}
