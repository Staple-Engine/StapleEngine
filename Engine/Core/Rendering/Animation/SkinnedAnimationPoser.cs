using Staple.Jobs;
using System.Numerics;

namespace Staple;

/// <summary>
/// Skinned mesh poser component.
/// Automatically syncs transforms with the animated mesh.
/// </summary>
public sealed class SkinnedAnimationPoser : IComponent, IComponentDisposable
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
    internal Transform[] transformCache = [];

    /// <summary>
    /// Current mesh we're dealing with, to know whether to reset
    /// </summary>
    internal Mesh currentMesh;

    /// <summary>
    /// A cache of bone matrices
    /// </summary>
    internal Matrix4x4[] cachedBoneMatrices;

    /// <summary>
    /// The bone matrix compute buffer for skinning
    /// </summary>
    internal VertexBuffer boneMatrixBuffer;

    /// <summary>
    /// THe handle for the last bone update job
    /// </summary>
    internal JobHandle boneUpdateHandle;

    public void DisposeComponent()
    {
        boneMatrixBuffer?.Destroy();

        boneMatrixBuffer = null;
    }
}
