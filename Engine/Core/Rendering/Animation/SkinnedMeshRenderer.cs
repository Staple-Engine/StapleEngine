using System.Collections.Generic;
using System.Numerics;

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
    /// The materials for each mesh
    /// </summary>
    public List<Material> materials = [];

    /// <summary>
    /// Lighting mode
    /// </summary>
    public MaterialLighting lighting = MaterialLighting.Lit;

    /// <summary>
    /// Cached animator for this renderer
    /// </summary>
    internal EntityQuery<SkinnedMeshAnimator> animator;

    /// <summary>
    /// The bone matrix compute buffer for skinning
    /// </summary>
    internal VertexBuffer boneMatrixBuffer;

    public void DisposeComponent()
    {
        boneMatrixBuffer?.Destroy();

        boneMatrixBuffer = null;
    }
}
