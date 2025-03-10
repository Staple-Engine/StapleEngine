using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Mesh Renderer component.
/// Contains a mesh and its related materials.
/// </summary>
public sealed class MeshRenderer : Renderable, IComponentDisposable
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

    public void DisposeComponent()
    {
        if(mesh != null && mesh.Guid?.Guid == null)
        {
            mesh.Destroy();
        }
    }
}
