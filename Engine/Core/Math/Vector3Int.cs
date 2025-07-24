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
public struct Vector3Int
{
    [Key(0)]
    public int X;

    [Key(1)]
    public int Y;

    [Key(2)]
    public int Z;

    public Vector3Int()
    {
    }

    public Vector3Int(int X, int Y, int Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }

    [IgnoreMember]
    public static readonly Vector3Int Zero = new();

    [IgnoreMember]
    public static readonly Vector3Int One = new(1, 1, 1);

    public static implicit operator Vector3(Vector3Int v) => new(v.X, v.Y, v.Z);

    public static implicit operator Vector3Int(Vector3 v) => new((int)v.X, (int)v.Y, (int)v.Z);

    public static Vector3Int operator+(Vector3Int a, Vector3Int b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3Int operator-(Vector3Int a, Vector3Int b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3Int operator*(Vector3Int a, Vector3Int b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    public static Vector3Int operator*(Vector3Int a, int b) => new(a.X * b, a.Y * b, a.Z * b);

    public static Vector3Int operator*(Vector3Int a, float b) => new((int)(a.X * b), (int)(a.Y * b), (int)(a.Z * b));

    public static Vector3Int operator/(Vector3Int a, Vector3Int b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

    public static Vector3Int operator/(Vector3Int a, int b) => new(a.X / b, a.Y / b, a.Z / b);

    public static Vector3Int operator/(Vector3Int a, float b) => new((int)(a.X / b), (int)(a.Y / b), (int)(a.Z / b));

    public static bool operator==(Vector3Int a, Vector3Int b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static bool operator!=(Vector3Int a, Vector3Int b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public override readonly bool Equals(object obj)
    {
        if (obj == null || obj is not Vector3Int v)
        {
            return false;
        }

        return X == v.X && Y == v.Y && Z == v.Z;
    }

    public override readonly string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}
