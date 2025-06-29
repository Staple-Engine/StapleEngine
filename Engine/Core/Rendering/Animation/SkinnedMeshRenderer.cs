using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// Skinned Mesh Renderer component
/// </summary>
public sealed class SkinnedMeshRenderer : Renderable, IComponentDisposable
{
    /// <summary>
    /// The mesh used for this
    /// </summary>
    public Mesh mesh;

    /// <summary>
    /// The materials for each mesh
    /// </summary>
    public List<Material> materials = [];

    internal Matrix4x4[] boneMatrices;

    internal VertexBuffer boneBuffer;

    internal Transform[] transformCache;

    internal MeshAsset.Node[] nodeCache;

    internal float transformUpdateTimer;

    public void DisposeComponent()
    {
        boneBuffer?.Destroy();

        boneBuffer = null;
    }
}
