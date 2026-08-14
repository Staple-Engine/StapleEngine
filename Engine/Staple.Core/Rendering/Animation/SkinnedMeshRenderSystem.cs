using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh render system
/// </summary>
public class SkinnedMeshRenderSystem : RenderSystemBase
{
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
            .Add(VertexAttribute.Position, VertexAttributeType.Float3)
            .Build();
    });

    private readonly ExpandableContainer<RenderInfo> renderers = new();

    private readonly SceneQuery<SkinnedMeshInstance, Transform> instances = new();

    private readonly ComponentVersionTracker<Transform> transformVersions = new();

    private readonly Dictionary<int, ShaderHandle[]> cachedMaterialBlendShapeShaderHandles = [];

    private VertexBuffer emptyBlendShapeBuffer;

    public SkinnedMeshRenderSystem() : base(false, typeof(SkinnedMeshRenderer), typeof(GenericRenderQueue<SkinnedMeshRenderer>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<SkinnedMeshRenderer>();

    public override void Prepare()
    {
        renderers.Clear();

        emptyBlendShapeBuffer ??= VertexBuffer.Create([Vector3.Zero], blendShapeVertexLayout.Value, RenderBufferFlags.GraphicsRead);
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
        if (renderQueue is not GenericRenderQueue<SkinnedMeshRenderer> queue)
        {
            return;
        }

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
                    }
                }
                else if((renderer.blendShapeWeights?.Count ?? 0) > 0)
                {
                    renderer.blendShapeWeights.Clear();
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
                    if(renderer.blendShapeBuffer == null ||
                        renderer.meshVertexCount != mesh.vertices.Length)
                    {
                        renderer.meshVertexCount = mesh.vertices.Length;

                        renderer.blendShapeBuffer?.Destroy();

                        var vertexCount = mesh.blendShape.channels.Length * mesh.vertices.Length;

                        var vertices = new Vector3[vertexCount * 2];

                        for(int i = 0, index = 0; i < mesh.blendShape.channels.Length; i++)
                        {
                            ref var channel = ref mesh.blendShape.channels[i];

                            var from = channel.positionOffsets.AsSpan();
                            var to = vertices.AsSpan(index, channel.positionOffsets.Length);

                            from.CopyTo(to);

                            if(channel.normalOffsets.Length > 0)
                            {
                                from = channel.normalOffsets.AsSpan();
                                to = vertices.AsSpan(index + channel.positionOffsets.Length, channel.positionOffsets.Length);

                                from.CopyTo(to);
                            }

                            index += channel.positionOffsets.Length * 2;
                        }

                        renderer.blendShapeBuffer = VertexBuffer.Create(vertices, blendShapeVertexLayout.Value, RenderBufferFlags.GraphicsRead);
                    }
                }
                else
                {
                    renderer.blendShapeBuffer?.Destroy();

                    renderer.blendShapeBuffer = null;
                }
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

                var key = HashCode.Combine(material.materialResource.shader.Guid.GuidHash, material.ShaderVariantKey);

                static bool HandlesValid(Span<ShaderHandle> handles)
                {
                    for (var i = 0; i < handles.Length; i++)
                    {
                        if (!handles[i].IsValid)
                        {
                            return false;
                        }
                    }

                    return true;
                }

                if (!cachedMaterialBlendShapeShaderHandles.TryGetValue(key, out var handles) || !HandlesValid(handles))
                {
                    handles = [
                        material.GetShaderHandle("StapleBlendShapeWeight0"),
                        material.GetShaderHandle("StapleBlendShapeWeight1"),
                        material.GetShaderHandle("StapleBlendShapeWeight2"),
                        material.GetShaderHandle("StapleBlendShapeWeight3"),
                        material.GetShaderHandle("StapleBlendShapeWeight4"),
                        material.GetShaderHandle("StapleBlendShapeWeight5"),
                        material.GetShaderHandle("StapleBlendShapeWeight6"),
                        material.GetShaderHandle("StapleBlendShapeWeight7"),
                        material.GetShaderHandle("StapleBlendShapeWeight8"),
                        material.GetShaderHandle("StapleBlendShapeWeight9"),
                        material.GetShaderHandle("StapleBlendShapeWeight10"),
                        material.GetShaderHandle("StapleBlendShapeWeight11"),
                        material.GetShaderHandle("StapleBlendShapeWeight12"),
                        material.GetShaderHandle("StapleBlendShapeWeight13"),
                        material.GetShaderHandle("StapleBlendShapeWeight14"),
                        material.GetShaderHandle("StapleBlendShapeWeight15"),
                        material.GetShaderHandle("StapleBlendShapeCount"),
                        material.GetShaderHandle("StapleBlendShapeVertexCount")
                    ];

                    cachedMaterialBlendShapeShaderHandles.AddOrSetKey(key, handles);
                }

                if ((handles?.Length ?? 0) != 18 ||
                    !HandlesValid(handles))
                {
                    continue;
                }

                ShaderHandle[] blendShapeWeights = 
                    [
                        handles[0],
                        handles[1],
                        handles[2],
                        handles[3],
                        handles[4],
                        handles[5],
                        handles[6],
                        handles[7],
                        handles[8],
                        handles[9],
                        handles[10],
                        handles[11],
                        handles[12],
                        handles[13],
                        handles[14],
                        handles[15],
                    ];

                var blendShapeCount = handles[16];
                var blendShapeVertexCount = handles[17];

                var blendCount = meshAssetMesh.blendShape?.channels.Length ?? 0;

				//Temporary
                if(blendCount > 16)
                {
                    blendCount = 16;
                }

                material.materialResource.shader.SetInt(material.ShaderVariantKey, blendShapeCount, blendCount);

                material.materialResource.shader.SetInt(material.ShaderVariantKey, blendShapeVertexCount, meshAssetMesh.vertices.Length);

                for (var k = 0; k < blendCount; k++)
                {
                    material.materialResource.shader.SetFloat(material.ShaderVariantKey, blendShapeWeights[k], renderer.blendShapeWeights[k]);
                }

                renderState.world = item.transform.Matrix;

                renderer.mesh.SetActive(ref renderState, j);

                LightSystem.Instance.ApplyLightProperties(material, RenderSystem.CurrentCamera.transform.Position, lighting);

                if(!lastDisableSkinning)
                {
                    renderState.ApplyStorageBufferIfNeeded("StapleBoneMatrices", instance.boneBuffer);
                    renderState.ApplyStorageBufferIfNeeded("StapleBlendData", renderer.blendShapeBuffer ?? emptyBlendShapeBuffer);
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

    #region Lifecycle
    public override void Startup()
    {
    }

    public override void Shutdown()
    {
        emptyBlendShapeBuffer?.Destroy();

        emptyBlendShapeBuffer = null;
    }
    #endregion
}
