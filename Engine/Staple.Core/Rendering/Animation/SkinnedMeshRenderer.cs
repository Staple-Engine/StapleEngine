using Staple.Internal;
using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Skinned Mesh Renderer component
/// </summary>
public sealed class SkinnedMeshRenderer : Renderable, IComponentDisposable
{
    /// <summary>
    /// The mesh used for this
    /// </summary>
    public Mesh mesh;

    /// <summary>
    /// Whether to disable skinning entirely for this renderer
    /// </summary>
    public bool disableSkinning = false;

    /// <summary>
    /// Blend shape weights for this renderer
    /// </summary>
    public List<float> blendShapeWeights = [];

    /// <summary>
    /// Skinned mesh instance query
    /// </summary>
    internal EntityQuery<SkinnedMeshInstance> instance;

    /// <summary>
    /// Stores the blend shapes of the mesh, if any
    /// </summary>
    internal VertexBuffer blendShapeBuffer;

    /// <summary>
    /// Stores the vertex count of the mesh, if any
    /// </summary>
    internal int meshVertexCount;

    public void DisposeComponent()
    {
        blendShapeBuffer?.Destroy();

        blendShapeBuffer = null;
    }
}
