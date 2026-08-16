using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh render system
/// </summary>
public class SkinnedMeshRenderSystem : RenderSystemBase
{
    public static readonly string BoneMatricesKey = "StapleBoneMatrices";
    public static readonly string BlendShapeDataKey = "StapleBlendShapeData";
    public static readonly string BlendShapeParametersKey = "StapleBlendShapeParameters";

    /// <summary>
    /// Info for rendering
    /// </summary>
    private struct RenderInfo
    {
        /// <summary>
        /// The renderer
        /// </summary>
        public SkinnedMeshRenderer renderer;

        /// <summary>
        /// The transform of the object
        /// </summary>
        public Transform transform;
    }

    private readonly Lazy<VertexLayout> blendShapeVertexLayout = new(() =>
    {
        return VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.Position, VertexAttributeType.Float4)
            .Build();
    });

    private readonly ExpandableContainer<RenderInfo> renderers = new();

    private readonly SceneQuery<SkinnedMeshInstance, Transform> instances = new();

    private readonly ComponentVersionTracker<Transform> transformVersions = new();

    private static readonly Dictionary<StringID, VertexBuffer> cachedBlendShapeBuffers = [];

    private static VertexBuffer emptyBlendShapeBuffer;

    [OnAssetsReimported]
    private static void OnAssetsReimported()
    {
        foreach(var pair in cachedBlendShapeBuffers)
        {
            pair.Value.Destroy();
        }

        cachedBlendShapeBuffers.Clear();
    }

    public SkinnedMeshRenderSystem() : base(false, typeof(SkinnedMeshRenderer), typeof(GenericRenderQueue<SkinnedMeshRenderer>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<SkinnedMeshRenderer>();

    public override void Startup()
    {
        World.AddComponentChangedCallback(typeof(SkinnedMeshRenderer),
            (world, entity, ref component) =>
            {
                if (component is not SkinnedMeshRenderer renderer)
                {
                    return;
                }

                renderer.needsUpdate = true;
            });
    }

    public override void Shutdown()
    {
        foreach(var pair in cachedBlendShapeBuffers)
        {
            pair.Value.Destroy();
        }

        cachedBlendShapeBuffers.Clear();

        emptyBlendShapeBuffer?.Destroy();

        emptyBlendShapeBuffer = null;
    }

    public override void Prepare()
    {
        renderers.Clear();
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
        if (renderQueue is not GenericRenderQueue<SkinnedMeshRenderer> queue)
        {
            return;
        }

        emptyBlendShapeBuffer ??= VertexBuffer.Create([Vector4.Zero], blendShapeVertexLayout.Value, RenderBufferFlags.GraphicsRead);

        var items = queue.Items;

        foreach (var entry in items)
        {
            var renderer = entry.component;

            if (renderer.mesh == null ||
                renderer.mesh.meshAsset == null ||
                renderer.mesh.meshAssetIndex < 0 ||
                renderer.mesh.meshAssetIndex >= renderer.mesh.meshAsset.Meshes.Length ||
                renderer.materials == null ||
                renderer.materials.Count == 0)
            {
                continue;
            }

            var skip = false;

            for (var i = 0; i < renderer.materials.Count; i++)
            {
                if (!(renderer.materials[i]?.IsValid ?? false))
                {
                    skip = true;

                    break;
                }
            }

            if (skip)
            {
                continue;
            }

            if(renderer.mesh.MeshAssetMesh is MeshAsset.MeshInfo meshInfo)
            {
                if(meshInfo.blendShape != null)
                {
                    var blendCount = meshInfo.blendShape.channels.Length;

                    if ((renderer.blendShapeWeights?.Count ?? 0) != blendCount)
                    {
                        renderer.blendShapeWeights ??= [];

                        while(renderer.blendShapeWeights.Count > blendCount)
                        {
                            renderer.blendShapeWeights.RemoveAt(renderer.blendShapeWeights.Count - 1);
                        }

                        while (renderer.blendShapeWeights.Count < blendCount)
                        {
                            renderer.blendShapeWeights.Add(meshInfo.blendShape.channels[renderer.blendShapeWeights.Count].weight);
                        }

                        Array.Resize(ref renderer.blendShapeNames, blendCount);

                        for(var i = 0; i < blendCount; i++)
                        {
                            renderer.blendShapeNames[i] = meshInfo.blendShape.channels[i].name;
                        }
                    }
                }
                else if((renderer.blendShapeWeights?.Count ?? 0) > 0)
                {
                    renderer.blendShapeWeights.Clear();

                    renderer.blendShapeNames = [];
                }
            }

            if (transformVersions.ShouldUpdateComponent(entry.entity, in entry.transform))
            {
                var localSize = Vector3.Abs(renderer.mesh.bounds.size.Transformed(entry.transform.LocalRotation));

                var globalSize = Vector3.Abs(renderer.mesh.bounds.size.Transformed(entry.transform.Rotation));

                renderer.localBounds = new(entry.transform.LocalPosition +
                    renderer.mesh.bounds.center.Transformed(entry.transform.LocalRotation) * entry.transform.LocalScale,
                    localSize * entry.transform.LocalScale);

                renderer.UpdateBounds(new(entry.transform.Position + renderer.mesh.bounds.center.Transformed(entry.transform.Rotation) * entry.transform.Scale,
                    globalSize * entry.transform.Scale));
            }
        }
    }

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
        var needsInstanceUpdate = false;

        if (renderQueue is not GenericRenderQueue<SkinnedMeshRenderer> queue)
        {
            return;
        }

        var items = queue.Items;

        foreach (var entry in items)
        {
            var renderer = entry.component;

            if (!renderer.isVisible ||
                renderer.mesh?.MeshAssetMesh == null ||
                renderer.materials == null ||
                renderer.materials.Count == 0)
            {
                continue;
            }

            var skip = false;

            for (var i = 0; i < renderer.materials.Count; i++)
            {
                if (!(renderer.materials[i]?.IsValid ?? false))
                {
                    skip = true;

                    break;
                }
            }

            if(skip)
            {
                continue;
            }

            renderer.mesh.UploadMeshData();

            if (renderer.instance == null)
            {
                var rootTransform = FindRootTransform(entry.transform, renderer.mesh.meshAsset.Nodes.FirstOrDefault());

                if (rootTransform != null)
                {
                    if(rootTransform.Entity.GetComponent<SkinnedMeshInstance>() == null)
                    {
                        var instance = rootTransform.Entity.AddComponent<SkinnedMeshInstance>();

                        instance.mesh = renderer.mesh;
                    }

                    if(rootTransform.Entity.GetComponent<CullingVolume>() == null)
                    {
                        rootTransform.Entity.AddComponent<CullingVolume>();
                    }
                }

                if(renderer.instance == null)
                {
                    needsInstanceUpdate = true;
                }

                renderer.instance ??= new(entry.entity, EntityQueryMode.Parent, false);
            }

            if(renderer.mesh?.MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                if(mesh.blendShape != null)
                {
                    if(renderer.needsUpdate)
                    {
                        var key = new StringID(renderer.mesh.Guid.Guid);

                        if (!cachedBlendShapeBuffers.TryGetValue(key, out renderer.blendShapeBuffer))
                        {
                            var vertexCount = mesh.blendShape.channels.Length * mesh.vertices.Length;

                            var vertices = new Vector4[vertexCount * 2];

                            for (int i = 0, index = 0; i < mesh.blendShape.channels.Length; i++, index += mesh.vertices.Length * 2)
                            {
                                ref var channel = ref mesh.blendShape.channels[i];

                                if (channel.positionOffsets.Length != mesh.vertices.Length)
                                {
                                    continue;
                                }

                                var copyNormals = channel.normalOffsets.Length == channel.positionOffsets.Length;

                                for (int j = 0, counter = 0; j < channel.positionOffsets.Length; j++, counter += 2)
                                {
                                    vertices[index + counter] = channel.positionOffsets[j].ToVector4();

                                    if (copyNormals)
                                    {
                                        vertices[index + counter + 1] = channel.normalOffsets[j].ToVector4();
                                    }
                                }
                            }

                            renderer.blendShapeBuffer = VertexBuffer.Create(vertices, blendShapeVertexLayout.Value, RenderBufferFlags.GraphicsRead);

                            cachedBlendShapeBuffers.Add(key, renderer.blendShapeBuffer);
                        }
                    }

                    if(renderer.blendShapeParameterBuffer == null ||
                        renderer.needsUpdate)
                    {
                        var parameters = new Vector4[mesh.blendShape.channels.Length + 1];

                        parameters[0].X = mesh.blendShape.channels.Length;
                        parameters[0].Y = mesh.vertices.Length;

                        while(renderer.blendShapeWeights.Count < mesh.blendShape.channels.Length)
                        {
                            renderer.blendShapeWeights.Add(mesh.blendShape.channels[renderer.blendShapeWeights.Count - 1].weight);
                        }

                        unsafe
                        {
                            fixed(void *ptr = &parameters[1].X)
                            {
                                var from = CollectionsMarshal.AsSpan(renderer.blendShapeWeights).Slice(0, mesh.blendShape.channels.Length);
                                var to = new Span<float>(ptr, mesh.blendShape.channels.Length);

                                from.CopyTo(to);
                            }
                        }

                        if(renderer.blendShapeParameterBuffer == null)
                        {
                            renderer.blendShapeParameterBuffer = VertexBuffer.Create(parameters, blendShapeVertexLayout.Value,
                                RenderBufferFlags.GraphicsRead);
                        }
                        else
                        {
                            renderer.blendShapeParameterBuffer.Update(parameters);
                        }
                    }
                }
                else
                {
                    renderer.blendShapeParameterBuffer?.Destroy();

                    renderer.blendShapeBuffer = null;
                    renderer.blendShapeParameterBuffer = null;
                }

                renderer.needsUpdate = false;
            }

            renderers.Add(new()
            {
                renderer = renderer,
                transform = entry.transform,
            });
        }

        if(needsInstanceUpdate)
        {
            instances.WorldChanged(World.Current);
        }

        foreach (var (entity, instance, transform) in instances.Contents)
        {
            if (instance.mesh?.MeshAssetMesh is null)
            {
                var animator = entity.GetComponent<SkinnedMeshAnimator>();

                if (animator?.mesh is not null)
                {
                    instance.mesh = animator.mesh;
                }

                var renderers = entity.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (var renderer in renderers)
                {
                    if (renderer.mesh is not null)
                    {
                        instance.mesh = renderer.mesh;

                        break;
                    }
                }

                if (instance.mesh is null)
                {
                    continue;
                }
            }

            Matrix4x4[] boneMatrices;

            if ((instance.boneMatrices?.Length ?? 0) == 0)
            {
                instance.boneMatrices = boneMatrices = new Matrix4x4[instance.mesh.meshAsset.BoneCount];

                instance.nodeCache = instance.mesh.meshAsset.Nodes;
                instance.transformCache = new Transform[instance.mesh.meshAsset.Nodes.Length];

                GatherNodeTransforms(transform, instance.transformCache, instance.nodeCache);

                UpdateBoneMatrices(instance.mesh.meshAsset, boneMatrices, instance.transformCache);
            }
            else
            {
                boneMatrices = instance.boneMatrices;
            }

            if ((instance.boneBuffer?.Disposed ?? true))
            {
                instance.boneBuffer = VertexBuffer.Create(boneMatrices.AsSpan(), VertexLayoutBuilder.CreateNew()
                    .Add(VertexAttribute.TexCoord0, VertexAttributeType.Float4)
                    .Add(VertexAttribute.TexCoord1, VertexAttributeType.Float4)
                    .Add(VertexAttribute.TexCoord2, VertexAttributeType.Float4)
                    .Add(VertexAttribute.TexCoord3, VertexAttributeType.Float4)
                    .Build(), RenderBufferFlags.GraphicsRead);
            }

            instance.transformUpdateTimer += Time.deltaTime;

            var limit = instance.mesh.meshAsset.SyncAnimationToRefreshRate ? 1.0f / Screen.RefreshRate : 1.0f / instance.mesh.meshAsset.FrameRate;

            if (instance.transformUpdateTimer >= limit)
            {
                instance.transformUpdateTimer -= limit;

                instance.modifiers ??= new(entity, EntityQueryMode.SelfAndChildren, false);

                instance.animator ??= new(entity, EntityQueryMode.Self, false);

                foreach (var (t, modifier) in instance.modifiers.Contents)
                {
                    if (instance.animator.Content?.evaluator != null)
                    {
                        continue;
                    }

                    modifier.Apply(t, false);
                }

                UpdateBoneMatrices(instance.mesh.meshAsset, instance.boneMatrices, instance.transformCache);

                instance.boneBuffer.Update(instance.boneMatrices.AsSpan());
            }
        }
    }

    public override void Submit()
    {
        Material lastMaterial = null;

        var lastMeshAsset = 0;
        var lastLighting = MaterialLighting.Unlit;
        var lastTopology = MeshTopology.Triangles;
        var lastDisableSkinning = false;

        var renderState = RenderState.Default;

        var l = renderers.Length;

        for (var i = 0; i < l; i++)
        {
            var item = renderers.Contents[i];

            var renderer = item.renderer;
            var instance = renderer.instance.Content;

            if(instance == null)
            {
                continue;
            }

            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var meshAssetMesh = mesh.MeshAssetMesh;
            var lighting = renderer.overrideLighting ? renderer.lighting : meshAsset.Lighting;

            for (var j = 0; j < renderer.mesh.submeshes.Count; j++)
            {
                if (j >= renderer.materials.Count)
                {
                    break;
                }

                var assetGuid = meshAsset.Guid.GuidHash;

                var material = renderer.materials[j];

                var needsChange = assetGuid != lastMeshAsset ||
                    material.StateHash != (lastMaterial?.StateHash ?? 0) ||
                    lastLighting != lighting ||
                    lastTopology != renderer.mesh.MeshTopology ||
                    lastDisableSkinning != renderer.disableSkinning;

                void SetupMaterial()
                {
                    if(renderer.disableSkinning)
                    {
                        material.DisableShaderKeyword(Shader.SkinningKeyword);
                    }
                    else
                    {
                        material.EnableShaderKeyword(Shader.SkinningKeyword);
                    }

                    LightSystem.Instance.ApplyMaterialLighting(material, lighting);
                }

                if (needsChange)
                {
                    lastMeshAsset = assetGuid;
                    lastMaterial = material;
                    lastLighting = lighting;
                    lastTopology = renderer.mesh.MeshTopology;
                    lastDisableSkinning = renderer.disableSkinning;

                    SetupMaterial();

                    if (material.ShaderProgram == null)
                    {
                        continue;
                    }

                    material.ApplyProperties(ref renderState);
                }

                if (material.ShaderProgram == null)
                {
                    continue;
                }

                renderState.world = item.transform.Matrix;

                renderer.mesh.SetActive(ref renderState, j);

                LightSystem.Instance.ApplyLightProperties(material, RenderSystem.CurrentCamera.transform.Position, lighting);

                if(!lastDisableSkinning)
                {
                    renderState.ApplyStorageBufferIfNeeded(BoneMatricesKey, instance.boneBuffer);
                    renderState.ApplyStorageBufferIfNeeded(BlendShapeDataKey, renderer.blendShapeBuffer ?? emptyBlendShapeBuffer);
                    renderState.ApplyStorageBufferIfNeeded(BlendShapeParametersKey, renderer.blendShapeParameterBuffer ?? emptyBlendShapeBuffer);
                }

                RenderSystem.Submit(renderState, renderer.mesh.SubmeshTriangleCount(j), 1);

                renderState.ClearStorageBuffers();
            }
        }
    }

    /// <summary>
    /// Attempts to find the root transform for a node
    /// </summary>
    /// <param name="current">The current transform</param>
    /// <param name="rootNode">The root node</param>
    /// <returns>The transform, or null</returns>
    public static Transform FindRootTransform(Transform current, MeshAsset.Node rootNode)
    {
        if(current == null || rootNode == null)
        {
            return null;
        }

        if(current.Entity.Name == rootNode.name)
        {
            return current;
        }

        //If we have a staple root, we need to go one more ahead
        if(rootNode.name == "StapleRoot")
        {
            if(current.Parent?.Parent?.Parent?.Entity.Name == rootNode.name)
            {
                return current.Parent?.Parent?.Parent;
            }
        }

        //All Skinned Meshes are in a child of a child of the root
        var expectedRoot = current.Parent?.Parent;

        if(expectedRoot == null)
        {
            return null;
        }

        foreach(var child in expectedRoot.Children)
        {
            if(child.Entity.Name == rootNode.name)
            {
                return child.Parent;
            }
        }

        return null;
    }

    /// <summary>
    /// Updates an span of bone matrices. The span must have the same length as a mesh asset's BoneCount
    /// </summary>
    /// <param name="meshAsset">The mesh asset to get info from</param>
    /// <param name="boneMatrices">The bone matrices to update</param>
    /// <param name="transforms">The transforms of the nodes</param>
    public static void UpdateBoneMatrices(MeshAsset meshAsset, Span<Matrix4x4> boneMatrices, Transform[] transforms)
    {
        if (boneMatrices.Length != meshAsset.BoneCount ||
            transforms.Length == 0)
        {
            return;
        }

        var reverseParentTransform = Matrix4x4.Identity;

        var parent = transforms[0]?.Parent;

        if (parent != null)
        {
            Matrix4x4.Invert(parent.Matrix, out reverseParentTransform);
        }

        for (var i = 0; i < meshAsset.Meshes.Length; i++)
        {
            var m = meshAsset.Meshes[i];
            var c = m.bones.Length;

            for (var j = 0; j < c; j++)
            {
                var bone = m.bones[j];

                var localTransform = bone.nodeIndex >= 0 && bone.nodeIndex < transforms.Length ?
                    transforms[bone.nodeIndex] : null;

                var transformMatrix = localTransform?.Matrix ?? Matrix4x4.Identity;

                if (localTransform != null)
                {
                    transformMatrix *= reverseParentTransform;
                }

                boneMatrices[m.startBoneIndex + j] = localTransform != null ?
                    bone.offsetMatrix * transformMatrix : bone.offsetMatrix;
            }
        }
    }

    /// <summary>
    /// Attempts to get the animation/bone nodes for a mesh asset
    /// </summary>
    /// <param name="meshAsset">The asset</param>
    /// <param name="animator">The animator animating the asset, if any</param>
    /// <returns>The nodes</returns>
    public static MeshAsset.Node[] GetNodes(MeshAsset meshAsset, SkinnedMeshAnimator animator)
    {
        return animator?.evaluator?.nodes ?? meshAsset.Nodes;
    }

    /// <summary>
    /// Gets all transforms related to animation nodes
    /// </summary>
    /// <param name="parent">The parent transform</param>
    /// <param name="transformCache">The transform cache</param>
    /// <param name="nodes">The nodes</param>
    public static void GatherNodeTransforms(Transform parent, Transform[] transformCache, MeshAsset.Node[] nodes)
    {
        if (parent == null ||
            transformCache == null ||
            nodes == null ||
            transformCache.Length != nodes.Length)
        {
            return;
        }

        for (var i = 0; i < nodes.Length; i++)
        {
            var childTransform = parent.SearchChild(nodes[i].name);

            if (childTransform == null)
            {
                transformCache[i] = null;

                continue;
            }

            transformCache[i] = childTransform;
        }
    }

    /// <summary>
    /// Applies a node's transform to a single element in a more effective way
    /// </summary>
    /// <param name="index">The node index</param>
    /// <param name="position">The new position</param>
    /// <param name="rotation">The new rotation</param>
    /// <param name="scale">The new scale</param>
    /// <param name="transformCache">The transform cache</param>
    public static void ApplyNodeTransformQuick(int index, Vector3 position, Quaternion rotation, Vector3 scale, Transform[] transformCache)
    {
        if(transformCache == null ||
            index < 0 ||
            index >= transformCache.Length ||
            transformCache[index] is not Transform transform)
        {
            return;
        }

        transform.LocalPosition = position;
        transform.LocalRotation = rotation;
        transform.LocalScale = scale;
    }

    /// <summary>
    /// Applies the transforms of a node cache into its related entity transforms
    /// </summary>
    /// <param name="nodeCache">The node cache</param>
    /// <param name="transformCache">The transform cache</param>
    /// <param name="original">Whether we want the original transforms (before animating)</param>
    public static void ApplyNodeTransform(MeshAsset.Node[] nodeCache, Transform[] transformCache, bool original = false)
    {
        if (nodeCache == null ||
            transformCache == null ||
            nodeCache.Length != transformCache.Length)
        {
            return;
        }

        for(var i = 0; i < nodeCache.Length; i++)
        {
            var transform = transformCache[i];

            if(transform == null)
            {
                continue;
            }

            var node = nodeCache[i];

            transform.LocalPosition = original ? node.OriginalPosition : node.Position;
            transform.LocalRotation = original ? node.OriginalRotation : node.Rotation;
            transform.LocalScale = original ? node.OriginalScale : node.Scale;
        }
    }
}
