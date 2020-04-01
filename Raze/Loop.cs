using GVS.Entities;
using GVS.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Threading;

namespace GVS
{
    public static class Loop
    {
        /// <summary>
        /// The target application framerate. The game will be updated and rendered at this frequency, whenever possible.
        /// If set to 0 (zero) then there is no target framerate and the game will update as fast as possible.
        /// </summary>
        public static float TargetFramerate
        {
            get
            {
                return targetFramerate;
            }
            set
            {
                if (value == targetFramerate)
                    return;

                if (value < 0)
                    value = 0;

                targetFramerate = value;
                Debug.Trace($"Updated target framerate to {value} {(value == 0 ? "(none)" : "")}");
            }
        }

        /// <summary>
        /// The current update and draw frequency that the application is running at, calculated each frame.
        /// More accurate at lower framerates, less accurate at higher framerates. For a more stable and reliable value,
        /// see <see cref="Framerate"/>.
        /// </summary>
        public static float ImmediateFramerate { get; private set; }

        /// <summary>
        /// The current update and draw frequency, calculated once per second.
        /// The framerate is affected by <see cref="TargetFramerate"/> and <see cref="VSyncMode"/> and of course
        /// the actual speed of game updating and rendering.
        /// </summary>
        public static float Framerate { get; private set; }

        /// <summary>
        /// If true, then framerate is limited and maintained using a more accurate technique, leading to more
        /// consistent framerates.
        /// </summary>
        public static bool EnablePrecisionFramerate { get; set; } = true;

        /// <summary>
        /// The color to clear the background to.
        /// </summary>
        public static Color ClearColor { get; set; } = Color.CornflowerBlue;

        /// <summary>
        /// Gets or sets the vertical sync mode for the display. Default is disabled.
        /// </summary>
        public static VSyncMode VSyncMode
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
                switch (value)
                {
                    case VSyncMode.DISABLED:
                        Main.GlobalGraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
                        break;
                    case VSyncMode.ENABLED:
                        Main.GlobalGraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.One;
                        break;
                    case VSyncMode.DOUBLE:
                        Main.GlobalGraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Two;
                        break;
                }

                Debug.Trace($"Updated VSync mode to {value}");
            }
        }

        public static Thread Thread { get; private set; }
        public static bool IsBackupThreadActive { get; internal set; }
        public static Thread BackupThread { get; private set; }
        public static bool Running { get; private set; }
        public static bool ThreadQuit { get; private set; }
        public static bool InUIDraw { get; private set; }
        public class Stats
        {
            public double FrameTotalTime;
            public double FrameUpdateTime;
            public double FrameDrawTime;
            public double FrameSleepTime;
            public double FramePresentingTime;
            public bool Waited;
            public GraphicsMetrics DrawMetrics { get; internal set; }
        }
        public static Stats Statistics { get; private set; } = new Stats();

        private static readonly object drawKey = new object();
        private static float targetFramerate;
        private static int cumulativeFrames;
        private static readonly Stopwatch frameTimer = new Stopwatch();
        private static VSyncMode vsm = VSyncMode.ENABLED;

        private static double TargetFramerateInterval()
        {
            // Remember physics: f=1/i  so  i=1/f
            return 1.0 / TargetFramerate;
        }

        public static void Start()
        {
            if (Running)
                return;

            Running = true;
            ThreadQuit = false;

            VSyncMode = VSyncMode.ENABLED;

            Thread = new Thread(Run);
            Thread.Name = "Game Loop";
            Thread.Priority = ThreadPriority.Highest;

            BackupThread = new Thread(RunBackup);
            BackupThread.Name = "Backup Rendering Thread";
            BackupThread.Priority = ThreadPriority.AboveNormal;

            Thread.Start();
            BackupThread.Start();
            frameTimer.Start();
            Framerate = 0;
            ImmediateFramerate = 0;
        }

        public static void Stop()
        {
            if (!Running)
                return;

            Running = false;
        }

        public static void StopAndWait()
        {
            Stop();
            while (!ThreadQuit)
            {
                Thread.Sleep(1);
            }
        }

        private static void Run()
        {
            Begin();

            Debug.Log("Starting game loop...");
            SpriteBatch spr = Main.SpriteBatch;
            Stopwatch watch = new Stopwatch();
            Stopwatch watch2 = new Stopwatch();
            Stopwatch watch3 = new Stopwatch();
            Stopwatch sleepWatch = new Stopwatch();

            double updateTime = 0.0;
            double renderTime = 0.0;
            double presentTime = 0.0;
            double total = 0.0;
            double sleep = 0.0;

            while (Running)
            {
                watch2.Restart();

                // Determine the ideal loop time, in seconds.
                double target = 0.0;
                if (TargetFramerate != 0f)
                    target = TargetFramerateInterval();

                Time.StartFrame();

                watch.Restart();
                Update();
                watch.Stop();
                updateTime = watch.Elapsed.TotalSeconds;
                Statistics.FrameUpdateTime = updateTime;

                watch.Restart();
                lock (drawKey)
                {
                    Draw(spr);
                }
                watch.Stop();
                renderTime = watch.Elapsed.TotalSeconds;
                Statistics.FrameDrawTime = renderTime;

                watch.Restart();
                Present();
                watch.Stop();
                presentTime = watch.Elapsed.TotalSeconds;
                Statistics.FramePresentingTime = presentTime;

                total = updateTime + renderTime + presentTime;
                sleep = target - total;

                if(sleep > 0.0)
                {
                    sleepWatch.Restart();
                    if (!EnablePrecisionFramerate)
                    {
                        // Sleep using the normal method. Allow the CPU to do whatever it wants.
                        TimeSpan s = TimeSpan.FromSeconds(sleep);
                        Thread.Sleep(s);
                    }
                    else
                    {
                        // Sleep by slowly creeping up to the target time in a loop. Sometimes more accurate.
                        watch3.Restart();
                        while (watch3.Elapsed.TotalSeconds + (0.001) < sleep)
                        {
                            Thread.Sleep(1);
                        }
                        watch3.Stop();
                    }
                    sleepWatch.Stop();
                    Statistics.FrameSleepTime = sleepWatch.Elapsed.TotalSeconds;
                    Statistics.Waited = true;
                }
                else
                {
                    Statistics.Waited = false;
                }

                watch2.Stop();
                ImmediateFramerate = (float)(1.0 / watch2.Elapsed.TotalSeconds);
                Statistics.FrameTotalTime = watch2.Elapsed.TotalSeconds;
            }

            ThreadQuit = true;
            Thread = null;
            Debug.Log("Stopped game loop!");
        }

        private static void RunBackup()
        {
            SpriteBatch spr2 = new SpriteBatch(Main.GlobalGraphicsDevice);

            const float TARGET_FRAMERATE = 60f;
            const float DELTA_TIME = 1f / TARGET_FRAMERATE;
            const int WAIT_TIME = (int) (1000f / TARGET_FRAMERATE);

            while (Running)
            {
                Thread.Sleep(WAIT_TIME);
                if (IsBackupThreadActive)
                {
                    lock (drawKey)
                    {
                        spr2.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                        spr2.Draw(Debug.Pixel, new Rectangle(0, 0, Screen.Width, Screen.Height), Color.Black);
                        Main.ScreenManager.DrawUIBackupThread(spr2, DELTA_TIME);
                        spr2.End();
                    }
                    Present();
                }
            }

            spr2.Dispose();
        }

        private static void Begin()
        {

        }

        private static void Update()
        {
            cumulativeFrames++;
            if(frameTimer.Elapsed.TotalSeconds >= 1.0)
            {
                frameTimer.Restart();
                Framerate = cumulativeFrames;
                cumulativeFrames = 0;
            }

            Debug.Update();
            Input.StartFrame();

            Debug.Text($"FPS: {Framerate:F0} (Target: {(TargetFramerate == 0 ? "uncapped" : TargetFramerate.ToString("F0"))}, VSync: {VSyncMode})");
            Debug.Text($"Time Scale: {Time.TimeScale}");
            //Debug.Text($"Screen Res: ({Screen.Width}x{Screen.Height})");
            Debug.Text($"Used memory: {Main.GameProcess.PrivateMemorySize64 / 1024 / 1024}MB.");
            Debug.Text($"Texture Swap Count: {Loop.Statistics.DrawMetrics.TextureCount}");
            Debug.Text($"Draw Calls: {Loop.Statistics.DrawMetrics.DrawCount}");
            Debug.Text($"Sprites Drawn: {Loop.Statistics.DrawMetrics.SpriteCount}");
            Debug.Text($"Total Entities: {Entity.SpawnedCount}.");
            Debug.Text($"Net - Client: {Net.Client?.ConnectionStatus.ToString() ?? "null"}, Server: {Net.Server?.Status.ToString() ?? "null"}");
            Debug.Text($"Under mouse: {(Input.TileUnderMouse == null ? "null" : Input.TileUnderMouse.Position.ToString())}");

            // Update currently active screen.
            Main.MainUpdate();

            Input.EndFrame();
        }

        private static void Draw(SpriteBatch spr)
        {
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

            InUIDraw = true;

            spr.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, null, null, null, null);

            // Draw the UI.
            Main.MainDrawUI();
            Debug.DrawUI(spr);

            spr.End();

            InUIDraw = false;
        }

        private static void Present()
        {
            Statistics.DrawMetrics = Main.GlobalGraphicsDevice.Metrics;
            Main.GlobalGraphicsDevice.Present();
        }
    }
}
