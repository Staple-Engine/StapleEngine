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

    /// <summary>
    /// A list of all the renderers in self and children
    /// </summary>
    internal EntityQuery<SkinnedMeshRenderer> renderers;

    /// <summary>
    /// In the case of separate animations, the mesh asset we're given has no mesh/bone info. So we must find out from any of the renderers related to this.
    /// </summary>
    internal int BoneCount
    {
        get
        {
            return renderers.Length > 0 ? renderers.Contents[0].mesh?.meshAsset?.BoneCount ?? 0 : 0;
        }
    }

    /// <summary>
    /// In the case of separate animations, the mesh asset we're given has no mesh/bone info. So we must find out from any of the renderers related to this.
    /// </summary>
    internal MeshAsset MeshAsset
    {
        get
        {
            return renderers.Length > 0 ? renderers.Contents[0].mesh?.meshAsset ?? null : null;
        }
    }

    public void DisposeComponent()
    {
        boneMatrixBuffer?.Destroy();

        boneMatrixBuffer = null;
    }
}
