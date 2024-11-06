using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Skinned mesh poser component.
/// Automatically syncs transforms with the animated mesh.
/// </summary>
public sealed class SkinnedAnimationPoser : IComponent
{
    /// <summary>
    /// The mesh to use to animate
    /// </summary>
    public Mesh mesh;

    /// <summary>
    /// Cache of nodes
    /// </summary>
    internal MeshAsset.Node[] nodeCache = [];

    /// <summary>
    /// Cache of transforms
    /// </summary>
    internal Dictionary<int, Transform> transformCache = [];

    /// <summary>
    /// Current mesh we're dealing with, to know whether to reset
    /// </summary>
    internal Mesh currentMesh;
}
