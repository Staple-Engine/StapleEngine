using Bgfx;
using System;

namespace Staple;

/// <summary>
/// The geometry types to use for mesh rendering
/// </summary>
[Flags]
public enum MeshTopology : ulong
{
    Triangles = 0,
    TriangleStrip = bgfx.StateFlags.PtTristrip,
    Lines = bgfx.StateFlags.PtLines,
    LineStrip = bgfx.StateFlags.PtLinestrip,
    Points = bgfx.StateFlags.PtPoints,
}
