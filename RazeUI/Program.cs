using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.IO;
using RazeContent;

namespace RazeUI
{
    internal class Program : Game
    {
        private static void Main(string[] args)
        {
            using Program p = new Program();

            p.Run();
        }

        private readonly GraphicsDeviceManager Graphics;
        private SpriteBatch spr;
        private RazeContentManager content;

        private GameFont font1;
        private GameFont font2;

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

            string path = Path.Combine(new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName, "Content");
            content = new RazeContentManager(Graphics.GraphicsDevice, path);

            font1 = content.Load<GameFont>("Pacifico.ttf");
            font2 = content.Load<GameFont>("Lato-Black.ttf");
        }

        private TimeSpan lastSpan;
        private Stopwatch sw = new Stopwatch();
        private Texture2D pixel;
        protected override void Draw(GameTime gameTime)
        {
            if(pixel == null)
            {
                pixel = new Texture2D(Graphics.GraphicsDevice, 1, 1);
                pixel.SetData(new Color[] { Color.White });
            }

            sw.Restart();

            Graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            spr.Begin(samplerState: SamplerState.LinearClamp);

            //spr.DrawString(font1, $"Hello, world! This is a test: {lastSpan.TotalMilliseconds:F1} ms", Vector2.Zero, Color.Black);

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                font2.Size += 1;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
            {
                font2.Size -= 1;
            }

            if(font2 != null)
                spr.DrawString(font2, "Hello, world! This is a test!", Vector2.One * 100f, Color.Black);

            string toDraw = "REEEEE!KEKEKEKEKE okay tho fr where u at.";

            var size = font2.MeasureString(toDraw);
            var bounds = new Rectangle(100, 200, (int) size.X, (int) size.Y);
            var bounds2 = font2.GetBounds(new Vector2(100, 200), toDraw);
            spr.Draw(pixel, bounds, Color.Green);
            spr.Draw(pixel, bounds2, Color.Red);

            Vector2 offset = bounds2.Location.ToVector2() -new Vector2(100, 200);
            spr.DrawString(font2, offset.ToString(), Vector2.Zero, Color.Black);

            if (font2 != null)
                spr.DrawString(font2, toDraw, new Vector2(100, 200), Color.Black);

            spr.Draw(pixel, new Vector2(100, 200), Color.Red);

            spr.End();

            sw.Stop();
            lastSpan = sw.Elapsed;

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
