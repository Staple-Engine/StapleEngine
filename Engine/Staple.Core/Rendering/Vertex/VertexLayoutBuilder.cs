using Staple.Internal;

namespace Staple;

/// <summary>
/// Creates a vertex layout
/// </summary>
public abstract class VertexLayoutBuilder
{
    protected bool completed = false;
    protected MeshAssetComponent components;

    /// <summary>
    /// Gets a new instance of a vertex layout builder
    /// </summary>
    /// <returns>The new instance</returns>
    public static VertexLayoutBuilder CreateNew()
    {
        return RenderSystem.Backend.CreateVertexLayoutBuilder();
    }

    /// <summary>
    /// Adds a vertex layout element
    /// </summary>
    /// <param name="name">The attribute name</param>
    /// <param name="type">The attribute data type</param>
    /// <returns>The current instance of this vertex layout builder</returns>
    public virtual VertexLayoutBuilder Add(VertexAttribute name, VertexAttributeType type)
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

        return this;
    }

    /// <summary>
    /// Builds and returns a finalized vertex layout based on the added elements here
    /// </summary>
    /// <returns>The new vertex layout, or null</returns>
    public abstract VertexLayout Build();
}
