using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Color32
    {
        public byte r, g, b, a;

        public uint uintValue
        {
            get
            {
                return (uint)(r << 24) + (uint)(g << 16) + (uint)(b << 8) + a;
            }
        }

        public Color32(byte R, byte G, byte B, byte A)
        {
            r = R;
            g = G;
            b = B;
            a = A;
        }

        public static implicit operator Color(Color32 v) => new Color(v.r / 255.0f, v.g / 255.0f, v.b / 255.0f, v.a / 255.0f);

        public static Color32 operator +(Color32 a, Color32 b) => new Color32((byte)Math.Clamp(a.r + b.r, 0, 255),
            (byte)Math.Clamp(a.g + b.g, 0, 255),
            (byte)Math.Clamp(a.b + b.b, 0, 255),
            (byte)Math.Clamp(a.a + b.a, 0, 255));

        public static Color32 operator -(Color32 a, Color32 b) => new Color32((byte)Math.Clamp(a.r - b.r, 0, 255),
            (byte)Math.Clamp(a.g - b.g, 0, 255),
            (byte)Math.Clamp(a.b - b.b, 0, 255),
            (byte)Math.Clamp(a.a - b.a, 0, 255));

        public static Color32 operator *(Color32 a, Color32 b) => new Color32((byte)(Math.Clamp01(a.r / 255.0f * b.r / 255.0f) * 255),
            (byte)(Math.Clamp01(a.g / 255.0f * b.g / 255.0f) * 255),
            (byte)(Math.Clamp01(a.b / 255.0f + b.b / 255.0f) * 255),
            (byte)(Math.Clamp01(a.a / 255.0f + b.a / 255.0f) * 255));

        public static bool operator ==(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        public static bool operator !=(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

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
                Color32 c = (Color32)obj;

                return r == c.r && g == c.g && b == c.b && a == c.a;
            }

            return false;
        }
    }
}
