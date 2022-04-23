using GlmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public struct Vector3
    {
        private vec3 point;

        public float x { get => point.x; set => point.x = value; }

        public float y { get => point.y; set => point.y = value; }

        public float z { get => point.z; set => point.z = value; }

        public Vector3(float X, float Y, float Z)
        {
            point.x = X;
            point.y = Y;
            point.z = Z;
        }

        public static implicit operator vec3(Vector3 v) => new vec3(v.x, v.y, v.z);
        public static implicit operator vec4(Vector3 v) => new vec4(v.x, v.y, v.z, 0);
        public static implicit operator Vector3(vec3 v) => new Vector3(v.x, v.y, v.z);
        public static implicit operator Vector3(vec4 v) => new Vector3(v.x, v.y, v.z);

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);

        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);

        public static Vector3 operator *(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);

        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x * b, a.y * b, a.z * b);

        public static Vector3 operator /(Vector3 a, Vector3 b) => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);

        public static bool operator ==(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

        public static bool operator !=(Vector3 a, Vector3 b) => a.x != b.x || a.y != b.y || a.z != b.z;

        public static readonly Vector3 forward = new Vector3(0, 0, 1);
        public static readonly Vector3 back = new Vector3(0, 0, -1);
        public static readonly Vector3 up = new Vector3(0, 1, 0);
        public static readonly Vector3 down = new Vector3(0, 1, 0);
        public static readonly Vector3 right = new Vector3(1, 0, 0);
        public static readonly Vector3 left = new Vector3(-1, 0, 0);
    }
}
