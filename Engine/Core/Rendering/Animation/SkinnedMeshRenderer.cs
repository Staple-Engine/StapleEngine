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

    /// <summary>
    /// A query to get a data source in self or parent
    /// </summary>
    internal EntityQuery<ISkinnedMeshDataSource> dataSource;

    /// <summary>
    /// Cached bone matrices
    /// </summary>
    internal Matrix4x4[] boneMatrices;

    /// <summary>
    /// Cached bone buffer
    /// </summary>
    internal VertexBuffer boneBuffer;

    /// <summary>
    /// Cached transforms
    /// </summary>
    internal Transform[] transformCache;

    /// <summary>
    /// Cached nodes
    /// </summary>
    internal MeshAsset.Node[] nodeCache;

    /// <summary>
    /// Timer for updating transforms
    /// </summary>
    internal float transformUpdateTimer;

    public void DisposeComponent()
    {
        boneBuffer?.Destroy();

        boneBuffer = null;
    }
}
