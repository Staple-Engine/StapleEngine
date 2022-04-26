using GlmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public static class Math
    {
        public static float Round(float value)
        {
            return value < 0.0f ? (int)(value - 0.5f) : (int)(value + 0.5f);
        }

        public static int RoundToInt(float value)
        {
            return value < 0.0f ? (int)(value - 0.5f) : (int)(value + 0.5f);
        }
    }
}
