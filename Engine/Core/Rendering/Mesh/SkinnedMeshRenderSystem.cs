using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

public class SkinnedMeshRenderSystem : IRenderSystem
{
    private struct RenderInfo
    {
        public SkinnedMeshRenderer renderer;
        public SkinnedMeshAnimator animator;
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
        var renderer = relatedComponent as SkinnedMeshRenderer;

        if (renderer.mesh == null ||
            renderer.mesh.meshAsset == null ||
            renderer.mesh.meshAssetIndex < 0 ||
            renderer.mesh.meshAssetIndex >= renderer.mesh.meshAsset.meshes.Count ||
            renderer.materials == null ||
            renderer.materials.Count != renderer.mesh.submeshes.Count ||
            renderer.materials.Any(x => x == null || x.IsValid == false))
        {
            return;
        }
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        var renderer = relatedComponent as SkinnedMeshRenderer;

        if (renderer.mesh == null ||
            renderer.mesh.meshAsset == null ||
            renderer.mesh.meshAssetIndex < 0 ||
            renderer.mesh.meshAssetIndex >= renderer.mesh.meshAsset.meshes.Count ||
            renderer.materials == null ||
            renderer.materials.Count != renderer.mesh.submeshes.Count ||
            renderer.materials.Any(x => x == null || x.IsValid == false))
        {
            return;
        }

        var animator = entity.GetComponentInParent<SkinnedMeshAnimator>();

        renderers.Add(new RenderInfo()
        {
            renderer = renderer,
            animator = animator,
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
            var animator = pair.animator;
            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var meshAssetMesh = meshAsset.meshes[mesh.meshAssetIndex];

            var useAnimator = animator != null && animator.evaluator != null;

            for (var i = 0; i < renderer.mesh.submeshes.Count; i++)
            {
                if (meshAssetMesh.bones[i].Count > 128)
                {
                    Log.Warning($"Skipping skinned mesh render for {meshAssetMesh.name}: " +
                        $"Bone count of {meshAssetMesh.bones[i].Count} exceeds limit of 128, try setting split large meshes in the import settings!");

                    continue;
                }

                var boneMatrices = new Matrix4x4[meshAssetMesh.bones[i].Count];

                for (var j = 0; j < boneMatrices.Length; j++)
                {
                    var bone = meshAssetMesh.bones[i][j];

                    Matrix4x4 globalTransform;

                    if (useAnimator && MeshAsset.TryGetNode(animator.evaluator.rootNode, bone.name, out var localNode))
                    {
                        globalTransform = localNode.GlobalTransform;
                    }
                    else
                    {
                        var node = mesh.meshAsset.GetNode(bone.name);

                        globalTransform = node.GlobalTransform;
                    }

                    boneMatrices[j] = bone.offsetMatrix * globalTransform;
                }

                unsafe
                {
                    var transform = pair.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)(state | renderer.mesh.PrimitiveFlag() | renderer.materials[i].shader.BlendingFlag()), 0);

                renderer.materials[i].ApplyProperties();

                renderer.materials[i].shader.SetMatrix4x4("u_boneMatrices", boneMatrices, boneMatrices.Length);

                renderer.materials[i].shader.SetFloat("u_isSkinning", 1);

                renderer.mesh.SetActive(i);

                bgfx.submit(pair.viewID, renderer.materials[i].shader.program, 0, (byte)bgfx.DiscardFlags.All);
            }
        }
    }

    public static void GatherNodes(Transform parent, Dictionary<string, MeshAsset.Node> nodeCache, MeshAsset.Node rootNode)
    {
        if (parent == null || nodeCache == null)
        {
            return;
        }

        nodeCache.Clear();

        void GatherNodes(MeshAsset.Node node)
        {
            if (node == null)
            {
                return;
            }

            nodeCache.AddOrSetKey(node.name, node);

            foreach (var child in node.children)
            {
                GatherNodes(child);
            }
        }

        GatherNodes(rootNode);
    }

    public static void GatherNodeTransforms(Transform parent, Dictionary<string, Transform> transformCache, MeshAsset.Node rootNode)
    {
        if (parent == null || transformCache == null)
        {
            return;
        }

        transformCache.Clear();

        void GatherNodes(MeshAsset.Node node)
        {
            if (node == null)
            {
                return;
            }

            var childTransform = parent.SearchChild(node.name);

            if (childTransform == null)
            {
                foreach (var child in node.children)
                {
                    GatherNodes(child);
                }

                return;
            }

            transformCache.AddOrSetKey(node.name, childTransform);

            foreach (var child in node.children)
            {
                GatherNodes(child);
            }
        }

        GatherNodes(rootNode);
    }

    public static void ApplyNodeTransform(Dictionary<string, MeshAsset.Node> nodeCache, Dictionary<string, Transform> transformCache, bool original = false)
    {
        foreach (var pair in transformCache)
        {
            if(nodeCache.TryGetValue(pair.Key, out var node) == false ||
                Matrix4x4.Decompose(original ? node.OriginalTransform : node.Transform,
                    out var scale, out var rotation, out var translation) == false)
            {
                continue;
            }

            pair.Value.LocalPosition = translation;
            pair.Value.LocalRotation = rotation;
            pair.Value.LocalScale = scale;
        }
    }

    public static void ApplyTransformsToNodes(Dictionary<string, MeshAsset.Node> nodeCache, Dictionary<string, Transform> transformCache)
    {
        foreach(var pair in nodeCache)
        {
            if(transformCache.TryGetValue(pair.Key, out var transform) == false)
            {
                continue;
            }

            pair.Value.Transform = Math.TransformationMatrix(transform.LocalPosition, transform.LocalScale, transform.LocalRotation);
        }
    }
}
