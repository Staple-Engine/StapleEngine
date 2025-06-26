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
        foreach (var (entity, _, relatedComponent) in entities)
        {
            if (relatedComponent is SkinnedMeshAnimator animator)
            {
                animator.shouldRender = false;

                animator.renderers ??= new(entity, EntityQueryMode.Children, false);
            }
        }
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
        if(viewID != RenderSystem.FirstCameraViewID)
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
                animator.mesh.meshAsset.animations.ContainsKey(animator.animation) == false ||
                animator.renderers.Length == 0)
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

                        animator.shouldRender = true;

                        animator.playTime = 0;
                    }
                }

                animator.evaluator?.Evaluate();

                if(animator.shouldRender)
                {
                    foreach(var renderer in animator.renderers.Contents)
                    {
                        if(renderer.mesh?.meshAsset == null)
                        {
                            continue;
                        }

                        if(animator.renderInfos.TryGetValue(renderer.mesh.meshAsset.Guid.GuidHash, out var renderInfo) == false)
                        {
                            renderInfo = new();

                            animator.renderInfos.Add(renderer.mesh.meshAsset.Guid.GuidHash, renderInfo);
                        }

                        if (renderInfo.boneMatrixBuffer?.Disposed ?? true)
                        {
                            renderInfo.boneMatrixBuffer = VertexBuffer.CreateDynamic(new VertexLayoutBuilder()
                                .Add(VertexAttribute.TexCoord0, 4, VertexAttributeType.Float)
                                .Add(VertexAttribute.TexCoord1, 4, VertexAttributeType.Float)
                                .Add(VertexAttribute.TexCoord2, 4, VertexAttributeType.Float)
                                .Add(VertexAttribute.TexCoord3, 4, VertexAttributeType.Float)
                                .Build(), RenderBufferFlags.ComputeRead, true, (uint)renderer.mesh.meshAsset.BoneCount);

                            renderInfo.cachedBoneMatrices = new Matrix4x4[renderer.mesh.meshAsset.BoneCount];
                        }
                    }

                    if (animator.boneUpdateHandle.Valid && animator.boneUpdateHandle.Completed == false)
                    {
                        animator.boneUpdateHandle.Complete();
                    }

                    animator.boneUpdateHandle = JobScheduler.Schedule(new ActionJob(() =>
                    {
                        foreach(var renderer in animator.renderers.Contents)
                        {
                            if(renderer.mesh?.meshAsset?.Guid?.GuidHash == null ||
                                animator.renderInfos.TryGetValue(renderer.mesh.meshAsset.Guid.GuidHash, out var renderInfo) == false)
                            {
                                continue;
                            }

                            SkinnedMeshRenderSystem.UpdateBoneMatrices(renderer.mesh.meshAsset, renderInfo.cachedBoneMatrices, animator.nodeCache);

                            ThreadHelper.Dispatch(() =>
                            {
                                renderInfo.boneMatrixBuffer.Update(renderInfo.cachedBoneMatrices.AsSpan(), 0, true);
                            });
                        }
                    }));
                }
            }
            else if (animator.playInEditMode == false)
            {
                if (animator.evaluator != null)
                {
                    SkinnedMeshRenderSystem.ApplyNodeTransform(animator.nodeCache, animator.transformCache, true);

                    animator.shouldRender = true;

                    animator.evaluator = null;
                }
            }
        }
    }

    public void Submit(ushort viewID)
    {
    }
}