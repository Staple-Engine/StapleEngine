using System;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh attachment system.
/// Automatically syncs entity transforms with the skinned mesh's
/// </summary>
public class SkinnedMeshAttachmentSystem : IRenderSystem
{
    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => typeof(SkinnedMeshAttachment);

    public IRenderQueue CreateRenderQueue() => new GenericRenderQueue<SkinnedMeshAttachment>();

    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void Prepare()
    {
    }

    public void Preprocess(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
        if (renderQueue is not GenericRenderQueue<SkinnedMeshAttachment> queue)
        {
            return;
        }

        var items = queue.Items;

        foreach (var entry in items)
        {
            var attachment = entry.component;

            if (attachment.mesh == null ||
                attachment.mesh.meshAsset == null ||
                (attachment.boneName?.Length ?? 0) == 0)
            {
                continue;
            }

            attachment.animator ??= new(entry.entity, EntityQueryMode.Parent, false);

            var animator = attachment.animator.Content;

            var nodes = SkinnedMeshRenderSystem.GetNodes(attachment.mesh.meshAsset, animator);

            void Apply(bool first)
            {
                if ((nodes?.Length ?? 0) == 0 ||
                    attachment.nodeIndex < 0 ||
                    attachment.nodeIndex >= nodes.Length)
                {
                    return;
                }

                var node = nodes[attachment.nodeIndex];

                node.ApplyTo(entry.transform);
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

    public void Submit()
    {
    }
}
