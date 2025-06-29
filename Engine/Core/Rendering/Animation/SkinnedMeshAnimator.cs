using Staple.Internal;
using Staple.Jobs;
using System.Numerics;

namespace Staple;

/// <summary>
/// Skinned mesh animator component
/// </summary>
public sealed class SkinnedMeshAnimator : IComponent, IComponentDisposable, ISkinnedMeshDataSource
{
    /// <summary>
    /// The mesh to use
    /// </summary>
    public Mesh mesh;

    /// <summary>
    /// The animation to use
    /// </summary>
    public string animation;

    /// <summary>
    /// Whether the animation repeats
    /// </summary>
    public bool repeat = true;

    /// <summary>
    /// The state machine to use
    /// </summary>
    public SkinnedAnimationStateMachine stateMachine;

    /// <summary>
    /// The animation controller, if any (requires state)
    /// </summary>
    public SkinnedAnimationController animationController;

    /// <summary>
    /// Whether to play in edit mode
    /// </summary>
    internal bool playInEditMode;

    /// <summary>
    /// The current play time
    /// </summary>
    internal float playTime;

    /// <summary>
    /// A cache for transforms
    /// </summary>
    internal Transform[] transformCache = [];

    /// <summary>
    /// A cache for nodes
    /// </summary>
    internal MeshAsset.Node[] nodeCache = [];

    /// <summary>
    /// The animation evaluator
    /// </summary>
    internal SkinnedMeshAnimationEvaluator evaluator;
    
    /// <summary>
    /// A cache of bone matrices
    /// </summary>
    internal Matrix4x4[] cachedBoneMatrices;

    /// <summary>
    /// The bone matrix compute buffer for skinning
    /// </summary>
    internal VertexBuffer boneMatrixBuffer;

    /// <summary>
    /// The handle for the last bone update job
    /// </summary>
    internal JobHandle boneUpdateHandle;

    /// <summary>
    /// Sets the current animation
    /// </summary>
    /// <param name="name">The name of the animation</param>
    /// <param name="repeat">Whether the animation repeats</param>
    public void SetAnimation(string name, bool repeat)
    {
        this.repeat = repeat;

        animation = name;
        playTime = 0;
        evaluator = null;
    }

    public void DisposeComponent()
    {
        boneMatrixBuffer?.Destroy();

        boneMatrixBuffer = null;
    }

    public VertexBuffer GetSkinningBuffer(SkinnedMeshRenderer renderer) => boneMatrixBuffer;
}
