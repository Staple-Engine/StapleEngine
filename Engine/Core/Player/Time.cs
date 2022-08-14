using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class Time
    {
        public static float time { get; internal set; }

        public static float deltaTime { get; internal set; }

        public static float fixedDeltaTime { get; internal set; }

        public static int FPS { get; internal set; }

        internal static float Accumulator { get; set; }

        private static int frames;
        private static float frameTimer;

        internal static void UpdateClock(double current, double last)
        {
            var delta = (float)(current - last);

            time += delta;
            Accumulator += delta;

            //If we're larger than 1 we're definitely dealing with suspend or an extremely slow system
            if (delta > 1)
            {
                delta = 0;
            }

            deltaTime = delta;

            if(fixedDeltaTime > 0)
            {
                while (Accumulator >= fixedDeltaTime)
                {
                    Accumulator -= fixedDeltaTime;
                }
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
