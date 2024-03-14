using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple;

/// <summary>
/// Math utilities
/// </summary>
public static class Math
{
    public static readonly float PI = (float)System.Math.PI;

    public static readonly float Epsilon = float.Epsilon;

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

    /// <summary>
    /// Converts degrees to radians
    /// </summary>
    /// <param name="angle">The angle as degrees</param>
    /// <returns>The radians</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Deg2Rad(float angle) => angle * PI / 180;

    /// <summary>
    /// Converts radians to degrees
    /// </summary>
    /// <param name="angle">The angle as radians</param>
    /// <returns>The degrees</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Rad2Deg(float angle) => angle * 180 / PI;

    /// <summary>
    /// Gets the absolute value of a float
    /// </summary>
    /// <param name="f">The float</param>
    /// <returns>The absolute value</returns>
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
    public static float Log(float f, float newBase) => (float)System.Math.Log(f, newBase);

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

    public static Vector2 ToVector2(this Vector3 v)
    {
        return new Vector2(v.X, v.Y);
    }

    public static Vector2 ToVector2(this Vector4 v)
    {
        return new Vector2(v.X, v.Y);
    }

    public static Vector3 ToVector3(this Vector2 v)
    {
        return new Vector3(v.X, v.Y, 0);
    }

    public static Vector3 ToVector3(this Vector4 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    public static Vector4 ToVector4(this Vector2 v, bool transform = false)
    {
        return new Vector4(v.X, v.Y, 0, transform ? 1 : 0);
    }

    public static Vector4 ToVector4(this Vector3 v, bool transform = false)
    {
        return new Vector4(v.X, v.Y, v.Z, transform ? 1 : 0);
    }

    /// <summary>
    /// Converts a quaternion to a vector3 representation of each angle rotation as degrees
    /// </summary>
    /// <param name="q">The quaternion</param>
    /// <returns>The angles as a vector3 of degrees</returns>
    public static Vector3 ToEulerAngles(this Quaternion q)
    {
        var outValue = new Vector3();

        float squareX = q.X * q.X;
        float squareY = q.Y * q.Y;
        float squareZ = q.Z * q.Z;
        float squareW = q.W * q.W;
        float unit = squareX + squareY + squareZ + squareW;
        float test = q.X * q.W - q.Y * q.Z;

        static float MatchBounds(float x)
        {
            while (x < -360)
            {
                x += 360;
            }

            while (x > 360)
            {
                x -= 360;
            }

            return x;
        }

        Vector3 Normalize()
        {
            outValue.X = MatchBounds(Rad2Deg(outValue.X));
            outValue.Y = MatchBounds(Rad2Deg(outValue.Y));
            outValue.Z = MatchBounds(Rad2Deg(outValue.Z));

            return outValue;
        }

        if (test > 0.4995f * unit)
        {
            outValue.Y = 2.0f * Atan2(q.Y, q.X);
            outValue.X = PI / 2;

            return Normalize();
        }
        else if(test < -0.4995f * unit)
        {
            outValue.Y = -2.0f * Atan2(q.Y, q.X);
            outValue.X = -PI / 2;

            return Normalize();
        }

        var q2 = new Quaternion(q.W, q.Z, q.X, q.Y);

        outValue.X = Asin(2.0f * (q2.X * q2.Z - q2.W * q2.Y));
        outValue.Y = Atan2(2.0f * q2.X * q2.W + 2.0f * q2.Y * q2.Z,
            1 - 2.0f * (q2.Z * q2.Z + q2.W * q2.W));
        outValue.Z = Atan2(2.0f * q2.X * q2.Y + 2.0f * q2.Z * q2.W,
            1 - 2.0f * (q2.Y * q2.Y + q2.Z * q2.Z));

        return Normalize();
    }

    /// <summary>
    /// Creates a quaternion from a vector3 rotation with each member representing angles in degrees for that axis
    /// </summary>
    /// <param name="angles">The rotation per axis in degrees</param>
    /// <returns>The new quaternion</returns>
    public static Quaternion FromEulerAngles(Vector3 angles)
    {
        return Quaternion.CreateFromYawPitchRoll(Deg2Rad(angles.Y), Deg2Rad(angles.X), Deg2Rad(angles.Z));
    }

    /// <summary>
    /// Creates a transformation matrix
    /// </summary>
    /// <param name="position">The position</param>
    /// <param name="scale">The scale</param>
    /// <param name="rotation">The rotation</param>
    /// <returns>The transformation matrix</returns>
    public static Matrix4x4 TransformationMatrix(Vector3 position, Vector3 scale, Quaternion rotation)
    {
        return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
    }

    /// <summary>
    /// Creates a left-handed orthographic projection matrix
    /// </summary>
    /// <param name="left">left coordinate</param>
    /// <param name="right">right coordinate</param>
    /// <param name="bottom">bottom coordinate</param>
    /// <param name="top">top coordinate</param>
    /// <param name="zNear">near plane</param>
    /// <param name="zFar">far plane</param>
    /// <returns>The matrix</returns>
    public static Matrix4x4 OrthoLeftHanded(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        var outValue = Matrix4x4.Identity;

        outValue.M11 = 2.0f / (right - left);
        outValue.M22 = 2.0f / (bottom - top);
        outValue.M33 = 1.0f / (zFar - zNear);
        outValue.M41 = (left + right) / (left - right);
        outValue.M42 = (top + bottom) / (bottom - top);
        outValue.M43 = zNear / (zNear - zFar);

        return outValue;
    }

    /// <summary>
    /// Creates a right-handed orthographic projection matrix
    /// </summary>
    /// <param name="left">left coordinate</param>
    /// <param name="right">right coordinate</param>
    /// <param name="bottom">bottom coordinate</param>
    /// <param name="top">top coordinate</param>
    /// <param name="zNear">near plane</param>
    /// <param name="zFar">far plane</param>
    /// <returns>The matrix</returns>
    public static Matrix4x4 OrthoRightHanded(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        var outValue = Matrix4x4.Identity;

        outValue.M11 = 2.0f / (right - left);
        outValue.M22 = 2.0f / (top - bottom);
        outValue.M33 = 1.0f / (zNear - zFar);
        outValue.M41 = (left + right) / (left - right);
        outValue.M42 = (top + bottom) / (bottom - top);
        outValue.M43 = zNear / (zNear - zFar);

        return outValue;
    }

    /// <summary>
    /// Calculates a quaternion for a direction and up axis
    /// </summary>
    /// <param name="forward">The direction</param>
    /// <param name="up">The up axis</param>
    /// <returns>The rotation</returns>
    /// <remarks>From https://discussions.unity.com/t/what-is-the-source-code-of-quaternion-lookrotation/72474</remarks>
    public static Quaternion LookAt(Vector3 forward, Vector3 up)
    {
        var vector = Vector3.Normalize(forward);
        var vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
        var vector3 = Vector3.Cross(vector, vector2);

        var m00 = vector2.X;
        var m01 = vector2.Y;
        var m02 = vector2.Z;
        var m10 = vector3.X;
        var m11 = vector3.Y;
        var m12 = vector3.Z;
        var m20 = vector.X;
        var m21 = vector.Y;
        var m22 = vector.Z;

        float num8 = (m00 + m11) + m22;

        var quaternion = new Quaternion();

        if (num8 > 0f)
        {
            var num = (float)Sqrt(num8 + 1f);

            quaternion.W = num * 0.5f;

            num = 0.5f / num;

            quaternion.X = (m12 - m21) * num;
            quaternion.Y = (m20 - m02) * num;
            quaternion.Z = (m01 - m10) * num;

            return quaternion;
        }
        if ((m00 >= m11) && (m00 >= m22))
        {
            var num7 = (float)Sqrt(((1f + m00) - m11) - m22);
            var num4 = 0.5f / num7;

            quaternion.X = 0.5f * num7;
            quaternion.Y = (m01 + m10) * num4;
            quaternion.Z = (m02 + m20) * num4;
            quaternion.W = (m12 - m21) * num4;

            return quaternion;
        }

        if (m11 > m22)
        {
            var num6 = (float)Sqrt(((1f + m11) - m00) - m22);
            var num3 = 0.5f / num6;

            quaternion.X = (m10 + m01) * num3;
            quaternion.Y = 0.5f * num6;
            quaternion.Z = (m21 + m12) * num3;
            quaternion.W = (m20 - m02) * num3;

            return quaternion;
        }

        var num5 = (float)Sqrt(((1f + m22) - m00) - m11);
        var num2 = 0.5f / num5;

        quaternion.X = (m20 + m02) * num2;
        quaternion.Y = (m21 + m12) * num2;
        quaternion.Z = 0.5f * num5;
        quaternion.W = (m01 - m10) * num2;

        return quaternion;
    }
}
