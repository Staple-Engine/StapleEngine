using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    internal class VertexLayout
    {
        public bgfx.VertexLayout layout;
        public bgfx.VertexLayoutHandle layoutHandle;

        public bool Has(bgfx.Attrib name)
        {
            unsafe
            {
                fixed (bgfx.VertexLayout* v = &layout)
                {
                    return bgfx.vertex_layout_has(v, name);
                }
            }
        }

        public void Decode(bgfx.Attrib name, out byte offset, out bgfx.AttribType type, out bool normalized, out bool asInt)
        {
            unsafe
            {
                byte num;
                bgfx.AttribType typeUnsafe;
                bool normalizedUnsafe;
                bool asIntUnsafe;

                fixed (bgfx.VertexLayout* v = &layout)
                {
                    bgfx.vertex_layout_decode(v, name, &num, &typeUnsafe, &normalizedUnsafe, &asIntUnsafe);
                }

                offset = num;
                type = typeUnsafe;
                normalized = normalizedUnsafe;
                asInt = asIntUnsafe;
            }
        }
    }
}
