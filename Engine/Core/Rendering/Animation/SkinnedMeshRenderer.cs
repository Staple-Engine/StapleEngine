using System.Collections.Generic;

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
    /// Cached animator for this renderer
    /// </summary>
    internal EntityQuery<SkinnedMeshAnimator> animator;
}
