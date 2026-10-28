namespace Staple;

/// <summary>
/// Mesh Renderer component.
/// Contains a mesh and its related materials.
/// </summary>
public partial class MeshRenderer : Renderable
{
    /// <summary>
    /// The mesh used for this
    /// </summary>
    public partial Mesh mesh { get; set; }
}
