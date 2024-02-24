using System;

namespace Staple;

internal class SkinnedMeshAnimatorSystem : IRenderSystem
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

        var t = transform;
        var a = animator;

        void GatherNodes(MeshAsset.Node node)
        {
            if (node == null)
            {
                return;
            }

            var childTransform = t.SearchChild(node.name);

            if (childTransform == null)
            {
                return;
            }

            a.nodeRenderers.AddOrSetKey(node.name, new()
            {
                node = node,
                transform = childTransform,
            });

            foreach (var child in node.children)
            {
                GatherNodes(child);
            }
        }

        if (animator.nodeRenderers.Count == 0)
        {
            GatherNodes(animator.mesh.meshAsset.rootNode);
        }

        if ((animator.evaluator == null ||
            animator.evaluator.animation.name != animator.animation))
        {
            animator.evaluator = new(animator.mesh.meshAsset, animator.mesh.meshAsset.animations[animator.animation]);
        }

        if(Platform.IsPlaying)
        {
            animator.evaluator.Evaluate();
        }
    }

    public void Destroy()
    {
    }

    public void Submit()
    {
    }
}