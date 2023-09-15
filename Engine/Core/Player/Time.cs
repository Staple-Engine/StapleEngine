using System;

namespace Staple
{
    /// <summary>
    /// Time information class
    /// </summary>
    public class Time
    {
        /// <summary>
        /// The current time in seconds since booting the app
        /// </summary>
        public static float time { get; internal set; }

        /// <summary>
        /// The last frame's delta time
        /// </summary>
        public static float deltaTime { get; internal set; }

        /// <summary>
        /// The fixed tick delta time
        /// </summary>
        public static float fixedDeltaTime { get; internal set; }

        /// <summary>
        /// The current frame rate
        /// </summary>
        public static int FPS { get; internal set; }

        /// <summary>
        /// The current time accumulator
        /// </summary>
        internal static float Accumulator { get; set; }

        /// <summary>
        /// Called when the accumulator triggers
        /// </summary>
        internal static Action OnAccumulatorFinished { get; set; }

        private static int frames;
        private static float frameTimer;

        /// <summary>
        /// Updates the clock
        /// </summary>
        /// <param name="current">The current time</param>
        /// <param name="last">The previous time</param>
        internal static void UpdateClock(DateTime current, DateTime last)
        {
            var delta = (float)(current - last).TotalSeconds;

            time += delta;

            //If we're larger than 1 we're definitely dealing with suspend or an extremely slow system
            if (delta > 1)
            {
                delta = 0;
            }

            Accumulator += delta;

            deltaTime = delta;

            var previousAccumulator = Accumulator;

            if(fixedDeltaTime > 0)
            {
                while (Accumulator >= fixedDeltaTime)
                {
                    Accumulator -= fixedDeltaTime;
                }
            }

            if(Accumulator < previousAccumulator)
            {
                OnAccumulatorFinished?.Invoke();
            }

            frames++;
            frameTimer += delta;

            if(frameTimer >= 1.0f)
            {
                FPS = frames;

                frameTimer = 0;
                frames = 0;
            }
        }
    }
}
