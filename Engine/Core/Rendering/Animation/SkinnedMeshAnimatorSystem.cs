using Staple.Jobs;
using System;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh animator system.
/// Animates skinned meshes.
/// </summary>
public sealed class SkinnedMeshAnimatorSystem : IRenderSystem
{
    public bool NeedsUpdate { get; set; }

    public bool UsesOwnRenderProcess => false;

    #region Lifecycle
    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void ClearRenderData(ushort viewID)
    {
    }

    public Type RelatedComponent()
    {
        return typeof(SkinnedMeshAnimator);
    }

    public void Prepare()
    {
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Submit(ushort viewID)
    {
    }
    #endregion

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
        if(viewID != RenderSystem.FirstCameraViewID && (Platform.IsEditor == false || viewID != RenderSystem.EditorSceneViewID))
        {
            return;
        }

        foreach (var (entity, transform, relatedComponent) in entities)
        {
            var animator = relatedComponent as SkinnedMeshAnimator;

            if (animator.mesh == null ||
                animator.mesh.meshAsset == null ||
                animator.mesh.meshAssetIndex < 0 ||
                animator.mesh.meshAssetIndex >= animator.mesh.meshAsset.animations.Count ||
                (animator.animation?.Length ?? 0) == 0 ||
                animator.mesh.meshAsset.animations.ContainsKey(animator.animation) == false)
            {
                return;
            }

            if (animator.nodeCache.Length == 0 && animator.transformCache.Length == 0)
            {
                animator.transformCache = new Transform[animator.mesh.meshAsset.nodes.Length];

                SkinnedMeshRenderSystem.GatherNodeTransforms(transform, animator.transformCache, animator.mesh.meshAsset.nodes);
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
                    if(animator.mesh.meshAsset.animations.TryGetValue(animator.animation, out var animation))
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
                    animator.modifiers ??= new(entity, EntityQueryMode.SelfAndChildren, false);

                    foreach(var (t, modifier) in animator.modifiers.Contents)
                    {
                        modifier.Apply(t, true);
                    }
                }
            }
            else if (animator.playInEditMode == false)
            {
                if (animator.evaluator != null)
                {
                    SkinnedMeshRenderSystem.ApplyNodeTransform(animator.nodeCache, animator.transformCache, true);

                    animator.evaluator = null;

                    animator.modifiers ??= new(entity, EntityQueryMode.SelfAndChildren, false);

                    foreach (var (t, modifier) in animator.modifiers.Contents)
                    {
                        modifier.Apply(t, true);
                    }
                }
            }
        }
    }
}