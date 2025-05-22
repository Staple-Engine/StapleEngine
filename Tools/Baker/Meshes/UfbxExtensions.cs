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
}
