using System;

namespace Staple.Internal;

public class SkinnedMeshAnimatorSystem : IRenderSystem
{
    public Type RelatedComponent()
    {
        return typeof(SkinnedMeshAnimator);
    }

    public void Prepare()
    {
    }

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
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

        if(animator.nodeCache.Count == 0 && animator.transformCache.Count == 0)
        {
            SkinnedMeshRenderSystem.GatherNodeTransforms(transform, animator.transformCache, animator.mesh.meshAsset.rootNode);
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
            if (animator.evaluator == null ||
                animator.evaluator.animation.name != animator.animation)
            {
                animator.evaluator = new(animator.mesh.meshAsset,
                    animator.mesh.meshAsset.animations[animator.animation],
                    animator.mesh.meshAsset.rootNode.Clone(),
                    animator);

                SkinnedMeshRenderSystem.GatherNodes(transform, animator.nodeCache, animator.evaluator.rootNode);

                animator.playTime = 0;
            }

            animator.evaluator.Evaluate();

            SkinnedMeshRenderSystem.ApplyNodeTransform(animator.nodeCache, animator.transformCache);
        }
        else if (animator.playInEditMode == false)
        {
            if(animator.evaluator != null)
            {
                SkinnedMeshRenderSystem.ApplyNodeTransform(animator.nodeCache, animator.transformCache, true);
            }

            animator.evaluator = null;
        }
    }

    public void Destroy()
    {
    }

    public void Submit()
    {
    }
}