using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// Skinned Mesh Renderer component
/// </summary>
public sealed class SkinnedMeshRenderer : Renderable
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
    /// Contains cached nodes for bone matrices
    /// </summary>
    internal List<MeshAsset.Node[]> cachedNodes = [];

    /// <summary>
    /// Contains cached nodes for bone matrices
    /// </summary>
    internal List<MeshAsset.Node[]> cachedAnimatorNodes = [];

    /// <summary>
    /// Cached animator for this renderer
    /// </summary>
    internal EntityQuery<SkinnedMeshAnimator> animator;

    /// <summary>
    /// Resets the animation state of this renderer
    /// </summary>
    internal void ResetAnimationState()
    {
        cachedNodes.Clear();
        cachedAnimatorNodes.Clear();
    }
}
