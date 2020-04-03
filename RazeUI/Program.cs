using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RazeContent;
using RazeUI.Providers;
using System;
using System.Diagnostics;
using System.IO;

namespace RazeUI
{
    internal class Program : Game
    {
        private static void Main()
        {
            using Program p = new Program();

            p.Run();
        }

        public static GraphicsDeviceManager Graphics;
        private SpriteBatch spr;
        private RazeContentManager content;
        private LayoutUserInterface ui;

        private Program()
        {
            Graphics = new GraphicsDeviceManager(this);
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;

            Window.AllowUserResizing = true;
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            spr = new SpriteBatch(Graphics.GraphicsDevice);
            IsFixedTimeStep = false;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            Console.WriteLine("Loading...");

            string path = Path.Combine(new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName, "Content");
            content = new RazeContentManager(Graphics.GraphicsDevice, path);

            ui = new LayoutUserInterface(new UserInterface(spr, new MouseProvider(), new ScreenProvider(), new ContentProvider(){Content = content}));
        }

        private void DrawUI()
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Space))
                ui.Scale += 0.01f;
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                ui.Scale -= 0.01f;

            ui.Button("Play");
            if (ui.Button($"High accuracy: {ui.UI.Font.HighAccuracyPositioning}"))
            {
                ui.UI.Font.HighAccuracyPositioning = !ui.UI.Font.HighAccuracyPositioning;
                ui.UI.Font.Clean();
            }

            ui.PanelContext(new Point(300, 400), PanelType.Solid);
            ui.Button("I'm inside the panel.");
            ui.Button("Me too");
            ui.ReleaseContext();

            ui.Anchor = Anchor.Horizontal;
            ui.Button("To the right of the panel\nAnd multiline!");

            ui.PanelContext(new Point(-100, 400), PanelType.Solid, true);
            ui.ExpandWidth = false;
            ui.Button("This should be to the left of the long panel.");
            ui.Button("Below?");
            ui.Anchor = Anchor.Horizontal;
            ui.Button("To Right?");
            ui.Anchor = Anchor.Centered;
            ui.Button("Centered?");
            ui.Button("Centered and tall?", new Point(0, 100));
            ui.ReleaseContext();

            ui.Anchor = Anchor.Horizontal;
            ui.Button("Hi :)");

            ui.Anchor = Anchor.Vertical;
            ui.Button("I'm outside the panel.");

            ui.ExpandWidth = true;
            ui.Button("I'm expanded, I hope.");
        }

        private Texture2D pixel;
        protected override void Draw(GameTime gameTime)
        {
            if(pixel == null)
            {
                pixel = new Texture2D(Graphics.GraphicsDevice, 1, 1);
                pixel.SetData(new Color[] { Color.White });
            }

            Graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            spr.Begin();

            // Draw UI here.
            (ui.UI.MouseProvider as MouseProvider).Update();
            ui.BeginDraw();

            DrawUI();

            spr.End();
            base.Draw(gameTime);
        }

        private class ContentProvider : IContentProvider
        {
            public RazeContentManager Content;

            public Texture2D LoadTexture(string localPath)
            {
                return Content.Load<Texture2D>(localPath);
            }

            public GameFont LoadFont(string localPath)
            {
                return Content.Load<GameFont>(localPath);
            }
        }

        private class MouseProvider : IMouseProvider
        {
            private MouseState state, lastState;

            public void Update()
            {
                lastState = state;
                state = Mouse.GetState();
            }

            public Point GetMousePos()
            {
                return state.Position;
            }

            public bool IsLeftMouseDown()
            {
                return state.LeftButton == ButtonState.Pressed;
            }

            public bool IsRightMouseDown()
            {
                return state.LeftButton == ButtonState.Pressed;
            }

            public bool IsLeftMouseClick()
            {
                return IsLeftMouseDown() && lastState.LeftButton != ButtonState.Pressed;
            }

            public bool IsRightMouseClick()
            {
                return IsRightMouseDown() && lastState.RightButton != ButtonState.Pressed;
            }
        }

        private class ScreenProvider : IScreenProvider
        {
            public int GetWidth()
            {
                return Graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
            }

            public int GetHeight()
            {
                return Graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;
            }
        }
    }
}
