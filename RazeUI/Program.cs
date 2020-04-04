using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RazeContent;
using RazeUI.Handles;
using RazeUI.Providers;
using RazeUI.UISprites;
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
        private LayoutUserInterface uiRef;
        private KeyboardProvider keyboardProvider;

        private Program()
        {
            Graphics = new GraphicsDeviceManager(this);
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;

            Window.AllowUserResizing = true;
            IsMouseVisible = true;

            keyboardProvider = new KeyboardProvider();

            Window.TextInput += keyboardProvider.OnTextInput;
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

            Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

            string path = Path.Combine(new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName, "Content");
            content = new RazeContentManager(Graphics.GraphicsDevice, path);

            uiRef = new LayoutUserInterface(new UserInterface(Graphics.GraphicsDevice, new MouseProvider(), keyboardProvider, new ScreenProvider(), new ContentProvider(){Content = content}));
            uiRef.DrawUI += DrawUI;
        }

        private TextBoxHandle text = new TextBoxHandle();
        private void DrawUI(LayoutUserInterface ui)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Space))
                ui.Scale += 0.01f;
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                ui.Scale -= 0.01f;

            text.HintText = "Type something...";
            ui.UI.TextBox(new Rectangle(20, 20, 250, 40), text);

            ui.Button("Play");
            if (ui.Button($"High accuracy: {ui.UI.Font.HighAccuracyPositioning}"))
            {
                ui.UI.Font.HighAccuracyPositioning = !ui.UI.Font.HighAccuracyPositioning;
                ui.UI.Font.Clean();
            }

            ui.PanelContext(new Point(300, 400), PanelType.Solid);
            ui.Button("I'm inside the panel.");
            ui.Button("Me too");
            ui.Checkbox("Checkbox!", ref A);
            ui.FlatSeparator(64, Color.White);
            ui.ExpandWidth = false;
            ui.Button("Button -> ");
            ui.Anchor = Anchor.Horizontal;
            ui.Checkbox("Toggle!", ref A);
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
            ui.FlatSeparator(null, Color.White);
            ui.Checkbox("Centered checkbox", ref B);
            ui.ReleaseContext();

            ui.Anchor = Anchor.Horizontal;
            ui.Button("Hi :)");

            ui.Anchor = Anchor.Vertical;
            ui.Button("I'm outside the panel.");
            ui.Label("Label");
            ui.Label("Do something:");
            ui.Anchor = Anchor.Horizontal;
            ui.Button("New World");
            ui.Anchor = Anchor.Vertical;

            ui.ExpandWidth = true;
            ui.Button("I'm expanded, I hope.");

        }

        private bool A;
        private bool B;
        private Texture2D pixel;
        protected override void Draw(GameTime gameTime)
        {
            if(pixel == null)
            {
                pixel = new Texture2D(Graphics.GraphicsDevice, 1, 1);
                pixel.SetData(new Color[] { Color.White });
            }

            Graphics.GraphicsDevice.SetRenderTarget(null);
            Graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            // Update mouse input, clunky way to do it.
            (uiRef.UI.MouseProvider as MouseProvider).Update();

            spr.Begin();
            spr.Draw(pixel, Vector2.One * 200f, Color.White);
            spr.End();

            // Draw UI. Note that it is outside of the spritebatch Begin and End bounds.
            uiRef.Draw();

            base.Draw(gameTime);
        }

        private class ContentProvider : IContentProvider
        {
            public RazeContentManager Content;

            public UISprite LoadSprite(string localPath)
            {
                return new UISprite(Content.Load<Texture2D>(localPath));
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
                return state.RightButton == ButtonState.Pressed;
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

        private class KeyboardProvider : IKeyboardProvider
        {
            public void OnTextInput(object sender, TextInputEventArgs e)
            {
                OnKeyboardEvent?.Invoke(e.Key, e.Character);
            }

            public event KeyboardEvent OnKeyboardEvent;
        }
    }
}
