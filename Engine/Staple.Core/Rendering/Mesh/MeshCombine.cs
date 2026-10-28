using System.Collections.Generic;

namespace Staple;

public partial class MeshCombine : Renderable, IComponentDisposable
{
    public enum MeshCombineChildMode
    {
        DisableRendering,
        DestroyRenderers,
    }

    [Tooltip("What to do for children after combining. Destroying renderers only applies in play mode")]
    public partial MeshCombineChildMode childMode { get; set; }

    internal readonly List<Mesh> combinedMeshes = [];
    internal readonly List<Material> combinedMaterials = [];
    internal AABB combinedMeshBounds;

    internal EntityQuery<MeshRenderer> renderers;
    internal bool processed;

    public MeshCombine()
    {
        childMode = MeshCombineChildMode.DisableRendering;
    }

    public void DisposeComponent()
    {
        renderers?.Unregister();
    }
}
