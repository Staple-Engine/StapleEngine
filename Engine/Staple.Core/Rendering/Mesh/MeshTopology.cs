using System;

namespace Staple;

/// <summary>
/// The geometry types to use for mesh rendering
/// </summary>
[Flags]
public enum MeshTopology : ulong
{
    Triangles,
    TriangleStrip,
    Lines,
    LineStrip,
}
