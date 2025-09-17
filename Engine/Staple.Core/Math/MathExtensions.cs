using System.Numerics;

namespace Staple;

public static class Vector2Extensions
{
    extension(Vector2 v)
    {
        public Vector2 Normalized => Vector2.Normalize(v);

        public Vector2 Transformed(Matrix4x4 matrix) => Vector2.Transform(v, matrix);

        public Vector2 Transformed(Quaternion quaternion) => Vector2.Transform(v, quaternion);
    }
}

public static class Vector3Extensions
{
    extension(Vector3 v)
    {
        public static Vector3 Right => Vector3.UnitX;

        public static Vector3 Up => Vector3.UnitY;

        public static Vector3 Forward => Vector3.UnitZ;

        public static Vector3 Left => -Vector3.UnitX;

        public static Vector3 Down => -Vector3.UnitY;

        public static Vector3 Back => -Vector3.UnitZ;

        public Vector3 Normalized => Vector3.Normalize(v);

        public Vector3 Transformed(Matrix4x4 matrix) => Vector3.Transform(v, matrix);

        public Vector3 Transformed(Quaternion quaternion) => Vector3.Transform(v, quaternion);

        public Vector3 Cross(Vector3 other) => Vector3.Cross(v, other);
    }
}

public static class Vector4Extensions
{
    extension(Vector4 v)
    {
        public static Vector4 Right => Vector4.UnitX;

        public static Vector4 Up => Vector4.UnitY;

        public static Vector4 Forward => Vector4.UnitZ;

        public static Vector4 Left => -Vector4.UnitX;

        public static Vector4 Down => -Vector4.UnitY;

        public static Vector4 Back => -Vector4.UnitZ;

        public Vector4 Normalized => Vector4.Normalize(v);

        public Vector4 Transformed(Matrix4x4 matrix) => Vector4.Transform(v, matrix);

        public Vector4 Transformed(Quaternion quaternion) => Vector4.Transform(v, quaternion);

        public Vector4 Cross(Vector4 other) => Vector4.Cross(v, other);
    }
}
