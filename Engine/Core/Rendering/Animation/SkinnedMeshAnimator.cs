using Staple.Internal;
using Staple.Jobs;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// Skinned mesh animator component
/// </summary>
public sealed class SkinnedMeshAnimator : IComponent, IComponentDisposable
{
    internal class RenderInfo
    {
        /// <summary>
        /// A cache of bone matrices
        /// </summary>
        internal Matrix4x4[] cachedBoneMatrices;

        /// <summary>
        /// The bone matrix compute buffer for skinning
        /// </summary>
        internal VertexBuffer boneMatrixBuffer;
    }

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
    /// Whether we should render
    /// </summary>
    internal bool shouldRender = true;

    /// <summary>
    /// A list of all the renderers in self and children
    /// </summary>
    internal EntityQuery<SkinnedMeshRenderer> renderers;

    /// <summary>
    /// THe handle for the last bone update job
    /// </summary>
    internal JobHandle boneUpdateHandle;

    /// <summary>
    /// Rendering info per mesh asset
    /// </summary>
    internal readonly Dictionary<int, RenderInfo> renderInfos = [];

    /// <summary>
    /// Gets the bone matrix buffer for a specific mesh asset
    /// </summary>
    /// <param name="meshAssetGuid">The mesh asset guid</param>
    /// <returns>The vertex buffer, if any</returns>
    public VertexBuffer GetBoneMatrixBuffer(int meshAssetGuid)
    {
        return renderInfos.TryGetValue(meshAssetGuid, out var info) ? info.boneMatrixBuffer : null;
    }

    public void DisposeComponent()
    {
        foreach (var pair in renderInfos)
        {
            pair.Value.boneMatrixBuffer?.Destroy();

            pair.Value.boneMatrixBuffer = null;
        }

        renderInfos.Clear();
    }

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
}
