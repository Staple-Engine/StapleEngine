using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Mesh Renderer component.
/// Contains a mesh and its related materials.
/// </summary>
public sealed class MeshRenderer : Renderable
{
    public Mesh mesh;
    public List<Material> materials = new();
}
