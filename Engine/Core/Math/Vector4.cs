using GlmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Vector4
    {
        private vec4 point;

        public float x { get => point.x; set => point.x = value; }

        public float y { get => point.y; set => point.y = value; }

        public float z { get => point.z; set => point.z = value; }

        public float w { get => point.w; set => point.w = value; }

        public Vector4(float X, float Y, float Z, float W)
        {
            point.x = X;
            point.y = Y;
            point.z = Z;
            point.w = W;
        }

        public Vector4(vec4 v)
        {
            point = v;
        }

        public Vector4 Normalized => new Vector4(point.NormalizedSafe);

        public float Magnitude => point.Length;

        public float MagnitudeSqr => point.LengthSqr;

        public void Normalize() => point = point.NormalizedSafe;

        public static float Dot(Vector4 first, Vector4 other) => glm.Dot((vec4)first, other);

        public static Vector4 Cross(Vector4 first, Vector4 other) => glm.Cross(first, other);

        public override string ToString()
        {
            return $"({point.x}, {point.y}, {point.z})";
        }

        public override int GetHashCode()
        {
            return ((((point.x.GetHashCode() * 397) ^ point.y.GetHashCode()) * 397) ^ (point.z.GetHashCode()) * 397) ^ point.w.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is Vector4 v)
            {
                return v.point.x.Equals(point.x) && v.point.y.Equals(point.y) && v.point.z.Equals(point.z) && v.point.w.Equals(point.w);
            }

            return false;
        }

        public static implicit operator vec3(Vector4 v) => new vec3(v.x, v.y, v.z);
        public static implicit operator vec4(Vector4 v) => new vec4(v.x, v.y, v.z, v.w);
        public static implicit operator Vector4(vec3 v) => new Vector4(v.x, v.y, v.z, 0);
        public static implicit operator Vector4(vec4 v) => new Vector4(v.x, v.y, v.z, v.w);
        public static implicit operator Vector4(Vector2 v) => new Vector4(v.x, v.y, 0, 0);

        public static Vector4 operator +(Vector4 a, Vector4 b) => new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);

        public static Vector4 operator -(Vector4 a, Vector4 b) => new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);

        public static Vector4 operator *(Vector4 a, Vector4 b) => new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);

        public static Vector4 operator *(Vector4 a, float b) => new Vector4(a.x * b, a.y * b, a.z * b, a.w * b);

        public static Vector4 operator /(Vector4 a, Vector4 b) => new Vector4(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);

        public static Vector4 operator /(Vector4 a, float b) => new Vector4(a.x / b, a.y / b, a.z / b, a.w / b);

        public static bool operator ==(Vector4 a, Vector4 b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;

        public static bool operator !=(Vector4 a, Vector4 b) => a.x != b.x || a.y != b.y || a.z != b.z || a.w != b.w;

        public static readonly Vector4 forward = new Vector4(0, 0, 1, 0);
        public static readonly Vector4 back = new Vector4(0, 0, -1, 0);
        public static readonly Vector4 up = new Vector4(0, 1, 0, 0);
        public static readonly Vector4 down = new Vector4(0, 1, 0, 0);
        public static readonly Vector4 right = new Vector4(1, 0, 0, 0);
        public static readonly Vector4 left = new Vector4(-1, 0, 0, 0);
        public static readonly Vector4 one = new Vector4(1, 1, 1, 1);
        public static readonly Vector4 zero = new Vector4(0, 0, 0, 0);
    }
}
