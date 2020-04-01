using System.Diagnostics;

namespace GVS
{
    public static class Time
    {
        /// <summary>
        /// The elapsed (delta) time between frames after which the time will be clamped to this value.
        /// This means that you will not get values larger than this for deltaTime.
        /// Set to zero to disable.
        /// Also see <see cref="ResumeElapsedTime"/>.
        /// </summary>
        public static double MaxElapsedTime { get; set; } = 1.0;

        /// <summary>
        /// The elapsed time between frames, multiplied by the time scale.
        /// Multiply this value with another one to make it be 'per second' instead of 'per frame'.
        /// </summary>
        public static float deltaTime { get; private set; }

        /// <summary>
        /// /// The elapsed time between frames, multiplied by the time scale.
        /// Multiply this value with another one to make it be 'per second' instead of 'per frame'.
        /// Increased precision over <see cref="deltaTime"/>. 
        /// </summary>
        public static double doubleDeltaTime { get; private set; }

        /// <summary>
        /// The elapsed time between frames, unaffected by the time scale.
        /// Multiply this value with another one to make it be 'per second' instead of 'per frame'.
        /// </summary>
        public static float unscaledDeltaTime { get; private set; }

        /// <summary>
        /// /// The elapsed time between frames, unaffected by the time scale.
        /// Multiply this value with another one to make it be 'per second' instead of 'per frame'.
        /// Increased precision over <see cref="unscaledDeltaTime"/>. 
        /// </summary>
        public static double doubleUnscaledDeltaTime { get; private set; }

        /// <summary>
        /// True on any frame where the elapsed time between frames is greater than <see cref="MaxElapsedTime"/>.
        /// When this happens, the deltaTime value is clamped, which can lead to things running slower than they should but can avoid bugs.
        /// </summary>
        public static bool IsRunningSlowly { get; private set; }

        /// <summary>
        /// The total elapsed time in seconds since the game was launched. This IS affected by <see cref="TimeScale"/>,
        /// so it may not represent the real-world elapsed time.
        /// </summary>
        public static float time { get; private set; }

        /// <summary>
        /// The total elapsed time in seconds since the game was launched. This IS NOT affected by <see cref="TimeScale"/>,
        /// so it should, under ideal conditions, represents real-world elapsed time.
        /// </summary>
        public static float unscaledTime { get; private set; }

        /// <summary>
        /// The deltaTime scale factor. A value of 1 is normal time. A value smaller than 1 is slow-motion.
        /// A value greater than one will speed up time. Minimum value is 0, which will freeze time. There is no upper
        /// limit, but very high values may break things.
        /// </summary>
        public static double TimeScale
        {
            get
            {
                return timeScale;
            }
            set
            {
                timeScale = value;
                if (timeScale < 0f)
                    timeScale = 0f;
            }
        }

        private static double timeScale = 1f;
        private static readonly Stopwatch timer = new Stopwatch();
        private static bool forceNormalTime = false;

        public static void StartFrame()
        {
            // Pause timer.
            timer.Stop();

            // Reset variables.
            IsRunningSlowly = false;

            // Check elapsed time...
            var elapsed = timer.Elapsed.TotalSeconds;
            if (MaxElapsedTime != 0.0 && elapsed > MaxElapsedTime)
            {
                elapsed = MaxElapsedTime;
                IsRunningSlowly = true;
            }

            if (forceNormalTime)
            {
                IsRunningSlowly = false;
                elapsed = 1f / 60f;
                forceNormalTime = false;
            }

            doubleUnscaledDeltaTime = elapsed;
            unscaledDeltaTime = (float)doubleUnscaledDeltaTime;
            
            doubleDeltaTime = doubleUnscaledDeltaTime * TimeScale;
            deltaTime = (float)doubleDeltaTime;

            time += deltaTime;
            unscaledTime += unscaledDeltaTime;

            // Restart timer.
            timer.Restart();
        }

        /// <summary>
        /// Forces the deltaTime value to be 1/60 (60fps) for the next frame.
        /// Only affects a single frame.
        /// </summary>
        public static void ForceNormalTime()
        {
            forceNormalTime = true;
        }
    }
}
