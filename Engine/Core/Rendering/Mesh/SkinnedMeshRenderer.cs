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
    public MeshLighting lighting = MeshLighting.Lit;

    /// <summary>
    /// Contains cached bone matrices to reduce allocations
    /// </summary>
    internal List<Matrix4x4[]> cachedBoneMatrices = [];

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
    internal SkinnedMeshAnimator animator;

    /// <summary>
    /// Whether we've checked for an animator yet
    /// </summary>
    internal bool checkedAnimator = false;
}
