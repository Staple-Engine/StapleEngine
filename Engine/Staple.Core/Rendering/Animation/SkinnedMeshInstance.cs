using System.Numerics;

namespace Staple;

public class SkinnedMeshInstance : IComponent, IComponentDisposable
{
    /// <summary>
    /// The mesh asset used for this
    /// </summary>
    internal Mesh mesh;

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

    /// <summary>
    /// List of modifiers
    /// </summary>
    internal EntityQuery<Transform, ISkinModifier> modifiers;

    /// <summary>
    /// Whether we have an animator
    /// </summary>
    internal EntityQuery<SkinnedMeshAnimator> animator;

    public void DisposeComponent()
    {
        boneBuffer?.Destroy();

        boneBuffer = null;
    }
}
