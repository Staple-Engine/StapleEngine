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
        None = 0,
        Write = bgfx.BufferFlags.ComputeWrite,
        Read = bgfx.BufferFlags.ComputeRead,
        Index32 = bgfx.BufferFlags.Index32,
    }
}
