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
}
