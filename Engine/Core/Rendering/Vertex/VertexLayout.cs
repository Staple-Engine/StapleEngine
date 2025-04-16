using Bgfx;
using Staple.Internal;

namespace Staple;

/// <summary>
/// Manages a vertex layout
/// </summary>
public sealed class VertexLayout
{
    internal bgfx.VertexLayout layout;

    /// <summary>
    /// Decodes attribute data from the vertex layout
    /// </summary>
    /// <param name="name">The attribute name</param>
    /// <param name="offset">The offset in bytes</param>
    /// <param name="type">The attribute type</param>
    /// <param name="normalized">Whether it's normalized</param>
    /// <param name="asInt">Whether it's an int</param>
    public void Decode(VertexAttribute name, out byte offset, out VertexAttributeType type, out bool normalized, out bool asInt)
    {
        unsafe
        {
            byte num;
            bgfx.AttribType typeUnsafe;
            bool normalizedUnsafe;
            bool asIntUnsafe;

            fixed (bgfx.VertexLayout* v = &layout)
            {
                bgfx.vertex_layout_decode(v, BGFXUtils.GetBGFXVertexAttribute(name), &num, &typeUnsafe, &normalizedUnsafe, &asIntUnsafe);
            }

            offset = num;
            type = BGFXUtils.GetVertexAttributeType(typeUnsafe);
            normalized = normalizedUnsafe;
            asInt = asIntUnsafe;
        }
    }
}
