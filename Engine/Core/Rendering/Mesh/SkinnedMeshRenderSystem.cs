using Bgfx;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

internal class SkinnedMeshRenderSystem : IRenderSystem
{
    private struct RenderInfo
    {
        public SkinnedMeshRenderer renderer;
        public Matrix4x4 transform;
        public ushort viewID;
    }

    private readonly List<RenderInfo> renderers = new();

    public void Destroy()
    {
    }

    public void Prepare()
    {
        renderers.Clear();
    }

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform)
    {
        var r = relatedComponent as SkinnedMeshRenderer;

        if (r.mesh == null ||
            r.mesh.meshAsset == null ||
            r.mesh.meshAssetIndex < 0 ||
            r.mesh.meshAssetIndex >= r.mesh.meshAsset.meshes.Count ||
            r.material == null ||
            r.material.Disposed ||
            r.material.shader == null ||
            r.material.shader.Disposed)
        {
            return;
        }

        void GatherNodes(MeshAsset.Node node)
        {
            if(node == null)
            {
                return;
            }

            var t = transform.parent?.SearchChild(node.name);

            if (t == null)
            {
                return;
            }

            r.nodeRenderers.AddOrSetKey(node.name, new SkinnedMeshRendererItem()
            {
                node = node,
                transform = t,
            });

            foreach(var child in node.children)
            {
                GatherNodes(child);
            }
        }

        GatherNodes(r.mesh.meshAsset.rootNode);
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        var r = relatedComponent as SkinnedMeshRenderer;

        if (r.mesh == null ||
            r.mesh.meshAsset == null ||
            r.mesh.meshAssetIndex < 0 ||
            r.mesh.meshAssetIndex >= r.mesh.meshAsset.meshes.Count ||
            r.material == null ||
            r.material.Disposed ||
            r.material.shader == null ||
            r.material.shader.Disposed)
        {
            return;
        }

        renderers.Add(new RenderInfo()
        {
            renderer = r,
            transform = transform.Matrix,
            viewID = viewId,
        });
    }

    public Type RelatedComponent()
    {
        return typeof(SkinnedMeshRenderer);
    }

    public void Submit()
    {
        bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal;

        foreach (var pair in renderers)
        {
            var mesh = pair.renderer.mesh;
            var meshAssetMesh = pair.renderer.mesh.meshAsset.meshes[mesh.meshAssetIndex];

            var boneMatrices = new Matrix4x4[meshAssetMesh.bones.Count];

            for(var i = 0; i < boneMatrices.Length; i++)
            {
                var bone = meshAssetMesh.bones[i];

                var node = mesh.meshAsset.GetNode(bone.name);

                var fullTransform = node.GlobalTransform;

                boneMatrices[i] = bone.offsetMatrix * fullTransform;

                if (pair.renderer.nodeRenderers.TryGetValue(bone.name, out var item) &&
                    Matrix4x4.Decompose(node.transform, out var scale, out var rotation, out var translation))
                {
                    item.transform.LocalPosition = translation;
                    item.transform.LocalRotation = rotation;
                    item.transform.LocalScale = scale;
                }
            }

            unsafe
            {
                var transform = pair.transform;

                _ = bgfx.set_transform(&transform, 1);
            }

            bgfx.set_state((ulong)(state | pair.renderer.mesh.PrimitiveFlag() | pair.renderer.material.shader.BlendingFlag()), 0);

            pair.renderer.material.ApplyProperties();

            pair.renderer.material.shader.SetMatrix4x4("u_boneMatrices", boneMatrices, boneMatrices.Length);

            pair.renderer.mesh.SetActive();

            bgfx.submit(pair.viewID, pair.renderer.material.shader.program, 0, (byte)bgfx.DiscardFlags.All);
        }
    }
}