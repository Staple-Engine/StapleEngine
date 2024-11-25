using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// Represents a 3x3 matrix
/// </summary>
public struct Matrix3x3
{
    public float M11, M12, M13,
        M21, M22, M23,
        M31, M32, M33;

    public static Matrix3x3 Identity
    {
        get
        {
            var matrix = new Matrix3x3();

            matrix.M11 = matrix.M22 = matrix.M33 = 1;

            return matrix;
        }
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(HashCode.Combine(M11, M12, M13),
            HashCode.Combine(M21, M22, M23),
            HashCode.Combine(M31, M32, M33));
    }

    public static bool operator==(Matrix3x3 lhs, Matrix3x3 rhs)
    {
        return lhs.M11 == rhs.M11 &&
            lhs.M12 == rhs.M12 &&
            lhs.M13 == rhs.M13 &&
            lhs.M21 == rhs.M21 &&
            lhs.M22 == rhs.M22 &&
            lhs.M23 == rhs.M23 &&
            lhs.M31 == rhs.M31 &&
            lhs.M32 == rhs.M32 &&
            lhs.M33 == rhs.M33;
    }

    public static bool operator !=(Matrix3x3 lhs, Matrix3x3 rhs)
    {
        return lhs.M11 != rhs.M11 ||
            lhs.M12 != rhs.M12 ||
            lhs.M13 != rhs.M13 ||
            lhs.M21 != rhs.M21 ||
            lhs.M22 != rhs.M22 ||
            lhs.M23 != rhs.M23 ||
            lhs.M31 != rhs.M31 ||
            lhs.M32 != rhs.M32 ||
            lhs.M33 != rhs.M33;
    }

    public override readonly bool Equals([NotNullWhen(true)] object obj)
    {
        return obj is Matrix3x3 m && this == m;
    }
}
