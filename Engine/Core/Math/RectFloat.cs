using MessagePack;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Staple;

/// <summary>
/// Represents a rectangle with float values
/// </summary>
[MessagePackObject]
public struct RectFloat
{
    [Key(0)]
    public float left;

    [Key(1)]
    public float right;

    [Key(2)]
    public float top;

    [Key(3)]
    public float bottom;

    public RectFloat()
    {
    }

    public RectFloat(Vector2 position, Vector2 size)
    {
        left = position.X;
        top = position.Y;
        right = position.X + size.X;
        bottom = position.Y + size.Y;
    }

    public RectFloat(float left, float right, float top, float bottom)
    {
        this.left = left;
        this.right = right;
        this.top = top;
        this.bottom = bottom;
    }

    [IgnoreMember]
    public readonly Vector2 Min => new(left, top);

    [IgnoreMember]
    public readonly Vector2 Max => new(right, bottom);

    [IgnoreMember]
    public float Width
    {
        readonly get => right - left;

        set
        {
            right = value + left;
        }
    }

    [IgnoreMember]
    public float Height
    {
        readonly get => bottom - top;

        set
        {
            bottom = value + top;
        }
    }

    public readonly bool Contains(Vector2Int v)
    {
        return v.X >= left && v.X <= right &&
            v.Y >= top && v.Y <= bottom;
    }

    public readonly bool Contains(Vector2 v)
    {
        return v.X >= left && v.X <= right &&
            v.Y >= top && v.Y <= bottom;
    }

    public static bool operator ==(RectFloat lhs, RectFloat rhs)
    {
        return lhs.left == rhs.left &&
            lhs.right == rhs.right &&
            lhs.top == rhs.top &&
            lhs.bottom == rhs.bottom;
    }

    public static bool operator !=(RectFloat lhs, RectFloat rhs)
    {
        return lhs.left != rhs.left ||
            lhs.right != rhs.right ||
            lhs.top != rhs.top ||
            lhs.bottom != rhs.bottom;
    }

    public override bool Equals([NotNullWhen(true)] object obj)
    {
        if (obj is RectFloat rhs)
        {
            return this == rhs;
        }

        return false;
    }

    public readonly bool ShouldSerializeMin() => false;

    public readonly bool ShouldSerializeMax() => false;

    public readonly bool ShouldSerializeWidth() => false;

    public readonly bool ShouldSerializeHeight() => false;

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(left, right, top, bottom);
    }
}
