using Bgfx;
using Staple.Internal;

namespace Staple;

/// <summary>
/// Creates a vertex layout
/// </summary>
public sealed class VertexLayoutBuilder
{
    private bgfx.VertexLayout layout;
    private bool completed = false;
    private MeshAssetComponent components;

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

    /// <summary>
    /// Adds a vertex layout element
    /// </summary>
    /// <param name="name">The attribute name</param>
    /// <param name="amount">The amount of elements in the attribute</param>
    /// <param name="type">The attribute data type</param>
    /// <param name="normalized">Whether the attribute is normalized</param>
    /// <param name="asInt">Whether the attribute should be converted to int</param>
    /// <returns>The current instance of this vertex layout builder</returns>
    public VertexLayoutBuilder Add(VertexAttribute name, byte amount, VertexAttributeType type, bool normalized = false, bool asInt = false)
    {
        if(completed)
        {
            return this;
        }

        switch (name)
        {
            case VertexAttribute.Normal:

                components |= MeshAssetComponent.Normal;

                break;

            case VertexAttribute.Tangent:

                components |= MeshAssetComponent.Tangent;

                break;

            case VertexAttribute.Bitangent:

                components |= MeshAssetComponent.Bitangent;

                break;

            case VertexAttribute.Color0:

                components |= MeshAssetComponent.Color1;

                break;

            case VertexAttribute.Color1:

                components |= MeshAssetComponent.Color2;

                break;

            case VertexAttribute.Color2:

                components |= MeshAssetComponent.Color3;

                break;

            case VertexAttribute.Color3:

                components |= MeshAssetComponent.Color4;

                break;

            case VertexAttribute.BoneIndices:
            case VertexAttribute.BoneWeight:

                components |= MeshAssetComponent.BoneIndicesWeights;

                break;

            case VertexAttribute.TexCoord0:

                components |= MeshAssetComponent.UV1;

                break;

            case VertexAttribute.TexCoord1:

                components |= MeshAssetComponent.UV2;

                break;

            case VertexAttribute.TexCoord2:

                components |= MeshAssetComponent.UV3;

                break;

            case VertexAttribute.TexCoord3:

                components |= MeshAssetComponent.UV4;

                break;

            case VertexAttribute.TexCoord4:

                components |= MeshAssetComponent.UV5;

                break;

            case VertexAttribute.TexCoord5:

                components |= MeshAssetComponent.UV6;

                break;

            case VertexAttribute.TexCoord6:

                components |= MeshAssetComponent.UV7;

                break;

            case VertexAttribute.TexCoord7:

                components |= MeshAssetComponent.UV8;

                break;
        }

        unsafe
        {
            fixed (bgfx.VertexLayout* v = &layout)
            {
                bgfx.vertex_layout_add(v, BGFXUtils.GetBGFXVertexAttribute(name), amount,
                    BGFXUtils.GetBGFXVertexAttributeType(type), normalized, asInt);
            }
        }

        return this;
    }

    /// <summary>
    /// Skips num bytes in the vertex stream
    /// </summary>
    /// <param name="num">The amount of bytes to skip</param>
    /// <returns>The current instance of this vertex layout builder</returns>
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

    /// <summary>
    /// Builds and returns a finalized vertex layout based on the added elements here
    /// </summary>
    /// <returns>The new vertex layout, or null</returns>
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
                completed = true;

                bgfx.vertex_layout_end(v);

                return new VertexLayout()
                {
                    layout = layout,
                    Components = components,
                };
            }
        }
    }
}
