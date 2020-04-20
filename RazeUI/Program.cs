using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RazeContent;
using RazeUI.Handles;
using RazeUI.Providers;
using RazeUI.Providers.Implementations;
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

            Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

            string path = Path.Combine(new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName, "Content");
            content = new RazeContentManager(Graphics.GraphicsDevice, path);

            uiRef = new LayoutUserInterface(new UserInterface(Graphics.GraphicsDevice, new MonoGameMouseProvider(), new MonoGameKeyboardProvider(Window), new MonoGameScreenProvider(GraphicsDevice), new RazeContentProvider(content)));
            uiRef.DrawUI += DrawUI;
        }

        private TextBoxHandle text = new TextBoxHandle();
        private void DrawUI(LayoutUserInterface ui)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
                ui.Scale += 0.01f;
            if (Keyboard.GetState().IsKeyDown(Keys.Down))
                ui.Scale -= 0.01f;

            ui.Button("Play");

            if (ui.Button($"High accuracy: {ui.IMGUI.Font.HighAccuracyPositioning}"))
            {
                ui.IMGUI.Font.HighAccuracyPositioning = !ui.IMGUI.Font.HighAccuracyPositioning;
                ui.IMGUI.Font.Clean();
            }

            ui.PanelContext(new Point(300, 400), PanelType.Special);
            ui.Button("I'm inside the panel.");
            ui.Button("Me too");
            ui.Checkbox("Checkbox!", ref A);
            ui.FlatSeparator(64, Color.White);
            ui.ExpandWidth = false;
            ui.Button("Button -> ");
            ui.Anchor = Anchor.Horizontal;
            ui.Checkbox("Toggle!", ref A);
            ui.EndContext();

            ui.Anchor = Anchor.Horizontal;
            ui.Button("To the right of the panel\nAnd multiline!");

            ui.PanelContext(new Point(-100, 400), PanelType.Special, true);
            ui.Button("This should be to the left of the long panel.");
            ui.ExpandWidth = false;
            ui.TextBox(text, new Point(300, 42));
            ui.Anchor = Anchor.Horizontal;
            ui.Button("To Right?");
            ui.Anchor = Anchor.Centered;
            ui.Button("Centered?");
            ui.Button("Centered and tall?", new Point(0, 100));
            ui.FlatSeparator(null, Color.White);
            ui.Checkbox("Centered checkbox", ref B);
            ui.EndContext();

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

            spr.Begin();
            spr.Draw(pixel, Vector2.One * 200f, Color.White);
            spr.End();

            // Draw UI. Note that it is outside of the spritebatch Begin and End bounds.
            uiRef.Draw();

            base.Draw(gameTime);
        }
    }
}
