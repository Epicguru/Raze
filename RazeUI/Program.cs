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
        private static void Main()
        {
            using Program p = new Program();

            p.Run();
        }

        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spr;
        private RazeContentManager content;

        private GameFont font2;

        private Program()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            //Graphics.SynchronizeWithVerticalRetrace = false;

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

            font2 = content.Load<GameFont>("Pacifico.ttf");
            font2.VerticalSpacingMultiplier = 1f;
            font2.Size = 32;

            frameTimer.Start();

            txt = File.ReadAllLines(@"D:\Dev\C#\Raze\text.txt");
        }

        private Stopwatch sw = new Stopwatch();
        private Texture2D pixel;
        private GraphicsMetrics metrics;
        private int fps;
        private int frameCounter;
        private Stopwatch frameTimer = new Stopwatch();
        private string[] txt;
        protected override void Draw(GameTime gameTime)
        {
            frameCounter++;
            if(frameTimer.Elapsed.TotalSeconds >= 1f)
            {
                frameTimer.Restart();
                fps = frameCounter;
                frameCounter = 0;
            }

            if(pixel == null)
            {
                pixel = new Texture2D(graphics.GraphicsDevice, 1, 1);
                pixel.SetData(new Color[] { Color.White });
            }

            sw.Restart();

            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            spr.Begin(SpriteSortMode.Deferred, samplerState: SamplerState.LinearClamp);

            spr.Draw(pixel, new Rectangle(0, 0, 1024, 1024), Color.Black);

            int x = 0;
            foreach (var texture in font2.EnumerateTextureAtlases())
            {
                spr.Draw(texture, new Vector2(x, 0), Color.White);
                x += texture.Width;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                font2.Size += 1;
                Console.WriteLine("Increased");
            }
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
            {
                font2.Size -= 1;
            }

            if (!Keyboard.GetState().IsKeyDown(Keys.F))
            {

                const TextAlignment ali = TextAlignment.Centered;
                var realTxt = TextUtils.WrapLines(font2, txt, 700, false);

                Point paraSize = TextUtils.MeasureLines(font2, realTxt, ali);
                Color red = new Color(1f, 0f, 0f, 0.5f);
                Color blue = new Color(0f, 0f, 1f, 0.5f);
                spr.Draw(pixel, new Rectangle(300, 300, 700, paraSize.Y), red);
                spr.Draw(pixel, new Rectangle(300, 300, paraSize.X, paraSize.Y), blue);
                TextUtils.DrawLines(spr, font2, realTxt, new Vector2(300f, 300f), ali, Color.White);
            }

            string toDraw = "REEEEE!KEKEKEKEKE okay tho fr where u at.";

            //Point size = font2.MeasureString(toDraw);
            //var bounds = new Rectangle(100, 200, size.X, size.Y);
            //spr.Draw(pixel, bounds, Color.Green);

            spr.DrawString(font2, $"Font textures: {font2.TextureAtlasCount}, Sprites: {metrics.SpriteCount}, Texture swaps: {metrics.TextureCount}, fps: {fps}", Vector2.Zero, Color.Black);

            if (font2 != null)
                spr.DrawString(font2, toDraw, new Vector2(100, 200), Color.Black);

            spr.Draw(pixel, new Vector2(100, 200), Color.Red);

            spr.End();

            sw.Stop();

            base.Draw(gameTime);

            metrics = GraphicsDevice.Metrics;
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
