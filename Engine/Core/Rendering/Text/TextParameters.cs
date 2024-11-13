using System;
using System.Numerics;

namespace Staple;

public struct TextParameters
{
    public Color textColor = Color.White;
    public Color secondaryTextColor = Color.White;
    public Color borderColor = Color.Clear;
    public Vector2 position;
    public int borderSize;
    public float rotation;
    public int fontSize = 12;

    internal string font;

    public TextParameters()
    {
    }

    public override int GetHashCode()
    {
        return textColor.GetHashCode() ^
            secondaryTextColor.GetHashCode() ^
            borderColor.GetHashCode() ^
            position.GetHashCode() ^
            borderSize.GetHashCode() ^
            rotation.GetHashCode() ^
            fontSize.GetHashCode();
    }

    public TextParameters Clone()
    {
        return new()
        {
            borderColor = borderColor,
            borderSize = borderSize,
            position = position,
            fontSize = fontSize,
            rotation = rotation,
            secondaryTextColor = secondaryTextColor,
            textColor = textColor,
            font = font,
        };
    }

    public TextParameters Rotate(float rotation)
    {
        this.rotation = rotation;

        return this;
    }

    public TextParameters FontSize(int fontSize)
    {
        this.fontSize = fontSize;

        return this;
    }

    public TextParameters TextColor(Color color)
    {
        textColor = secondaryTextColor = color;

        return this;
    }

    public TextParameters SecondaryTextColor(Color color)
    {
        secondaryTextColor = color;

        return this;
    }

    public TextParameters BorderColor(Color color)
    {
        borderColor = color;

        return this;
    }

    public TextParameters BorderSize(int borderSize)
    {
        this.borderSize = borderSize;

        return this;
    }

    public TextParameters Position(Vector2 position)
    {
        this.position = position;

        return this;
    }

    internal TextParameters Font(FontAsset font)
    {
        this.font = font?.Guid;

        return this;
    }
}
