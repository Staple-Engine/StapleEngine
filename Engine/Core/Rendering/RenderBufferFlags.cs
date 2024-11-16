using System;

namespace Staple;

[Flags]
public enum RenderBufferFlags
{
    None = 0,
    Index32 = (1 << 0),
    AllowResize = (1 << 1),
    Compute8x1 = (1 << 2),
    Compute8x2 = (1 << 3),
    Compute8x4 = (1 << 4),
    Compute16x1 = (1 << 5),
    Compute16x2 = (1 << 6),
    Compute16x4 = (1 << 7),
    Compute32x1 = (1 << 8),
    Compute32x2 = (1 << 9),
    Compute32x4 = (1 << 10),
    ComputeTypeInt = (1 << 11),
    ComputeTypeUInt = (1 << 12),
    ComputeTypeFloat = (1 << 13),
    ComputeWrite = (1 << 14),
    ComputeRead = (1 << 15),
    ComputeReadWrite = (1 << 16),
    DrawIndirect = (1 << 17),
}
