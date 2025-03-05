using System;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh attachment system.
/// Automatically syncs entity transforms with the skinned mesh's
/// </summary>
public class SkinnedMeshAttachmentSystem : IRenderSystem
{
    public bool NeedsUpdate { get; set; }

    public void Startup()
    {
    }

    public void Shutdown()
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
        foreach (var (entity, transform, relatedComponent) in entities)
        {
            if (relatedComponent is not SkinnedMeshAttachment attachment ||
                attachment.mesh == null ||
                attachment.mesh.meshAsset == null ||
                (attachment.boneName?.Length ?? 0) == 0)
            {
                continue;
            }

            attachment.animator ??= new(entity, EntityQueryMode.Parent, false);

            var animator = attachment.animator.Content;

            var nodes = SkinnedMeshRenderSystem.GetNodes(attachment.mesh.meshAsset, animator);

            void Apply(bool first)
            {
                if ((nodes?.Length ?? 0) == 0 ||
                    attachment.nodeIndex < 0 ||
                    attachment.nodeIndex >= nodes.Length ||
                    (animator?.shouldRender ?? first) == false)
                {
                    return;
                }

                var node = nodes[attachment.nodeIndex];

                node.ApplyTo(transform);
            }

            if (attachment.boneName != attachment.previousBoneName)
            {
                attachment.previousBoneName = attachment.boneName;

                if((nodes?.Length ?? 0) == 0)
                {
                    continue;
                }

                attachment.nodeIndex = Array.FindIndex(nodes, x => x.name == attachment.boneName);

                Apply(true);
            }
            else
            {
                Apply(false);
            }
        }
    }

    public Type RelatedComponent()
    {
        return typeof(SkinnedMeshAttachment);
    }

    public void Submit()
    {
    }
}
