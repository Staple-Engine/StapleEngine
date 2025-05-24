using System.Numerics;
using ufbx;

namespace Baker;

internal static class UfbxExtensions
{
    public static Vector3 ToVector3(this UfbxVec3 v)
    {
        return new(v.X, v.Y, v.Z);
    }

    public static Quaternion ToQuaternion(this UfbxQuat v)
    {
        return new(v.X, v.Y, v.Z, v.W);
    }

    public static Vector3[] ToVector3Array(this UfbxVec3List v)
    {
        var outValue = new Vector3[v.Count];

        for (ulong i = 0; i < v.Count; i++)
        {
            var t = v[i];

            outValue[i] = new(t.X, t.Y, t.Z);
        }

        return outValue;
    }

    public static Matrix4x4 ToMatrix4x4(this UfbxMatrix m)
    {
        return new(m.M00, m.M01, m.M02, m.M03,
            m.M10, m.M11, m.M12, m.M13,
            m.M20, m.M21, m.M22, m.M23,
            0, 0, 0, 1);
    }
}
