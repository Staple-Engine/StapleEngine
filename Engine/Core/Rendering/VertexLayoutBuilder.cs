using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    internal class VertexLayoutBuilder
    {
        private bgfx.VertexLayout layout;
        private bool completed = false;

        public VertexLayoutBuilder()
        {
            unsafe
            {
                fixed(bgfx.VertexLayout* v = &layout)
                {
                    bgfx.vertex_layout_begin(v, bgfx.RendererType.Noop);
                }
            }
        }

        public VertexLayoutBuilder Add(bgfx.Attrib name, byte amount, bgfx.AttribType type, bool normalized = false, bool asInt = false)
        {
            if(completed)
            {
                return this;
            }

            unsafe
            {
                fixed (bgfx.VertexLayout* v = &layout)
                {
                    bgfx.vertex_layout_add(v, name, amount, type, normalized, asInt);
                }
            }

            return this;
        }

        public VertexLayoutBuilder Skip(byte num)
        {
            if(completed)
            {
                return this;
            }

            unsafe
            {
                fixed (bgfx.VertexLayout* v = &layout)
                {
                    bgfx.vertex_layout_skip(v, num);
                }
            }

            return this;
        }

        public VertexLayout Build()
        {
            if(completed)
            {
                return new VertexLayout();
            }

            unsafe
            {
                fixed (bgfx.VertexLayout* v = &layout)
                {
                    bgfx.vertex_layout_end(v);
                    completed = true;

                    return new VertexLayout()
                    {
                        layout = layout
                    };
                }
            }
        }
    }
}
