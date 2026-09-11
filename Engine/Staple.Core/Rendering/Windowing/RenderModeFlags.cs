using System;

namespace Staple.Internal;

[Flags]
internal enum RenderModeFlags
{
    None = 0,
    Vsync = (1 << 1),
    TripleBuffering = (1 << 2),
    sRGB = (1 << 3),
    HDR10 = (1 << 4),
    Debug = (1 << 5),
    MSAA2x = (1 << 6),
    MSAA4x = (1 << 7),
    MSAA8x = (1 << 8),
}
