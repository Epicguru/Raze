using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Raze.IO;
using RazeContent;

namespace Raze
{
    public static class Debug
    {
        // TODO add an option (or just replace current system) to write the log entries straight to file
        // async to reduce memory consumption from having all these strings lying around.

        public static bool DebugActive { get; set; } = false;

        public static bool LogEnabled { get; set; } = true;
        public static bool LogCallingMethod { get; set; } = true;
        public static bool LogEnableTrace { get; set; } = true;
        public static bool LogEnableSave { get; set; } = true;
        public static bool Highlight { get; set; } = false;

        public static bool LogPrintLevel { get; set; } = true;
        public static bool LogPrintTime { get; set; } = false;
        public static string LogFilePath { get; private set; }

        public static Texture2D Pixel { get; private set; }

        internal static readonly List<string> DebugTexts = new List<string>();
        private static readonly StringBuilder stringBuilder = new StringBuilder();
        private static readonly StringBuilder textBuilder = new StringBuilder();
        private static readonly List<string> logEntries = new List<string>();
        private static bool doneInit = false;
        private static readonly object lockKey = new object();
        private static readonly Dictionary<string, System.Diagnostics.Stopwatch> timers = new Dictionary<string, System.Diagnostics.Stopwatch>();
        private static readonly Queue<System.Diagnostics.Stopwatch> timerPool = new Queue<System.Diagnostics.Stopwatch>();
        private static readonly List<string> timerNames = new List<string>();
        private static readonly List<(DrawInstruction, object[] args)> toDraw = new List<(DrawInstruction, object[] args)>();
        private static readonly List<(DrawInstruction, object[] args)> toDrawUI = new List<(DrawInstruction, object[] args)>();

        public delegate void DrawInstruction(SpriteBatch spr, object[] args);

        public static void Init()
        {
            if (doneInit)
                return;

            doneInit = true;

            Pixel = new Texture2D(Main.GlobalGraphicsDevice, 1, 1);
            Pixel.SetData(new Color[] {Color.White});

            string filePath = Path.Combine(GameIO.LogDirectory, "Log File.txt");
            LogFilePath = filePath;
        }

        public static void Shutdown()
        {
            Log($"Saving {logEntries.Count} log entries to {LogFilePath}.");
            GameIO.EnsureParentDirectory(LogFilePath);
            using(StreamWriter writer = new StreamWriter(File.Create(LogFilePath)))
            {
                foreach (var line in logEntries)
                {
                    writer.WriteLine(line);
                }
            }
        }

        private static System.Diagnostics.Stopwatch GetPooledTimer()
        {
            if(timerPool.Count == 0)
            {
                return new System.Diagnostics.Stopwatch();
            }
            else
            {
                return timerPool.Dequeue();
            }
        }

        private static void ReturnPooledTimer(System.Diagnostics.Stopwatch s)
        {
            s.Stop();
            timerPool.Enqueue(s);
;        }

        public static System.Diagnostics.Stopwatch StartTimer(string timerKey)
        {
            if (string.IsNullOrEmpty(timerKey))
            {
                Warn("Timer key cannot be null or empty. Timer will return a time of 0 seconds.");
                return null;
            }

            lock (lockKey)
            {
                if (timers.ContainsKey(timerKey))
                {
                    Warn($"Timer for key '{timerKey}' already exists. Stop that timer first.");
                    return null;
                }

                var timer = GetPooledTimer();
                timers.Add(timerKey, timer);
                timerNames.Add(timerKey);
                timer.Restart();
                return timer;
            }
        }

        public static TimeSpan StopTimer(string key, bool log = false)
        {
            TimeSpan s = StopInternal(key, log, out bool stopped);
            if (stopped)
            {
                timerNames.Remove(key);
            }
            return s;
        }

        public static TimeSpan StopTimer(bool log = false)
        {
            string name = timerNames.Count == 0 ? null : timerNames[^1];
            if (timerNames.Count != 0)
                timerNames.RemoveAt(timerNames.Count - 1);

            if (name == null)
                Warn("No timers are currently running, cannot stop.");

            return StopInternal(name, log, out bool _);
        }

        private static TimeSpan StopInternal(string key, bool log, out bool didStop)
        {
            if (string.IsNullOrEmpty(key))
            {
                didStop = false;
                return TimeSpan.Zero;
            }

            if (!timers.ContainsKey(key))
            {
                Warn($"Did not find timer for key '{key}'. Returning zero time span.");
                didStop = false;
                return TimeSpan.Zero;
            }

            var sw = timers[key];
            sw.Stop();
            timers.Remove(key);

            if (log)
            {
                Trace($"{key}: {sw.Elapsed.TotalMilliseconds:F2} ms");
            }

            didStop = true;
            return sw.Elapsed;
        }

        public static void Assert(bool condition, string errorMsg = null)
        {
            if (!condition)
            {
                Error(errorMsg ?? $"Assertion failed!");
            }
        }

        public static void Text(string s)
        {
            if(s != null)
                DebugTexts.Add(s);
        }

        public static void TextAt(string text, Vector2 position, Color color)
        {
            if (Loop.InUIDraw)
            {
                toDrawUI.Add((DrawText, new object[] { text, position, color }));
            }
            else
            {
                toDraw.Add((DrawText, new object[] { text, position, color }));
            }
        }

        private static void DrawText(SpriteBatch spr, object[] args)
        {
            spr.DrawString(Main.MediumFont, (string)args[0], (Vector2)args[1], (Color)args[2]);
        }

        public static void Box(Rectangle position, Color color)
        {
            if (Loop.InUIDraw)
            {
                toDrawUI.Add((DrawRect, new object[] { position, color }));
            }
            else
            {
                toDraw.Add((DrawRect, new object[] { position, color }));
            }
        }

        public static void Point(Vector2 position, float size, Color color)
        {
            if (Loop.InUIDraw)
            {
                toDrawUI.Add((DrawPoint, new object[] { position, size, color }));
            }
            else
            {
                toDraw.Add((DrawPoint, new object[] { position, size, color }));
            }
        }

        private static void DrawPoint(SpriteBatch spr, object[] args)
        {
            Vector2 pos = (Vector2)args[0];
            float size = (float) args[1];
            Color color = (Color) args[2];

            int centerX = (int)pos.X;
            int centerY = (int)pos.Y;
            int intSize = (int)size;

            Rectangle bounds = new Rectangle(centerX - intSize / 2, centerY - intSize / 2, intSize, intSize);

            spr.Draw(Pixel, bounds, color);
        }

        private static void DrawRect(SpriteBatch spr, object[] args)
        {
            Rectangle bounds = (Rectangle)args[0];
            Color c = (Color)args[1];

            spr.Draw(Pixel, bounds, c);
        }

        internal static void Update()
        {
            if (Input.IsKeyJustDown(Keys.F1))
                DebugActive = !DebugActive;

            DebugTexts.Clear();
        }

        internal static void Draw(SpriteBatch spr)
        {
            bool vis = DebugActive;
            if(!vis)
            {
                toDraw.Clear();
                return;
            }
            // Draw all stuff that is pending.
            while (toDraw.Count > 0)
            {
                (var method, object[] args) = toDraw[0];
                toDraw.RemoveAt(0);

                method.Invoke(spr, args);
            }
        }

        internal static void DrawUI(SpriteBatch spr)
        {
            bool vis = DebugActive;
            if (!vis)
            {
                toDrawUI.Clear();
                return;
            }

            // Draw all UI stuff that is pending.
            while (toDrawUI.Count > 0)
            {
                (var method, object[] args) = toDrawUI[0];
                toDrawUI.RemoveAt(0);

                method.Invoke(spr, args);
            }

            double total = Loop.FrameStats.TotalTime;
            double update = Loop.FrameStats.UpdateTime;
            double draw = Loop.FrameStats.DrawTime;
            double drawUI = Loop.FrameStats.DrawUITime;
            double present = Loop.FrameStats.PresentTime;
            double other = total - (update + draw + drawUI + present);

            if (total == 0.0)
                total = 0.1;

            int i = 0;
            DrawPart(i++, "Update", update, total, Color.Violet);
            DrawPart(i++, "Draw", draw, total, Color.LightSeaGreen);
            DrawPart(i++, "Draw UI", drawUI, total, Color.MediumPurple);
            DrawPart(i++, "Present", present, total, Color.IndianRed);
            DrawPart(i++, "Other", other, total, Color.Beige);

            int y = 140;

            foreach (string text in Debug.DebugTexts)
            {
                var size = Main.MediumFont.MeasureString(text);

                int x = Screen.Width - ((int)size.X + 5);
                spr.DrawString(Main.MediumFont, text, new Vector2(x, y), Color.Black);
                y += (int)size.Y;
            }
        }

        private static readonly StringBuilder str = new StringBuilder();
        private static void DrawPart(int index, string name, double time, double total, Color c)
        {
            const int WIDTH = 200;
            const int HEIGHT = 24;
            int x = Screen.Width - WIDTH - 5;
            int y = 5 + index * (HEIGHT + 2);

            double p = time / total;

            str.Clear();
            str.Append(name);
            str.Append(" [");
            str.Append((time * 1000).ToString("F1"));
            str.Append("ms, ");
            str.Append((p * 100.0).ToString("F1"));
            str.Append("%]");

            Debug.Box(new Rectangle(x, y, WIDTH, HEIGHT), Color.DimGray);
            Debug.Box(new Rectangle(x, y, (int)Math.Round(p * WIDTH), HEIGHT), c);
            Debug.TextAt(str.ToString(), new Vector2(x + 3, y + 2), Color.Black);
        }

        #region Logging

        public static void Trace(string text)
        {
            Print(LogLevel.TRACE, text, ConsoleColor.Gray);
        }

        public static void Log(string text)
        {
            Print(LogLevel.INFO, text, ConsoleColor.White);
        }

        public static void Warn(string text)
        {
            Print(LogLevel.WARN, text, ConsoleColor.Yellow);
        }

        public static void Error(string text, Exception e = null)
        {
            if(text != null)
            {
                Print(LogLevel.ERROR, text, ConsoleColor.Red);
            }

            if(e != null)
            {
                Print(LogLevel.ERROR, e.GetType().FullName + ": " + e.Message, ConsoleColor.Red);
                Print(LogLevel.ERROR, e.StackTrace, ConsoleColor.Red);
            }
        }

        private static void Print(LogLevel level, string text, ConsoleColor colour = ConsoleColor.White)
        {
            if (!LogEnabled)
                return;

            string stack = Environment.StackTrace;

            string lvl = null;
            switch (level)
            {
                case LogLevel.TRACE:
                    lvl = "[TRACE] ";
                    break;
                case LogLevel.INFO:
                    lvl = "[INFO] ";
                    break;
                case LogLevel.WARN:
                    lvl = "[WARN] ";
                    break;
                case LogLevel.ERROR:
                    lvl = "[ERROR] ";
                    break;
            }
            lvl = lvl.PadRight(8);
            stringBuilder.Append(lvl);
            if (LogPrintLevel)
                textBuilder.Append(lvl);

            DateTime t = DateTime.Now;
            string time = $"[{t.Hour}:{t.Minute}:{t.Second}.{t.Millisecond / 10}] ";
            time = time.PadRight(15);
            stringBuilder.Append(time);
            if (LogPrintTime)
                textBuilder.Append(time);

            textBuilder.Append(text ?? "<null>");
            stringBuilder.Append(text ?? "<null>");

            stringBuilder.AppendLine();
            stringBuilder.Append(stack);            

            if(level != LogLevel.TRACE || (LogEnableTrace))
            {
                if (Console.ForegroundColor != colour)
                    Console.ForegroundColor = colour;
                if (Highlight)
                {
                    switch (level)
                    {
                        case LogLevel.INFO:
                            Console.BackgroundColor = ConsoleColor.DarkCyan;
                            break;
                        case LogLevel.WARN:
                            Console.BackgroundColor = ConsoleColor.DarkYellow;
                            break;
                        case LogLevel.ERROR:
                            Console.BackgroundColor = ConsoleColor.DarkRed;
                            break;
                    }
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                }

                Console.WriteLine(textBuilder.ToString());
                Console.BackgroundColor = ConsoleColor.Black;
            }

            logEntries.Add(stringBuilder.ToString());
            stringBuilder.Clear();
            textBuilder.Clear();

            if (LogCallingMethod)
            {
                string[] split = stack.Split('\n');
                string interesting = split[4];
                string[] inSplit = interesting.Split('(');
                string methodName = inSplit[0].Substring(6);
                string fullPath = interesting.Substring(interesting.LastIndexOf(')'));
                fullPath = fullPath.Substring(fullPath.LastIndexOf('\\') + 1);

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"   from [{methodName.Trim()} -> {fullPath.Trim()}]");
            }            
        }

        #endregion
    }

    public enum LogLevel
    {
        TRACE,
        INFO,
        WARN,
        ERROR
    }
}
