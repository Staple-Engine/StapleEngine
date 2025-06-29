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

    public bool NeedsUpdate { get; set; }

    public bool UsesOwnRenderProcess => false;

    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void ClearRenderData(ushort viewID)
    {
        renderers.Remove(viewID);
    }

    public void Prepare()
    {
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var (entity, transform, relatedComponent) in entities)
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

            renderer.dataSource ??= new(entity, EntityQueryMode.SelfAndParent, false);

            renderer.localBounds = new(transform.LocalPosition + renderer.mesh.bounds.center * transform.LocalScale, renderer.mesh.bounds.size * transform.LocalScale);
            renderer.bounds = new(transform.Position + renderer.mesh.bounds.center * transform.Scale, renderer.mesh.bounds.size * transform.Scale);
        }
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
        if(NeedsUpdate == false)
        {
            return;
        }

        if(renderers.TryGetValue(viewID, out var container) == false)
        {
            container = new();

            renderers.Add(viewID, container);
        }
        else
        {
            container.Clear();
        }

        foreach (var (_, transform, relatedComponent) in entities)
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

            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;

            Matrix4x4[] boneMatrices;

            if((renderer.boneMatrices?.Length ?? 0) == 0)
            {
                renderer.boneMatrices = boneMatrices = new Matrix4x4[meshAsset.BoneCount];

                renderer.nodeCache = meshAsset.nodes;
                renderer.transformCache = new Transform[meshAsset.nodes.Length];

                var rootTransform = FindRootTransform(transform, renderer.nodeCache.FirstOrDefault());

                if(rootTransform != null)
                {
                    GatherNodeTransforms(rootTransform, renderer.transformCache, renderer.nodeCache);
                }

                UpdateBoneMatrices(meshAsset, boneMatrices, renderer.transformCache);
            }
            else
            {
                boneMatrices = renderer.boneMatrices;
            }

            if ((renderer.boneBuffer?.Disposed ?? true))
            {
                renderer.boneBuffer = VertexBuffer.CreateDynamic(new VertexLayoutBuilder()
                    .Add(VertexAttribute.TexCoord0, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord1, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord2, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord3, 4, VertexAttributeType.Float)
                    .Build(), RenderBufferFlags.ComputeRead, true, (uint)boneMatrices.Length);

                renderer.boneBuffer.Update(boneMatrices.AsSpan(), 0, true);
            }

            container.Add(new()
            {
                renderer = renderer,
                transform = transform,
            });
        }
    }

    public Type RelatedComponent()
    {
        return typeof(SkinnedMeshRenderer);
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

        var l = content.Length;

        for (var i = 0; i < l; i++)
        {
            var item = content.Contents[i];

            var renderer = item.renderer;
            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var dataSource = renderer.dataSource.Content;
            var lighting = renderer.overrideLighting ? renderer.lighting : meshAsset.lighting;

            if(dataSource == null)
            {
                renderer.transformUpdateTimer += Time.deltaTime;

                if (renderer.transformUpdateTimer >= 1 / Screen.RefreshRate)
                {
                    renderer.transformUpdateTimer -= 1 / Screen.RefreshRate;

                    UpdateBoneMatrices(renderer.mesh.meshAsset, renderer.boneMatrices, renderer.transformCache);

                    renderer.boneBuffer.Update(renderer.boneMatrices.AsSpan(), 0, true);
                }
            }

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

                var buffer = dataSource != null ? dataSource.GetSkinningBuffer(renderer) ?? renderer.boneBuffer :
                    renderer.boneBuffer;

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
}
