using GlmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public static class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float value) => value < 0.0f ? (int)(value - 0.5f) : (int)(value + 0.5f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundToInt(float value) => value < 0.0f ? (int)(value - 0.5f) : (int)(value + 0.5f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Deg2Rad(float angle) => glm.Radians(angle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Rad2Deg(float angle) => glm.Degrees(angle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Abs(float f) => glm.Abs(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Acos(float f) => glm.Acos(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Asin(float f) => glm.Asin(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atan(float f) => glm.Atan(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atan2(float x, float y) => (float)System.Math.Atan2(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Ceil(float f) => glm.Ceiling(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilToInt(float f) => (int)glm.Ceiling(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float x, float min, float max) => x < min ? min : x > max ? max : x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(float x) => x < 0 ? 0 : x > 1 ? 1 : x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cos(float x) => glm.Cos(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float x) => glm.Sin(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Exp(float x) => glm.Exp(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Floor(float f) => glm.Floor(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorToInt(float f) => (int)glm.Floor(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(int x) => (x & 1) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log(float f) => glm.Log(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log10(float f) => glm.Log10(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(params float[] f)
        {
            float t;

            if(f.Length > 0)
            {
                t = f[0];
            }
            else
            {
                return 0;
            }

            var length = f.Length;

            for(var i = 1; i < length; i++)
            {
                t = glm.Max(t, f[i]);
            }

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(params float[] f)
        {
            float t;

            if (f.Length > 0)
            {
                t = f[0];
            }
            else
            {
                return 0;
            }

            var length = f.Length;

            for (var i = 1; i < length; i++)
            {
                t = glm.Min(t, f[i]);
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
            int power = 2;
            value--;

            while((value >>= 1) != 0)
            {
                power <<= 1;
            }

            return power;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Pow(float f, float p) => glm.Pow(f, p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sign(float value) => glm.Sign(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sqrt(float value) => glm.Sqrt(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tan(float value) => glm.Tan(value);

        public static readonly float PI = (float)System.Math.PI;

        public static readonly float Epsilon = float.Epsilon;
    }
}
