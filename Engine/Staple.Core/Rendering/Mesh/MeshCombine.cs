using System.Collections.Generic;

namespace Staple;

public partial class MeshCombine : VersionableComponent
{
    public enum MeshCombineChildMode
    {
        DisableRendering,
        DestroyRenderers,
    }

    [Tooltip("What to do for children after combining. Destroying renderers only applies in play mode")]
    public partial MeshCombineChildMode childMode { get; set; }

    [Tooltip("The source container with all mesh renderers")]
    public Transform sourceContainer;

    [Tooltip("The target container to store the result")]
    public Transform targetContainer;

    internal readonly List<Mesh> combinedMeshes = [];
    internal readonly List<Material> combinedMaterials = [];
    internal AABB combinedMeshBounds;

    internal bool processed;

    public MeshCombine()
    {
        childMode = MeshCombineChildMode.DisableRendering;
    }
}
