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
    public struct Vector2
    {
        private vec2 point;

        public float x { get => point.x; set => point.x = value; }

        public float y { get => point.y; set => point.y = value; }

        public Vector2(float X, float Y)
        {
            point.x = X;
            point.y = Y;
        }

        public Vector2(vec2 v)
        {
            point = v;
        }

        public Vector2 Normalized => new Vector2(point.NormalizedSafe);

        public float Magnitude => point.Length;

        public float MagnitudeSqr => point.LengthSqr;

        public void Normalize() => point = point.NormalizedSafe;

        public static float Dot(Vector2 first, Vector2 other) => glm.Dot((vec2)first, other);

        public override string ToString()
        {
            return $"({point.x}, {point.y})";
        }

        public override int GetHashCode()
        {
            return (((point.x.GetHashCode() * 397) ^ point.y.GetHashCode()));
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is Vector2 v)
            {
                return v.point.x.Equals(point.x) && v.point.y.Equals(point.y);
            }

            return false;
        }

        public static implicit operator vec2(Vector2 v) => new vec2(v.x, v.y);
        public static implicit operator Vector2(vec2 v) => new Vector2(v.x, v.y);

        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);

        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);

        public static Vector2 operator *(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);

        public static Vector2 operator *(Vector2 a, float b) => new Vector2(a.x * b, a.y * b);

        public static Vector2 operator /(Vector2 a, Vector2 b) => new Vector2(a.x / b.x, a.y / b.y);

        public static Vector2 operator /(Vector2 a, float b) => new Vector2(a.x / b, a.y / b);

        public static bool operator ==(Vector2 a, Vector2 b) => a.x == b.x && a.y == b.y;

        public static bool operator !=(Vector2 a, Vector2 b) => a.x != b.x || a.y != b.y;

        public static readonly Vector2 up = new Vector2(0, 1);
        public static readonly Vector2 down = new Vector2(0, 1);
        public static readonly Vector2 right = new Vector2(1, 0);
        public static readonly Vector2 left = new Vector2(-1, 0);
        public static readonly Vector2 one = new Vector2(1, 1);
        public static readonly Vector2 zero = new Vector2(0, 0);
    }
}
