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
    private const int SkinningBufferIndex = 1;

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

    private readonly ExpandableContainer<RenderInfo> renderers = new();

    private readonly SceneQuery<SkinnedMeshInstance, Transform> instances = new();

    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => typeof(SkinnedMeshRenderer);

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
                renderer.materials.Count == 0)
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

            if (transform.ChangedThisFrame || renderer.localBounds.size == Vector3.Zero)
            {
                var localSize = Vector3.Abs(renderer.mesh.bounds.size.Transformed(transform.LocalRotation));

                var globalSize = Vector3.Abs(renderer.mesh.bounds.size.Transformed(transform.Rotation));

                renderer.localBounds = new(transform.LocalPosition + renderer.mesh.bounds.center.Transformed(transform.LocalRotation) * transform.LocalScale,
                    localSize * transform.LocalScale);

                renderer.bounds = new(transform.Position + renderer.mesh.bounds.center.Transformed(transform.Rotation) * transform.Scale,
                    globalSize * transform.Scale);
            }
        }
    }

    public void Process(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform)
    {
        renderers.Clear();

        foreach (var (entity, transform, relatedComponent) in entities)
        {
            var renderer = relatedComponent as SkinnedMeshRenderer;

            if (renderer.isVisible == false ||
                renderer.mesh?.MeshAssetMesh == null ||
                renderer.materials == null ||
                renderer.materials.Count == 0)
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

            renderer.mesh.UploadMeshData();

            if (renderer.instance == null)
            {
                var rootTransform = FindRootTransform(transform, renderer.mesh.meshAsset.nodes.FirstOrDefault());

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

                renderer.instance ??= new(entity, EntityQueryMode.Parent, false);
            }

            renderers.Add(new()
            {
                renderer = renderer,
                transform = transform,
            });
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
                instance.boneBuffer = VertexBuffer.Create(boneMatrices.AsSpan(), VertexLayoutBuilder.CreateNew()
                    .Add(VertexAttribute.TexCoord0, VertexAttributeType.Float4)
                    .Add(VertexAttribute.TexCoord1, VertexAttributeType.Float4)
                    .Add(VertexAttribute.TexCoord2, VertexAttributeType.Float4)
                    .Add(VertexAttribute.TexCoord3, VertexAttributeType.Float4)
                    .Build(), RenderBufferFlags.GraphicsRead);
            }

            instance.transformUpdateTimer += Time.deltaTime;

            var limit = instance.mesh.meshAsset.syncAnimationToRefreshRate ? 1.0f / Screen.RefreshRate : 1.0f / instance.mesh.meshAsset.frameRate;

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

    public void Submit()
    {
        Material lastMaterial = null;

        var lastMeshAsset = 0;
        var lastLighting = MaterialLighting.Unlit;
        var lastTopology = MeshTopology.Triangles;

        var renderState = new RenderState();

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
            var lighting = renderer.overrideLighting ? renderer.lighting : meshAsset.lighting;

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

                    SetupMaterial();

                    if (material.ShaderProgram == null)
                    {
                        continue;
                    }

                    material.ApplyProperties(Material.ApplyMode.All, ref renderState);
                }

                SetupMaterial();

                if (material.ShaderProgram == null)
                {
                    continue;
                }

                renderState.world = item.transform.Matrix;

                renderer.mesh.SetActive(ref renderState, j);

                lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position, lighting);

                var buffer = instance.boneBuffer;

                renderState.shader = material.shader;

                renderState.shaderVariant = material.ShaderVariantKey;

                renderState.storageBuffers = [(SkinningBufferIndex, buffer)];

                RenderSystem.Submit(renderState, renderer.mesh.SubmeshTriangleCount(j), 1);
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
                return current.Parent.Parent.Parent;
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

        if (transforms[0]?.Parent != null)
        {
            var parent = transforms[0].Parent;

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
