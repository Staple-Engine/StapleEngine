using Bgfx;
using System;

namespace Staple
{
    [Flags]
    public enum MeshTopology : ulong
    {
        Triangles = bgfx.StateFlags.PtTristrip,
        Lines = bgfx.StateFlags.PtLines,
        LineStrip = bgfx.StateFlags.PtLinestrip,
        Points = bgfx.StateFlags.PtPoints,
    }
}