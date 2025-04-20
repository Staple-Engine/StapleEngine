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
    /// Cached animator for this renderer
    /// </summary>
    internal EntityQuery<SkinnedMeshAnimator> animator;

    /// <summary>
    /// Cached poser for this renderer
    /// </summary>
    internal EntityQuery<SkinnedAnimationPoser> poser;
}
