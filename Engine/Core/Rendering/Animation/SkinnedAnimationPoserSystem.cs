using System;

namespace Staple.Internal;

public class SkinnedAnimationPoserSystem : IRenderSystem
{
    public void Destroy()
    {
    }

    public void Prepare()
    {
    }

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        if(relatedComponent is not SkinnedAnimationPoser poser ||
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
            SkinnedMeshRenderSystem.GatherNodes(transform, poser.nodeCache, poser.mesh.meshAsset.rootNode);
            SkinnedMeshRenderSystem.GatherNodeTransforms(transform, poser.transformCache, poser.mesh.meshAsset.rootNode);
        }

        SkinnedMeshRenderSystem.ApplyTransformsToNodes(poser.nodeCache, poser.transformCache);

        poser.currentMesh = poser.mesh;
    }

    public Type RelatedComponent()
    {
        return typeof(SkinnedAnimationPoser);
    }

    public void Submit()
    {
    }
}
