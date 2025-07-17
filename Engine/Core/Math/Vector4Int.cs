using MessagePack;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// 3D Vector that uses ints instead of floats
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 0)]
[MessagePackObject]
public struct Vector4Int
{
    [Key(0)]
    public int X;

    [Key(1)]
    public int Y;

    [Key(2)]
    public int Z;

    [Key(3)]
    public int W;

    public Vector4Int()
    {
    }

    public Vector4Int(int X, int Y, int Z, int W)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
        this.W = W;
    }

    [IgnoreMember]
    public static readonly Vector4Int Zero = new();

    [IgnoreMember]
    public static readonly Vector4Int One = new(1, 1, 1, 1);

    public static implicit operator Vector4(Vector4Int v) => new(v.X, v.Y, v.Z, v.W);

    public static implicit operator Vector4Int(Vector4 v) => new((int)v.X, (int)v.Y, (int)v.Z, (int)v.W);

    public static Vector4Int operator +(Vector4Int a, Vector4Int b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);

    public static Vector4Int operator -(Vector4Int a, Vector4Int b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);

    public static Vector4Int operator *(Vector4Int a, Vector4Int b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);

    public static Vector4Int operator *(Vector4Int a, int b) => new(a.X * b, a.Y * b, a.Z * b, a.W * b);

    public static Vector4Int operator *(Vector4Int a, float b) => new((int)(a.X * b), (int)(a.Y * b), (int)(a.Z * b), (int)(a.W * b));

    public static Vector4Int operator /(Vector4Int a, Vector4Int b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);

    public static Vector4Int operator /(Vector4Int a, int b) => new(a.X / b, a.Y / b, a.Z / b, a.W / b);

    public static Vector4Int operator /(Vector4Int a, float b) => new((int)(a.X / b), (int)(a.Y / b), (int)(a.Z / b), (int)(a.W / b));

    public static bool operator ==(Vector4Int a, Vector4Int b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;

    public static bool operator !=(Vector4Int a, Vector4Int b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z || a.W != b.W;

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }

    public override readonly bool Equals(object obj)
    {
        if (obj == null || obj is not Vector4Int v)
        {
            return false;
        }

        return X == v.X && Y == v.Y && Z == v.Z && W == v.W;
    }

    public override readonly string ToString()
    {
        return $"({X}, {Y}, {Z}, {W})";
    }
}
