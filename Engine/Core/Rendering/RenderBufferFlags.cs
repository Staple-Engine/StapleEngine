using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [Flags]
    enum RenderBufferFlags
    {
        None = bgfx.BufferFlags.None,
        Write = bgfx.BufferFlags.ComputeWrite,
        Read = bgfx.BufferFlags.ComputeRead,
        Index32 = bgfx.BufferFlags.Index32,
    }
}
