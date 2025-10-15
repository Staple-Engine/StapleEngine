using System;

namespace Staple;

[Flags]
public enum RenderBufferFlags
{
    None = 0,
    GraphicsRead = (1 << 0),
    ComputeRead = (1 << 1),
    ComputeWrite = (1 << 2),
}
