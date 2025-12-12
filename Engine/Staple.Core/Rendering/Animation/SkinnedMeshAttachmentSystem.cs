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

    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void Prepare()
    {
    }

    public void Preprocess(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach(var entry in renderQueue)
        {
            if (entry.component is not SkinnedMeshAttachment attachment ||
                attachment.mesh == null ||
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
