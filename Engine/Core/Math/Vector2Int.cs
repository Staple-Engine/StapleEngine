using System.Runtime.InteropServices;
using System;
using MessagePack;

namespace Staple;

/// <summary>
/// 2D Vector that uses ints instead of floats
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 0)]
[MessagePackObject]
public struct Vector2Int
{
    [Key(0)]
    public int X;

    [Key(1)]
    public int Y;

    public Vector2Int()
    {
    }

    public Vector2Int(int X, int Y)
    {
        this.X = X;
        this.Y = Y;
    }

    [IgnoreMember]
    public static readonly Vector2Int Zero = new();

    [IgnoreMember]
    public static readonly Vector2Int One = new(1, 1);

    public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new(a.X + b.X, a.Y + b.Y);

    public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new(a.X - b.X, a.Y - b.Y);

    public static Vector2Int operator *(Vector2Int a, Vector2Int b) => new(a.X * b.X, a.Y * b.Y);

    public static Vector2Int operator *(Vector2Int a, int b) => new(a.X * b, a.Y * b);

    public static Vector2Int operator *(Vector2Int a, float b) => new((int)(a.X * b), (int)(a.Y * b));

    public static Vector2Int operator /(Vector2Int a, Vector2Int b) => new(a.X / b.X, a.Y / b.Y);

    public static Vector2Int operator /(Vector2Int a, int b) => new(a.X / b, a.Y / b);

    public static Vector2Int operator /(Vector2Int a, float b) => new((int)(a.X / b), (int)(a.Y / b));

    public static bool operator ==(Vector2Int a, Vector2Int b) => a.X == b.X && a.Y == b.Y;

    public static bool operator !=(Vector2Int a, Vector2Int b) => a.X != b.X || a.Y != b.Y;

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override readonly bool Equals(object obj)
    {
        if (obj == null || obj is not Vector2Int v)
        {
            return false;
        }

        return X == v.X && Y == v.Y;
    }

    public override readonly string ToString()
    {
        return $"({X}, {Y})";
    }
}
