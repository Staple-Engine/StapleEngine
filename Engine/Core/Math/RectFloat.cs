using MessagePack;
using System.Numerics;

namespace Staple;

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

    public bool ShouldSerializeMin() => false;

    public bool ShouldSerializeMax() => false;

    public bool ShouldSerializeWidth() => false;

    public bool ShouldSerializeHeight() => false;
}
