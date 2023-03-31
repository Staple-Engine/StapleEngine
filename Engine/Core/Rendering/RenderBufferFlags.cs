using Bgfx;
using System;

namespace Staple
{
    [Flags]
    enum RenderBufferFlags
    {
        None = bgfx.BufferFlags.None,
        Write = bgfx.BufferFlags.ComputeWrite,
        Read = bgfx.BufferFlags.ComputeRead,
        Index32 = bgfx.BufferFlags.Index32,
        AllowResize = bgfx.BufferFlags.AllowResize,
    }
}
