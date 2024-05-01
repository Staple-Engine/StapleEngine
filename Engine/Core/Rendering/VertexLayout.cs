using Bgfx;

namespace Staple;

/// <summary>
/// Manages a vertex layout
/// </summary>
public class VertexLayout
{
    internal bgfx.VertexLayout layout;

    /// <summary>
    /// Whether an attribute is located in this layout
    /// </summary>
    /// <param name="name">The attribute name</param>
    /// <returns>Whether the attribute exists</returns>
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

    /// <summary>
    /// Decodes attribute data from the vertex layout
    /// </summary>
    /// <param name="name">The attribute name</param>
    /// <param name="offset">The offset in bytes</param>
    /// <param name="type">The attribute type</param>
    /// <param name="normalized">Whether it's normalized</param>
    /// <param name="asInt">Whether it's an int</param>
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
