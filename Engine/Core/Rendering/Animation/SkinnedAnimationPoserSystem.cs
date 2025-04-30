using Staple.Jobs;
using System;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh poser system.
/// Automatically syncs transforms into the skinned mesh
/// </summary>
public class SkinnedAnimationPoserSystem : IRenderSystem
{
    private float timer = 0.0f;

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

    public void Prepare()
    {
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        timer += Time.deltaTime;

        //TODO: Make screen refresh rate based
        var shouldUpdate = timer >= 1 / 60.0f;

        if (shouldUpdate == false)
        {
            return;
        }

        timer = 0;

        foreach (var (_, transform, relatedComponent) in entities)
        {
            if (relatedComponent is not SkinnedAnimationPoser poser ||
                poser.mesh == null ||
                poser.mesh.meshAsset == null ||
                poser.mesh.meshAsset.BoneCount == 0)
            {
                return;
            }

            if (poser.nodeCache.Length == 0 ||
                poser.transformCache.Length == 0 ||
                poser.currentMesh != poser.mesh)
            {
                poser.nodeCache = poser.mesh.meshAsset.CloneNodes();
                poser.transformCache = new Transform[poser.nodeCache.Length];

                SkinnedMeshRenderSystem.GatherNodeTransforms(transform, poser.transformCache, poser.mesh.meshAsset.nodes);
            }

            SkinnedMeshRenderSystem.ApplyTransformsToNodes(poser.nodeCache, poser.transformCache);

            if (poser.boneMatrixBuffer?.Disposed ?? true)
            {
                poser.boneMatrixBuffer = VertexBuffer.CreateDynamic(new VertexLayoutBuilder()
                    .Add(VertexAttribute.TexCoord0, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord1, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord2, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord3, 4, VertexAttributeType.Float)
                    .Build(), RenderBufferFlags.ComputeRead, true, (uint)poser.mesh.meshAsset.BoneCount);

                poser.cachedBoneMatrices = new Matrix4x4[poser.mesh.meshAsset.BoneCount];
            }

            if (poser.boneUpdateHandle.Valid && poser.boneUpdateHandle.Completed == false)
            {
                poser.boneUpdateHandle.Complete();
            }

            poser.boneUpdateHandle = JobScheduler.Schedule(new ActionJob(() =>
            {
                SkinnedMeshRenderSystem.UpdateBoneMatrices(poser.mesh.meshAsset, poser.cachedBoneMatrices, poser.nodeCache);

                ThreadHelper.Dispatch(() =>
                {
                    poser.boneMatrixBuffer?.Update(poser.cachedBoneMatrices.AsSpan(), 0, true);
                });
            }));

            poser.currentMesh = poser.mesh;
        }
    }

    public Type RelatedComponent()
    {
        return typeof(SkinnedAnimationPoser);
    }

    public void Submit(ushort viewID)
    {
    }
}