using System;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh poser system.
/// Automatically syncs transforms into the skinned mesh
/// </summary>
public class SkinnedAnimationPoserSystem : IRenderSystem
{
    public void Destroy()
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
        foreach (var (_, transform, relatedComponent) in entities)
        {
            if (relatedComponent is not SkinnedAnimationPoser poser ||
                poser.mesh == null ||
                poser.mesh.meshAsset == null ||
                (poser.mesh.boneIndices?.Length ?? 0) == 0)
            {
                return;
            }

            if (poser.nodeCache.Count == 0 ||
                poser.transformCache.Count == 0 ||
                poser.currentMesh != poser.mesh)
            {
                SkinnedMeshRenderSystem.GatherNodes(poser.nodeCache, poser.mesh.meshAsset.rootNode);
                SkinnedMeshRenderSystem.GatherNodeTransforms(transform, poser.transformCache, poser.mesh.meshAsset.rootNode);
            }

            SkinnedMeshRenderSystem.ApplyTransformsToNodes(poser.nodeCache, poser.transformCache);

            poser.currentMesh = poser.mesh;
        }
    }

    public Type RelatedComponent()
    {
        return typeof(SkinnedAnimationPoser);
    }

    public void Submit()
    {
    }
}
