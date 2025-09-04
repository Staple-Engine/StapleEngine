using System;
using System.Numerics;

namespace Staple;

/// <summary>
/// Randomization class, provides random numbers
/// </summary>
public class Randomizer
{
    private readonly Random random;

    /// <summary>
    /// The Seed of the randomizer.
    /// </summary>
    public int Seed { get; private set; }

    /// <summary>
    /// Default instance of the randomizer.
    /// </summary>
    public static readonly Randomizer Default = new();

    /// <summary>
    /// Creates the randomizer. It will set its seed to the current timestamp.
    /// </summary>
    public Randomizer()
    {
        Seed = (int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        random = new Random(Seed);
    }

    /// <summary>
    /// Creates the randomizer with a specific seed.
    /// </summary>
    /// <param name="seed">The seed to use</param>
    public Randomizer(int seed)
    {
        Seed = seed;

        random = new Random(seed);
    }

    /// <summary>
    /// Calculates a random float value between two values
    /// </summary>
    /// <param name="min">The min value</param>
    /// <param name="max">The max value</param>
    /// <returns>The random value</returns>
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

    /// <summary>
    /// Calculates a random int value between two values
    /// </summary>
    /// <param name="min">The min value</param>
    /// <param name="max">The max value</param>
    /// <returns>The random value</returns>
    public int Random(int min, int max)
    {
        return random.Next(min, max);
    }

    /// <summary>
    /// Gets a random normalized float value.
    /// </summary>
    /// <returns>A float value between 0 and 1</returns>
    public float RandomNormalized()
    {
        return Random(1.175494e-38f, float.MaxValue) / float.MaxValue;
    }

    /// <summary>
    /// Gets a randomized point in a sphere
    /// </summary>
    /// <returns>The randomized point</returns>
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
