using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

internal class SkinnedMeshAnimatorSystem : IRenderSystem
{
    public static Dictionary<string, SkinnedMeshAnimator.Item> GatherNodes(Transform current, MeshAsset.Node node)
    {
        var outValue = new Dictionary<string, SkinnedMeshAnimator.Item>();

        void GatherNodes(MeshAsset.Node node)
        {
            if (node == null)
            {
                return;
            }

            var childTransform = current.SearchChild(node.name);

            if (childTransform == null)
            {
                return;
            }

            outValue.AddOrSetKey(node.name, new()
            {
                node = node,
                transform = childTransform,
            });

            foreach (var child in node.children)
            {
                GatherNodes(child);
            }
        }

        GatherNodes(node);

        return outValue;
    }

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

        if (animator.nodeRenderers.Count == 0)
        {
            animator.nodeRenderers = GatherNodes(transform, animator.mesh.meshAsset.rootNode);
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
            }

            animator.evaluator.Evaluate();
        }
        else if (animator.playInEditMode == false)
        {
            if(animator.evaluator != null)
            {
                foreach(var pair in animator.nodeRenderers)
                {
                    if(Matrix4x4.Decompose(pair.Value.node.originalTransform, out var scale, out var rotation, out var translation) == false)
                    {
                        continue;
                    }

                    pair.Value.transform.LocalPosition = translation;
                    pair.Value.transform.LocalRotation = rotation;
                    pair.Value.transform.LocalScale = scale;
                }
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