using System;

namespace Staple.Internal;

public struct Glyph
{
    public int xAdvance;
    public int xOffset;
    public int yOffset;
    public Rect bounds;
    public RectFloat uvBounds;

    public byte[] bitmap;

    public static readonly Glyph Invalid = new()
    {
        bounds = new(0, 0, 0, 0),
        uvBounds = new(0, 0, 0, 0),
    };

    public static bool operator==(Glyph lhs, Glyph rhs)
    {
        return lhs.xAdvance == rhs.xAdvance &&
            lhs.xOffset == rhs.xOffset &&
            lhs.yOffset == rhs.yOffset &&
            lhs.bounds == rhs.bounds &&
            lhs.uvBounds == rhs.uvBounds;
    }

    public static bool operator!=(Glyph lhs, Glyph rhs)
    {
        return lhs.xAdvance != rhs.xAdvance ||
            lhs.xOffset != rhs.xOffset ||
            lhs.yOffset != rhs.yOffset ||
            lhs.bounds != rhs.bounds ||
            lhs.uvBounds != rhs.uvBounds;
    }

    public override bool Equals(object obj)
    {
        if(obj is Glyph rhs)
        {
            return this == rhs;
        }

        return false;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(xAdvance, xOffset, yOffset, bounds, uvBounds);
    }
}
