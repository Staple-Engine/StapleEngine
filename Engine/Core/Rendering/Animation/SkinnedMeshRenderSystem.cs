using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh render system
/// </summary>
public class SkinnedMeshRenderSystem : IRenderSystem
{
    private const int SkinningBufferIndex = 15;

    /// <summary>
    /// Infor for rendering
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

    private readonly Dictionary<ushort, ExpandableContainer<RenderInfo>> renderers = [];

    private readonly SceneQuery<SkinnedMeshInstance, Transform> instances = new();

    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => typeof(SkinnedMeshRenderer);

    public void ClearRenderData(ushort viewID)
    {
        renderers.Remove(viewID);
    }

    public void Preprocess(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var (_, transform, relatedComponent) in entities)
        {
            var renderer = relatedComponent as SkinnedMeshRenderer;

            if (renderer.mesh == null ||
                renderer.mesh.meshAsset == null ||
                renderer.mesh.meshAssetIndex < 0 ||
                renderer.mesh.meshAssetIndex >= renderer.mesh.meshAsset.meshes.Count ||
                renderer.materials == null ||
                renderer.materials.Count != renderer.mesh.submeshes.Count)
            {
                continue;
            }

            var skip = false;

            for (var i = 0; i < renderer.materials.Count; i++)
            {
                if ((renderer.materials[i]?.IsValid ?? false) == false)
                {
                    skip = true;

                    break;
                }
            }

            if (skip)
            {
                continue;
            }

            if (renderer.mesh.submeshes.Count > 0 && renderer.materials.Count != renderer.mesh.submeshes.Count)
            {
                continue;
            }

            var localSize = Vector3.Abs(Vector3.Transform(renderer.mesh.bounds.size, transform.LocalRotation));

            var globalSize = Vector3.Abs(Vector3.Transform(renderer.mesh.bounds.size, transform.Rotation));

            renderer.localBounds = new(transform.LocalPosition + Vector3.Transform(renderer.mesh.bounds.center, transform.LocalRotation) * transform.LocalScale,
                localSize * transform.LocalScale);

            renderer.bounds = new(transform.Position + Vector3.Transform(renderer.mesh.bounds.center, transform.Rotation) * transform.Scale,
                globalSize * transform.Scale);
        }
    }

    public void Process(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
        if(renderers.TryGetValue(viewID, out var container) == false)
        {
            container = new();

            renderers.Add(viewID, container);
        }
        else
        {
            container.Clear();
        }

        foreach (var (entity, transform, relatedComponent) in entities)
        {
            var renderer = relatedComponent as SkinnedMeshRenderer;

            if (renderer.isVisible == false ||
                renderer.mesh?.MeshAssetMesh is not MeshAsset.MeshInfo meshAssetMesh ||
                renderer.materials == null ||
                renderer.materials.Count != renderer.mesh.submeshes.Count)
            {
                continue;
            }

            var skip = false;

            for (var i = 0; i < renderer.materials.Count; i++)
            {
                if ((renderer.materials[i]?.IsValid ?? false) == false)
                {
                    skip = true;

                    break;
                }
            }

            if(skip)
            {
                continue;
            }

            if(renderer.instance == null)
            {
                var rootTransform = FindRootTransform(transform, renderer.mesh.meshAsset.nodes.FirstOrDefault());

                if (rootTransform != null)
                {
                    if(rootTransform.entity.GetComponent<SkinnedMeshInstance>() == null)
                    {
                        var instance = rootTransform.entity.AddComponent<SkinnedMeshInstance>();

                        instance.mesh = renderer.mesh;
                    }

                    if(rootTransform.entity.GetComponent<CullingVolume>() == null)
                    {
                        rootTransform.entity.AddComponent<CullingVolume>();
                    }
                }

                renderer.instance ??= new(entity, EntityQueryMode.Parent, false);
            }

            container.Add(new()
            {
                renderer = renderer,
                transform = transform,
            });
        }
    }

    public void Submit(ushort viewID)
    {
        if (renderers.TryGetValue(viewID, out var content) == false)
        {
            return;
        }

        bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal;

        Material lastMaterial = null;

        var lastMeshAsset = 0;
        var lastLighting = MaterialLighting.Unlit;
        var lastTopology = MeshTopology.Triangles;

        bgfx.discard((byte)bgfx.DiscardFlags.All);

        foreach(var (entity, instance, transform) in instances.Contents)
        {
            if(instance.mesh?.MeshAssetMesh is null)
            {
                var animator = entity.GetComponent<SkinnedMeshAnimator>();

                if(animator?.mesh is not null)
                {
                    instance.mesh = animator.mesh;
                }

                var renderers = entity.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach(var renderer in renderers)
                {
                    if(renderer.mesh is not null)
                    {
                        instance.mesh = renderer.mesh;
                    }
                }

                if(instance.mesh is null)
                {
                    continue;
                }
            }

            Matrix4x4[] boneMatrices;

            if ((instance.boneMatrices?.Length ?? 0) == 0)
            {
                instance.boneMatrices = boneMatrices = new Matrix4x4[instance.mesh.meshAsset.BoneCount];

                instance.nodeCache = instance.mesh.meshAsset.nodes;
                instance.transformCache = new Transform[instance.mesh.meshAsset.nodes.Length];

                GatherNodeTransforms(transform, instance.transformCache, instance.nodeCache);

                UpdateBoneMatrices(instance.mesh.meshAsset, boneMatrices, instance.transformCache);
            }
            else
            {
                boneMatrices = instance.boneMatrices;
            }

            if ((instance.boneBuffer?.Disposed ?? true))
            {
                instance.boneBuffer = VertexBuffer.CreateDynamic(new VertexLayoutBuilder()
                    .Add(VertexAttribute.TexCoord0, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord1, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord2, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord3, 4, VertexAttributeType.Float)
                    .Build(), RenderBufferFlags.ComputeRead, true, (uint)boneMatrices.Length);

                instance.boneBuffer.Update(boneMatrices.AsSpan(), 0, true);
            }

            instance.transformUpdateTimer += Time.deltaTime;

            var limit = instance.mesh.meshAsset.syncAnimationToRefreshRate ? 1.0f / Screen.RefreshRate : 1.0f / instance.mesh.meshAsset.frameRate;

            if (instance.transformUpdateTimer >= limit)
            {
                instance.transformUpdateTimer -= limit;

                instance.modifiers ??= new(entity, EntityQueryMode.SelfAndChildren, false);

                instance.animator ??= new(entity, EntityQueryMode.Self, false);

                foreach(var (t, modifier) in instance.modifiers.Contents)
                {
                    if(instance.animator.Content?.evaluator != null)
                    {
                        continue;
                    }

                    modifier.Apply(t, false);
                }

                UpdateBoneMatrices(instance.mesh.meshAsset, instance.boneMatrices, instance.transformCache);

                instance.boneBuffer.Update(instance.boneMatrices.AsSpan(), 0, true);
            }
        }

        var l = content.Length;

        for (var i = 0; i < l; i++)
        {
            var item = content.Contents[i];

            var renderer = item.renderer;
            var instance = renderer.instance.Content;

            if(instance == null)
            {
                continue;
            }

            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var lighting = renderer.overrideLighting ? renderer.lighting : meshAsset.lighting;

            for (var j = 0; j < renderer.mesh.submeshes.Count; j++)
            {
                var assetGuid = meshAsset.Guid.GuidHash;

                var material = renderer.materials[j];

                var needsChange = assetGuid != lastMeshAsset ||
                    material.StateHash != (lastMaterial?.StateHash ?? 0) ||
                    lastLighting != lighting ||
                    lastTopology != renderer.mesh.MeshTopology;

                var lightSystem = RenderSystem.Instance.Get<LightSystem>();

                void SetupMaterial()
                {
                    material.EnableShaderKeyword(Shader.SkinningKeyword);

                    material.DisableShaderKeyword(Shader.InstancingKeyword);

                    lightSystem?.ApplyMaterialLighting(material, lighting);
                }

                if (needsChange)
                {
                    lastMeshAsset = assetGuid;
                    lastMaterial = material;
                    lastLighting = lighting;
                    lastTopology = renderer.mesh.MeshTopology;

                    bgfx.discard((byte)bgfx.DiscardFlags.All);

                    SetupMaterial();

                    if (material.ShaderProgram.Valid == false)
                    {
                        bgfx.discard((byte)bgfx.DiscardFlags.All);

                        continue;
                    }

                    material.ApplyProperties(Material.ApplyMode.All);
                }

                SetupMaterial();

                if (material.ShaderProgram.Valid == false)
                {
                    bgfx.discard((byte)bgfx.DiscardFlags.All);

                    continue;
                }

                unsafe
                {
                    var transform = item.transform.Matrix;

                    _ = bgfx.set_transform(&transform, 1);
                }

                renderer.mesh.SetActive(j);

                lightSystem?.ApplyLightProperties(item.transform.Position, item.transform.Matrix, material,
                    RenderSystem.CurrentCamera.Item2.Position, lighting);

                var program = material.ShaderProgram;

                bgfx.set_state((ulong)(state |
                    renderer.mesh.PrimitiveFlag() |
                    material.shader.BlendingFlag |
                    material.CullingFlag), 0);

                var flags = bgfx.DiscardFlags.State;

                var buffer = instance.boneBuffer;

                buffer?.SetBufferActive(SkinningBufferIndex, Access.Read);

                bgfx.submit(viewID, program, 0, (byte)flags);
            }
        }

        bgfx.discard((byte)bgfx.DiscardFlags.All);
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

        if(current.entity.Name == rootNode.name)
        {
            return current;
        }

        //If we have a staple root, we need to go one more ahead
        if(rootNode.name == "StapleRoot")
        {
            if(current.parent?.parent?.parent?.entity.Name == rootNode.name)
            {
                return current.parent.parent.parent;
            }
        }

        //All Skinned Meshes are in a child of a child of the root
        var expectedRoot = current.parent?.parent;

        if(expectedRoot == null)
        {
            return null;
        }

        foreach(var child in expectedRoot.Children)
        {
            if(child.entity.Name == rootNode.name)
            {
                return child.parent;
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

        if (transforms[0]?.parent != null)
        {
            var parent = transforms[0].parent;

            if (parent != null)
            {
                Matrix4x4.Invert(parent.Matrix, out reverseParentTransform);
            }
        }

        for (var i = 0; i < meshAsset.meshes.Count; i++)
        {
            var m = meshAsset.meshes[i];
            var c = m.bones.Count;

            for (var j = 0; j < c; j++)
            {
                var bones = m.bones[j];
                var l = bones.Length;

                for (var k = 0; k < l; k++)
                {
                    var bone = bones[k];

                    var localTransform = bone.nodeIndex >= 0 && bone.nodeIndex < transforms.Length ?
                        transforms[bone.nodeIndex] : null;

                    var transformMatrix = localTransform?.Matrix ?? Matrix4x4.Identity;

                    if (localTransform != null)
                    {
                        transformMatrix *= reverseParentTransform;
                    }

                    boneMatrices[m.startBoneIndex + k] = localTransform != null ?
                        bone.offsetMatrix * transformMatrix : bone.offsetMatrix;
                }
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
        return animator?.evaluator?.nodes ?? meshAsset.nodes;
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
    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void Prepare()
    {
    }
    #endregion
}
