using System;
using System.Numerics;

namespace Staple
{
    public class Randomizer
    {
        private const int randMax = 32767;

        private Random random;

        public int Seed { get; private set; }

        public static readonly Randomizer Default = new Randomizer();

        public Randomizer()
        {
            Seed = (int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            random = new Random(Seed);
        }

        public Randomizer(int seed)
        {
            Seed = seed;

            random = new Random(seed);
        }

        public float Random(float min, float max)
        {
            if(max - min < 0)
            {
                return 0;
            }

            var next = random.Next();
            var diff = max - min;

            return next / (float)int.MaxValue * diff + min;
        }

        public int Random(int min, int max)
        {
            return random.Next(min, max);
        }

        public float RandomNormalized()
        {
            return Random(1.175494e-38f, float.MaxValue) / float.MaxValue;
        }

        public Vector3 RandomNormalizedSphere()
        {
            var theta = 2 * Math.PI * RandomNormalized();
            var phi = Math.Acos(2 * RandomNormalized() - 1);
            var cosTheta = Math.Cos(theta);
            var cosPhi = Math.Cos(phi);
            var sinTheta = Math.Sin(theta);
            var sinPhi = Math.Sin(phi);

            return new Vector3(cosTheta * sinPhi, sinTheta * sinPhi, cosPhi);
        }
    }
}
