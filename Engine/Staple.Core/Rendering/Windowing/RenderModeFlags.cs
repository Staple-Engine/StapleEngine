using System;

namespace Staple.Internal;

[Flags]
internal enum RenderModeFlags
{
    None = 0,
    Vsync = (1 << 1),
    TripleBuffering = (1 << 2),
    sRGB = (1 << 3),
    HDR10 = (1 << 6),
}
