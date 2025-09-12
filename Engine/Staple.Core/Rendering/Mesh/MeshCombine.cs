using System.Collections.Generic;

namespace Staple;

public sealed class MeshCombine : Renderable, IComponentDisposable
{
    public enum MeshCombineChildMode
    {
        DisableRendering,
        DestroyRenderers,
    }

    [Tooltip("What to do for children after combining. Destroying renderers only applies in play mode")]
    public MeshCombineChildMode childMode = MeshCombineChildMode.DisableRendering;

    internal readonly List<(Mesh, MaterialLighting)> meshes = [];
    internal readonly List<Material> materials = [];
    internal AABB combinedMeshBounds;

    internal EntityQuery<Transform, MeshRenderer> renderers;
    internal bool processed;

    public void DisposeComponent()
    {
        renderers?.Unregister();
    }
}
