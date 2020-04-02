using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RazeContent;
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

        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spr;
        private RazeContentManager content;

        private GameFont font2;
        private NinePatch button;

        private Program()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;

            Window.AllowUserResizing = true;
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            spr = new SpriteBatch(graphics.GraphicsDevice);
            IsFixedTimeStep = false;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            Console.WriteLine("Loading...");

            string path = Path.Combine(new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName, "Content");
            content = new RazeContentManager(graphics.GraphicsDevice, path);

            font2 = content.Load<GameFont>("Oxygen-Regular.ttf");
            font2.VerticalSpacingMultiplier = 1f;

            var tex = content.Load<Texture2D>("Textures/Buttons/DevButton.png");
            button = new NinePatch(new UISprite(tex), new Point(4, 4), new Point(8, 8));

            Console.WriteLine(button.BottomLeft.Region.ToString());
            Console.WriteLine(button.BottomCenter.Region.ToString());
            Console.WriteLine(button.BottomRight.Region.ToString());
        }

        private Texture2D pixel;
        protected override void Draw(GameTime gameTime)
        {
            if(pixel == null)
            {
                pixel = new Texture2D(graphics.GraphicsDevice, 1, 1);
                pixel.SetData(new Color[] { Color.White });
            }

            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            spr.Begin();

            // Draw UI here.

            Vector2 mousePos = Mouse.GetState().Position.ToVector2();
            button.Draw(spr, new Vector2(100, 100), mousePos - new Vector2(100, 100), Color.White);

            spr.End();
            base.Draw(gameTime);
        }

        /*
         * LE PLAN
         *
         * var window = GUI.Window(ref Rect pos);
         *
         * # Everything between window.start and window.end will automatically have window as parent.
         * window.Start();
         *
         * GUI.Button("Click me!", (e) => { Log("Was clicked!") });
         * var label = GUI.Label("Label", anchor: Anchor.ImmediateRight)
         *
         * window.End();
         */
    }
}
