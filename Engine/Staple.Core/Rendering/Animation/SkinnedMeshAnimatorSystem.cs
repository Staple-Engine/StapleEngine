using System;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh animator system.
/// Animates skinned meshes.
/// </summary>
public sealed class SkinnedMeshAnimatorSystem : RenderSystemBase
{
    public SkinnedMeshAnimatorSystem() : base(false, typeof(SkinnedMeshAnimator), typeof(GenericRenderQueue<SkinnedMeshAnimator>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<SkinnedMeshAnimator>();

    #region Lifecycle
    public override void Startup()
    {
    }

    public override void Shutdown()
    {
    }

    public override void Prepare()
    {
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
    }

    public override void Submit()
    {
    }
    #endregion

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
        if (renderQueue is not GenericRenderQueue<SkinnedMeshAnimator> queue)
        {
            return;
        }

        var items = queue.Items;

        foreach (var entry in items)
        {
            var animator = entry.component;

            if (animator.mesh == null ||
                animator.mesh.meshAsset == null ||
                animator.mesh.meshAssetIndex < 0 ||
                animator.mesh.meshAssetIndex >= animator.mesh.meshAsset.Meshes.Length ||
                (animator.animation?.Length ?? 0) == 0 ||
                !animator.mesh.meshAsset.Animations.ContainsKey(animator.animation))
            {
                return;
            }

            if (animator.nodeCache.Length == 0 && animator.transformCache.Length == 0)
            {
                animator.transformCache = new Transform[animator.mesh.meshAsset.Nodes.Length];

                SkinnedMeshRenderSystem.GatherNodeTransforms(entry.transform, animator.transformCache, animator.mesh.meshAsset.Nodes);
            }

            if (Platform.IsPlaying)
            {
                if (animator.stateMachine != null && animator.animationController == null)
                {
                    animator.animationController = new(animator);
                }
            }

            if (Platform.IsPlaying || animator.playInEditMode)
            {
                if ((animator.evaluator == null ||
                    animator.evaluator.animation.name != animator.animation) &&
                    animator.animation != null)
                {
                    if(animator.mesh.meshAsset.Animations.TryGetValue(animator.animation, out var animation))
                    {
                        animator.evaluator = new(animator.mesh.meshAsset, animation,
                            animator.mesh.meshAsset.CloneNodes(),
                            animator);

                        animator.nodeCache = animator.evaluator.nodes;

                        animator.playTime = 0;
                    }
                }

                if(animator.evaluator?.Evaluate() ?? false)
                {
                    animator.modifiers ??= new(entry.entity, EntityQueryMode.SelfAndChildren, false);

                    foreach(var (t, modifier) in animator.modifiers.Contents)
                    {
                        modifier.Apply(t, true);
                    }
                }
            }
            else if (!animator.playInEditMode)
            {
                if (animator.evaluator != null)
                {
                    SkinnedMeshRenderSystem.ApplyNodeTransform(animator.nodeCache, animator.transformCache, true);

                    animator.evaluator = null;

                    animator.modifiers ??= new(entry.entity, EntityQueryMode.SelfAndChildren, false);

                    foreach (var (t, modifier) in animator.modifiers.Contents)
                    {
                        modifier.Apply(t, true);
                    }
                }
            }
        }
    }
}
