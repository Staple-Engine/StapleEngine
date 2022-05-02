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
    public struct Color
    {
        public float r, g, b, a;

        public Color(float R, float G, float B, float A)
        {
            r = R;
            g = G;
            b = B;
            a = A;
        }

        public static implicit operator Color32(Color v) => new Color32((byte)(v.r * 255.0f), (byte)(v.g * 255.0f), (byte)(v.b * 255.0f), (byte)(v.a * 255.0f));

        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);

        public static Color operator -(Color a, Color b) => new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a);

        public static Color operator *(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);

        public static bool operator ==(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        public static bool operator !=(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        public override int GetHashCode()
        {
            return r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode() ^ a.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(obj is null)
            {
                return false;
            }

            if(obj is Color c)
            {
                return r == c.r && g == c.g && b == c.b && a == c.a;
            }

            return false;
        }
    }
}
