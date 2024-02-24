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
            var renderer = pair.renderer;
            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var meshAssetMesh = meshAsset.meshes[mesh.meshAssetIndex];

            var useAnimator = (renderer.animation?.Length ?? 0) > 0 && meshAsset.animations.ContainsKey(renderer.animation);

            if(useAnimator &&
                (renderer.animator == null ||
                renderer.animator.animation.name != renderer.animation))
            {
                renderer.animator = new()
                {
                    animation = meshAsset.animations[renderer.animation],
                    meshAsset = meshAsset,
                };
            }

            if(useAnimator)
            {
                renderer.animator.Evaluate();
            }

            var boneMatrices = new Matrix4x4[meshAssetMesh.bones.Count];

            for(var i = 0; i < boneMatrices.Length; i++)
            {
                var bone = meshAssetMesh.bones[i];

                var localTransform = Matrix4x4.Identity;
                var globalTransform = Matrix4x4.Identity;

                /*
                if (useAnimator && renderer.animator.currentTransforms.TryGetValue(bone.name, out localTransform))
                {
                    globalTransform = renderer.animator.GlobalTransform(bone.name);
                }
                else
                {
                */
                    var node = mesh.meshAsset.GetNode(bone.name);

                    localTransform = node.transform;
                    globalTransform = node.GlobalTransform;
                //}

                boneMatrices[i] = bone.offsetMatrix * globalTransform;

                if (renderer.nodeRenderers.TryGetValue(bone.name, out var item) &&
                    Matrix4x4.Decompose(localTransform, out var scale, out var rotation, out var translation))
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

            bgfx.set_state((ulong)(state | renderer.mesh.PrimitiveFlag() | renderer.material.shader.BlendingFlag()), 0);

            renderer.material.ApplyProperties();

            renderer.material.shader.SetMatrix4x4("u_boneMatrices", boneMatrices, boneMatrices.Length);

            renderer.mesh.SetActive();

            bgfx.submit(pair.viewID, renderer.material.shader.program, 0, (byte)bgfx.DiscardFlags.All);
        }
    }
}