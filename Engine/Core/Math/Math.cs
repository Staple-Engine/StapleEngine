using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        public static float Deg2Rad(float angle) => angle * PI / 180;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Rad2Deg(float angle) => angle * 180 / PI;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Ceil(float f) => (float)System.Math.Ceiling(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilToInt(float f) => (int)System.Math.Ceiling(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float x, float min, float max) => x < min ? min : x > max ? max : x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(float x) => x < 0 ? 0 : x > 1 ? 1 : x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cos(float x) => (float)System.Math.Cos(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float x) => (float)System.Math.Sin(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Exp(float x) => (float)System.Math.Exp(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Floor(float f) => (float)System.Math.Floor(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorToInt(float f) => (int)System.Math.Floor(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(int x) => (x & 1) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log(float f) => (float)System.Math.Log(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log10(float f) => (float)System.Math.Log10(f);

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
                t = System.Math.Max(t, f[i]);
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
                t = System.Math.Min(t, f[i]);
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
        public static float Pow(float f, float p) => (float)System.Math.Pow(f, p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sign(float value) => System.Math.Sign(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sqrt(float value) => (float)System.Math.Sqrt(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tan(float value) => (float)System.Math.Tan(value);

        public static readonly float PI = (float)System.Math.PI;

        public static readonly float Epsilon = float.Epsilon;

        public static Matrix4x4 Ortho(float left, float right, float bottom, float top, float zNear, float zFar)
        {
            Matrix4x4 outValue = Matrix4x4.Identity;

            outValue.M11 = 2.0f / (right - left);
            outValue.M22 = 2.0f / (bottom - top);
            outValue.M33 = 1.0f / (zFar - zNear);
            outValue.M41 = (left + right) / (left - right);
            outValue.M42 = (top + bottom) / (top - bottom);
            outValue.M43 = zNear / (zNear - zFar);

            return outValue;
        }

    }
}
