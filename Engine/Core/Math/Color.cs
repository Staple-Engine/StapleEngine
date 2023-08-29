using System;
using System.Runtime.InteropServices;

namespace Staple
{
    /// <summary>
    /// Represents a color as RGBA floats
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Color
    {
        public float r, g, b, a;

        /// <summary>
        /// Converts to a uint
        /// </summary>
        public uint UIntValue
        {
            get
            {
                return ((Color32)this).UIntValue;
            }
        }

        public static readonly Color White = new(1, 1, 1, 1);
        public static readonly Color Black = new(0, 0, 0, 1);

        public Color(float R, float G, float B, float A)
        {
            r = R;
            g = G;
            b = B;
            a = A;
        }

        public static implicit operator Color32(Color v) => new((byte)(v.r * 255.0f), (byte)(v.g * 255.0f), (byte)(v.b * 255.0f), (byte)(v.a * 255.0f));

        public static Color operator +(Color a, Color b) => new(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);

        public static Color operator -(Color a, Color b) => new(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a);

        public static Color operator *(Color a, Color b) => new(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);

        public static bool operator ==(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        public static bool operator !=(Color a, Color b) => a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a;

        public override int GetHashCode()
        {
            return r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode() ^ a.GetHashCode();
        }

        public override bool Equals(object obj)
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
    }
}
