using System;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh animator system.
/// Animates skinned meshes.
/// </summary>
public sealed class SkinnedMeshAnimatorSystem : IRenderSystem
{
    private static void ApplyMeshRendererTransforms(SkinnedMeshAnimator animator)
    {
        var nodes = SkinnedMeshRenderSystem.GetNodes(animator.mesh.meshAsset, animator);

        foreach (var (entity, _) in animator.meshRenderers.ContentEntities)
        {
            var transform = entity.GetComponent<Transform>()?.parent;
            var nodeName = transform?.entity.Name;

            if (nodeName == null)
            {
                continue;
            }

            MeshAsset.Node node = null;

            for (var i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].name == nodeName)
                {
                    node = nodes[i];

                    break;
                }
            }

            if (node == null)
            {
                continue;
            }

            node.ApplyTo(transform);
        }
    }

    public void Startup()
    {
    }

    public void Shutdown()
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
        foreach (var (entity, _, relatedComponent) in entities)
        {
            if (relatedComponent is SkinnedMeshAnimator animator)
            {
                animator.shouldRender = false;

                animator.meshRenderers ??= new(entity, EntityQueryMode.Children, true);
            }
        }
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
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
                        animator.mesh.meshAsset.CloneNodes(),
                        animator);

                    animator.nodeCache = animator.evaluator.nodes;

                    animator.shouldRender = true;

                    animator.playTime = 0;
                }

                animator.evaluator.Evaluate();

                if(animator.shouldRender)
                {
                    if (animator.boneMatrixBuffer?.Disposed ?? true)
                    {
                        animator.boneMatrixBuffer = VertexBuffer.CreateDynamic(new VertexLayoutBuilder()
                            .Add(VertexAttribute.TexCoord0, 4, VertexAttributeType.Float)
                            .Add(VertexAttribute.TexCoord1, 4, VertexAttributeType.Float)
                            .Add(VertexAttribute.TexCoord2, 4, VertexAttributeType.Float)
                            .Add(VertexAttribute.TexCoord3, 4, VertexAttributeType.Float)
                            .Build(), RenderBufferFlags.ComputeRead, true, (uint)animator.mesh.meshAsset.BoneCount);

                        animator.cachedBoneMatrices = new Matrix4x4[animator.mesh.meshAsset.BoneCount];
                    }

                    SkinnedMeshRenderSystem.UpdateBoneMatrices(animator.mesh.meshAsset, animator.cachedBoneMatrices, animator.nodeCache);

                    animator.boneMatrixBuffer.Update(animator.cachedBoneMatrices.AsSpan(), 0, true);

                    ApplyMeshRendererTransforms(animator);
                }
            }
            else if (animator.playInEditMode == false)
            {
                if (animator.evaluator != null)
                {
                    animator.shouldRender = true;

                    animator.evaluator = null;

                    ApplyMeshRendererTransforms(animator);
                }
            }
        }
    }

    public void Submit()
    {
    }
}