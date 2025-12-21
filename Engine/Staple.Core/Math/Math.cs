using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple;

/// <summary>
/// Math utilities
/// </summary>
public static class Math
{
    public const float PI = (float)System.Math.PI;

    public const float Epsilon = float.Epsilon;

    public const float ZeroTolerance = 1e-4f;

    public const float Deg2Rad = PI / 180;

    public const float Rad2Deg = 180 / PI;

    public const float Infinity = float.PositiveInfinity;

    public const float NegativeInfinity = float.NegativeInfinity;

    /// <summary>
    /// Rounds a float value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>The rounded float</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Round(float value) => value < 0.0f ? (int)(value - 0.5f) : (int)(value + 0.5f);

    /// <summary>
    /// Rounds a float to int
    /// </summary>
    /// <param name="value">The float</param>
    /// <returns>The rounded int</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RoundToInt(float value) => value < 0.0f ? (int)(value - 0.5f) : (int)(value + 0.5f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Abs(float f) => (float)System.Math.Abs(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Acos(float f) => (float)System.Math.Acos(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Asin(float f) => (float)System.Math.Asin(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Atan(float f) => (float)System.Math.Atan(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Atan2(float x, float y) => (float)System.Math.Atan2(x, y);

    /// <summary>
    /// Rounds a float value to the next highest value
    /// </summary>
    /// <param name="f">The value</param>
    /// <returns>The rounded value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Ceil(float f) => (float)System.Math.Ceiling(f);

    /// <summary>
    /// Rounds a float value to the next highest value as an int
    /// </summary>
    /// <param name="f">The value</param>
    /// <returns>The rounded value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilToInt(float f) => (int)System.Math.Ceiling(f);

    /// <summary>
    /// Rounds a float value to the next lowest value
    /// </summary>
    /// <param name="f">The value</param>
    /// <returns>The rounded value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Floor(float f) => (float)System.Math.Floor(f);

    /// <summary>
    /// Rounds a float value to the next lowest value as an int
    /// </summary>
    /// <param name="f">The value</param>
    /// <returns>The rounded value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FloorToInt(float f) => (int)System.Math.Floor(f);

    /// <summary>
    /// Limits a value to a min/max range
    /// </summary>
    /// <param name="x">The value</param>
    /// <param name="min">The min value</param>
    /// <param name="max">The max value</param>
    /// <returns>The new value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float x, float min, float max) => x < min ? min : x > max ? max : x;

    /// <summary>
    /// Limits a value to 0/1
    /// </summary>
    /// <param name="x">The value</param>
    /// <returns>The new value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp01(float x) => x < 0 ? 0 : x > 1 ? 1 : x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cos(float x) => (float)System.Math.Cos(x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sin(float x) => (float)System.Math.Sin(x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Exp(float x) => (float)System.Math.Exp(x);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(int x) => (x & 1) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log(float f) => (float)System.Math.Log(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log(float f, float newBase) => (float)System.Math.Log(f, newBase);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log10(float f) => (float)System.Math.Log10(f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(params float[] f)
    {
        if(f.Length == 0)
        {
            return 0;
        }

        float t = f[0];

        var length = f.Length;

        for(var i = 1; i < length; i++)
        {
            var value = f[i];

            if (value > t)
            {
                t = value;
            }
        }

        return t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(params float[] f)
    {
        if (f.Length == 0)
        {
            return 0;
        }

        float t = f[0];

        var length = f.Length;

        for (var i = 1; i < length; i++)
        {
            var value = f[i];

            if (value < t)
            {
                t = value;
            }
        }

        return t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float MoveTowards(float current, float target, float maxDelta)
    {
        if(target - current < maxDelta)
        {
            return target;
        }

        return current + maxDelta;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPowerOfTwo(int value)
    {
        var power = 2;

        value--;

        while((value >>= 1) != 0)
        {
            power <<= 1;
        }

        return power;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CopySign(float x, float sign)
    {
        return x * Sign(sign);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Pow(float f, float p) => (float)System.Math.Pow(f, p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sign(float value) => System.Math.Sign(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sqrt(float value) => (float)System.Math.Sqrt(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Tan(float value) => (float)System.Math.Tan(value);

    /// <summary>
    /// Ensures a value repeats to a specific length
    /// </summary>
    /// <param name="t">The value</param>
    /// <param name="length">The length</param>
    /// <returns>The new value</returns>
    public static float Repeat(float t, float length) => t - Floor(t / length) * length;
}
