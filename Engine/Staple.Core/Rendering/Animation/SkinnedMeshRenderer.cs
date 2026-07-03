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
    /// Whether to disable skinning entirely for this renderer
    /// </summary>
    public bool disableSkinning = false;

    /// <summary>
    /// Skinned mesh instance query
    /// </summary>
    internal EntityQuery<SkinnedMeshInstance> instance;
}
