using MessagePack;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Represents a color as RGBA floats
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 0)]
[MessagePackObject]
public struct Color
{
    [Key(0)]
    public float r;

    [Key(1)]
    public float g;

    [Key(2)]
    public float b;

    [Key(3)]
    public float a;

    /// <summary>
    /// Converts to a uint
    /// </summary>
    [IgnoreMember]
    public readonly uint UIntValue => ((Color32)this).UIntValue;

    [IgnoreMember]
    public readonly string HexValue => ((Color32)this).HexValue;

    public static readonly Color White = new(1, 1, 1, 1);
    public static readonly Color Black = new(0, 0, 0, 1);
    public static readonly Color Clear = new(0, 0, 0, 0);
    public static readonly Color Red = new(1, 0, 0, 1);
    public static readonly Color Green = new(0, 1, 0, 1);
    public static readonly Color Blue = new(0, 0, 1, 1);
    public static readonly Color LightBlue = new(0.678f, 0.847f, 0.902f, 1);

    public Color(float R, float G, float B, float A)
    {
        r = R;
        g = G;
        b = B;
        a = A;
    }

    public static implicit operator Color32(Color v) => new((byte)(v.r * 255.0f), (byte)(v.g * 255.0f), (byte)(v.b * 255.0f), (byte)(v.a * 255.0f));

    public static implicit operator Vector4(Color v) => new(v.r, v.g, v.b, v.a);

    public static Color operator +(Color a, Color b) => new(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);

    public static Color operator -(Color a, Color b) => new(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a);

    public static Color operator *(Color a, Color b) => new(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);

    public static Color operator *(Color a, float b) => new(a.r * b, a.g * b, a.b * b, a.a * b);

    public static Color operator /(Color a, Color b) => new(a.r / b.r, a.g / b.g, a.b / b.b, a.a / b.a);

    public static Color operator /(Color a, float b) => new(a.r / b, a.g / b, a.b / b, a.a / b);

    public static bool operator ==(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

    public static bool operator !=(Color a, Color b) => a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a;

    public override readonly int GetHashCode()
    {
        return r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode() ^ a.GetHashCode();
    }

    public override readonly bool Equals(object obj)
    {
        if(obj == null)
        {
            return false;
        }

        if(GetType().Equals(obj.GetType()))
        {
            Color c = (Color)obj;

            return r == c.r && g == c.g && b == c.b && a == c.a;
        }

        return false;
    }

    public static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp01(t);

        var vA = new Vector4(a.r, a.g, a.b, a.a);
        var vB = new Vector4(b.r, b.g, b.b, b.a);

        var i = Vector4.Lerp(vA, vB, t);

        return new Color(i.X, i.Y, i.Z, i.W);
    }
}
