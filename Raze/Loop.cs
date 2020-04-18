using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Raze.Entities;
using Raze.Networking;

namespace Raze
{
    public static class Loop
    {
        /// <summary>
        /// The current update and draw frequency, calculated once per second.
        /// The framerate is affected by <see cref="TargetFramerate"/> and <see cref="VSync"/> and of course
        /// the actual speed of game updating and rendering.
        /// </summary>
        public static float Framerate { get; private set; }

        /// <summary>
        /// The color to clear the background to.
        /// </summary>
        public static Color ClearColor { get; set; } = Color.CornflowerBlue;

        /// <summary>
        /// Gets or sets the vertical sync mode for the display. Default is disabled.
        /// </summary>
        public static bool VSync
        {
            get
            {
                return vsm;
            }
            set
            {
                if (value == vsm)
                    return;

                vsm = value;
                Main.Graphics.SynchronizeWithVerticalRetrace = value;
                Main.Graphics.ApplyChanges();

                Debug.Trace($"Changed VSync enabled to {value}");
            }
        }

        public static bool InUIDraw { get; private set; }

        public struct Stats
        {
            public double TotalTime;
            public double UpdateTime;
            public double DrawTime;
            public double DrawUITime;
            public double PresentTime;
            public GraphicsMetrics DrawMetrics;

            public override string ToString()
            {
                return $"Total: {TotalTime*1000f:F2}\nUpdate: {UpdateTime*1000f:F2}\nDraw: {DrawTime * 1000f:F2}\nDraw UI: {DrawUITime * 1000f:F2}\nPresent: {PresentTime * 1000f:F2}";
            }
        }
        public static Stats FrameStats;

        private static bool vsm;
        private static Stats thisFrameStats;
        private static int cumulativeFrames;
        private static Stopwatch tempTimer = new Stopwatch();
        private static Stopwatch fpsTimer = new Stopwatch();
        private static Stopwatch totalTimer = new Stopwatch();

        public static void Start()
        {
            fpsTimer.Start();
            Framerate = 0;
        }

        internal static void Update()
        {
            totalTimer.Restart();
            tempTimer.Restart();

            Time.StartFrame();

            Debug.Update();
            Input.StartFrame();

            // Main update here.
            Main.MainUpdate();

            Debug.Text($"FPS: {Framerate:F0}, VSync: {VSync}");
            Debug.Text($"Time Scale: {Time.TimeScale}");
            //Debug.Text($"Screen Res: ({Screen.Width}x{Screen.Height})");
            Debug.Text($"Used memory: {Main.GameProcess.PrivateMemorySize64 / 1024 / 1024}MB.");
            Debug.Text($"Texture Swap Count: {Loop.FrameStats.DrawMetrics.TextureCount}");
            Debug.Text($"Draw Calls: {Loop.FrameStats.DrawMetrics.DrawCount}");
            Debug.Text($"Sprites Drawn: {Loop.FrameStats.DrawMetrics.SpriteCount}");
            Debug.Text($"Total Entities: {Entity.SpawnedCount}.");
            Debug.Text($"Net - Client: {Net.Client?.ConnectionStatus.ToString() ?? "null"}, Server: {Net.Server?.Status.ToString() ?? "null"}");
            Debug.Text($"Under mouse: {(Input.TileUnderMouse == null ? "null" : Input.TileUnderMouse.Position.ToString())}");

            Input.EndFrame();

            cumulativeFrames++;
            if (fpsTimer.Elapsed.TotalSeconds >= 1.0)
            {
                fpsTimer.Restart();
                Framerate = cumulativeFrames;
                cumulativeFrames = 0;
            }

            tempTimer.Stop();
            thisFrameStats.UpdateTime = tempTimer.Elapsed.TotalSeconds;
        }

        internal static void Draw(SpriteBatch spr)
        {
            tempTimer.Restart();
            Main.Camera.UpdateMatrix(Main.GlobalGraphicsDevice);
            Main.GlobalGraphicsDevice.Clear(ClearColor);

            SamplerState s = Main.Camera.Zoom >= 1 ? SamplerState.PointClamp : SamplerState.LinearClamp;
            spr.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, s, null, null, null, Main.Camera.GetMatrix());

            // Main world draw.
            Main.MainDraw();

            spr.End();

            spr.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, Main.Camera.GetMatrix());
            Debug.Draw(spr);
            spr.End();

            tempTimer.Stop();
            thisFrameStats.DrawTime = tempTimer.Elapsed.TotalSeconds;

            InUIDraw = true;

            tempTimer.Restart();

            spr.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, null);

            // Draw the UI.
            Main.MainDrawUI();
            Debug.DrawUI(spr);

            spr.End();

            InUIDraw = false;

            tempTimer.Stop();
            thisFrameStats.DrawUITime = tempTimer.Elapsed.TotalSeconds;
        }

        internal static void Present()
        {
            tempTimer.Restart();

            Main.GlobalGraphicsDevice.Present();

            tempTimer.Stop();

            thisFrameStats.PresentTime = tempTimer.Elapsed.TotalSeconds;

            totalTimer.Stop();
            thisFrameStats.TotalTime = totalTimer.Elapsed.TotalSeconds; // TODO fix this so that the debug drawer doesn't mix info from two frames
            Debug.Log($"{thisFrameStats}");

            thisFrameStats.DrawMetrics = Main.GlobalGraphicsDevice.Metrics;

            FrameStats = thisFrameStats;
            thisFrameStats = default;
        }
    }
}
