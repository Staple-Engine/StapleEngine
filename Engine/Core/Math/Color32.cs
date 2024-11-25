using MessagePack;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Represents a color as RGBA bytes
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 0)]
[MessagePackObject]
public struct Color32
{
    [Key(0)]
    public byte r;

    [Key(1)]
    public byte g;

    [Key(2)]
    public byte b;

    [Key(3)]
    public byte a;

    /// <summary>
    /// Converts to a uint
    /// </summary>
    [IgnoreMember]
    public readonly uint UIntValue => (uint)(r << 24) + (uint)(g << 16) + (uint)(b << 8) + a;

    [IgnoreMember]
    public readonly string HexValue => $"{r:X2}{g:X2}{b:X2}{a:X2}";

    public static readonly Color32 White = new(255, 255, 255, 255);
    public static readonly Color32 Black = new(0, 0, 0, 255);
    public static readonly Color32 Clear = new(0, 0, 0, 0);
    public static readonly Color32 Red = new(255, 0, 0, 255);
    public static readonly Color32 Green = new(0, 255, 0, 255);
    public static readonly Color32 Blue = new(0, 0, 255, 255);
    public static readonly Color32 LightBlue = new(173, 216, 230, 255);

    public Color32(byte R, byte G, byte B, byte A)
    {
        r = R;
        g = G;
        b = B;
        a = A;
    }

    /// <summary>
    /// Converts a HTML hex string to a Color.
    /// </summary>
    /// <param name="value">The hex string</param>
    /// <remarks>Expected format: "#RRGGBB" or "#RRGGBBAA"</remarks>
    public Color32(string value)
    {
        //Compensate for missing alpha component
        if(value.Length == 7)
        {
            value += "FF";
        }

        uint v = Convert.ToUInt32(value[1..], 16);

        r = (byte)((v & 0xFF000000) >> 24);
        g = (byte)((v & 0x00FF0000) >> 16);
        b = (byte)((v & 0x0000FF00) >> 8);
        a = (byte)(v & 0x000000FF);
    }

    public Color32(uint value)
    {
        r = (byte)(value & 0xFF000000);
        g = (byte)(value & 0x00FF0000);
        b = (byte)(value & 0x0000FF00);
        a = (byte)(value & 0x000000FF);
    }

    public static implicit operator Color(Color32 v) => new(v.r / 255.0f, v.g / 255.0f, v.b / 255.0f, v.a / 255.0f);

    public static implicit operator Vector4(Color32 v) => new(v.r / 255.0f, v.g / 255.0f, v.b / 255.0f, v.a / 255.0f);

    public static Color32 operator +(Color32 a, Color32 b) => new((byte)Math.Clamp(a.r + b.r, 0, 255),
        (byte)Math.Clamp(a.g + b.g, 0, 255),
        (byte)Math.Clamp(a.b + b.b, 0, 255),
        (byte)Math.Clamp(a.a + b.a, 0, 255));

    public static Color32 operator -(Color32 a, Color32 b) => new((byte)Math.Clamp(a.r - b.r, 0, 255),
        (byte)Math.Clamp(a.g - b.g, 0, 255),
        (byte)Math.Clamp(a.b - b.b, 0, 255),
        (byte)Math.Clamp(a.a - b.a, 0, 255));

    public static Color32 operator *(Color32 a, Color32 b) => new((byte)(Math.Clamp01(a.r / 255.0f * b.r / 255.0f) * 255),
        (byte)(Math.Clamp01(a.g / 255.0f * b.g / 255.0f) * 255),
        (byte)(Math.Clamp01(a.b / 255.0f + b.b / 255.0f) * 255),
        (byte)(Math.Clamp01(a.a / 255.0f + b.a / 255.0f) * 255));

    public static bool operator ==(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

    public static bool operator !=(Color32 a, Color32 b) => a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a;

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(r, g, b, a);
    }

    public override readonly bool Equals(object obj)
    {
        if(obj == null)
        {
            return false;
        }

        if(obj is Color32 c)
        {
            return r == c.r && g == c.g && b == c.b && a == c.a;
        }

        if(obj is Color cF)
        {
            c = cF;

            return r == c.r && g == c.g && b == c.b && a == c.a;
        }

        return false;
    }

    /// <summary>
    /// Mixes two colors
    /// </summary>
    /// <param name="a">The first color</param>
    /// <param name="b">The second color</param>
    /// <param name="t">The percentage as a float (0.0 to 1.0). 0 will return a, 1 will return b, and anything in-between is a mix</param>
    /// <returns>The mixed color</returns>
    public static Color32 Lerp(Color32 a, Color32 b, float t) => Color.Lerp(a, b, t);
}
