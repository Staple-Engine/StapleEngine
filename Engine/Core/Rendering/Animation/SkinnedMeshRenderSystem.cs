﻿using Bgfx;
using System;
using System.Collections.Generic;
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
        /// The render cache
        /// </summary>
        public RenderCache cache;

        /// <summary>
        /// The transform of the object
        /// </summary>
        public Transform transform;
    }

    private class RenderCache
    {
        public Matrix4x4[] boneMatrices;

        public VertexBuffer boneBuffer;
    }

    private readonly Dictionary<ushort, ExpandableContainer<RenderInfo>> renderers = [];

    private readonly Dictionary<int, RenderCache> renderCache = [];

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

        foreach (var (entity, transform, relatedComponent) in entities)
        {
            var renderer = relatedComponent as SkinnedMeshRenderer;

            if (renderer.isVisible == false ||
                renderer.mesh == null ||
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

            if(skip)
            {
                continue;
            }

            renderer.animator ??= new(entity, EntityQueryMode.Parent, false);
            renderer.poser ??= new(entity, EntityQueryMode.Parent, false);

            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;

            Matrix4x4[] boneMatrices;

            if (renderCache.TryGetValue(meshAsset.Guid.GuidHash, out var cache) == false)
            {
                boneMatrices = new Matrix4x4[meshAsset.BoneCount];

                cache = new()
                {
                    boneMatrices = boneMatrices,
                };

                renderCache.Add(meshAsset.Guid.GuidHash, cache);

                UpdateBoneMatrices(meshAsset, boneMatrices, meshAsset.nodes);
            }
            else
            {
                boneMatrices = cache.boneMatrices;
            }

            if ((cache.boneBuffer?.Disposed ?? true))
            {
                cache.boneBuffer = VertexBuffer.CreateDynamic(new VertexLayoutBuilder()
                    .Add(VertexAttribute.TexCoord0, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord1, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord2, 4, VertexAttributeType.Float)
                    .Add(VertexAttribute.TexCoord3, 4, VertexAttributeType.Float)
                    .Build(), RenderBufferFlags.ComputeRead, true, (uint)boneMatrices.Length);

                cache.boneBuffer.Update(boneMatrices.AsSpan(), 0, true);
            }

            container.Add(new()
            {
                renderer = renderer,
                cache = cache,
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
        SkinnedMeshAnimator lastAnimator = null;

        var lastMeshAsset = 0;
        var lastLighting = MaterialLighting.Unlit;
        var lastTopology = MeshTopology.Triangles;

        bgfx.discard((byte)bgfx.DiscardFlags.All);

        var l = content.Length;

        for (var i = 0; i < l; i++)
        {
            var item = content.Contents[i];

            var renderer = item.renderer;
            var cache = item.cache;
            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var animator = renderer.animator.Content;
            var poser = renderer.poser.Content;
            var lighting = renderer.overrideLighting ? renderer.lighting : meshAsset.lighting;

            for (var j = 0; j < renderer.mesh.submeshes.Count; j++)
            {
                var assetGuid = meshAsset.Guid.GuidHash;

                var material = renderer.materials[j];

                var useAnimator = animator != null && animator.evaluator != null;

                var usePoser = poser != null;

                var needsChange = assetGuid != lastMeshAsset ||
                    material.StateHash != (lastMaterial?.StateHash ?? 0) ||
                    lastAnimator != animator ||
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
                    lastAnimator = animator;
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

                var buffer = useAnimator ? animator.GetBoneMatrixBuffer(meshAsset.Guid.GuidHash) :
                    usePoser ? poser.boneMatrixBuffer :
                    cache.boneBuffer;

                buffer?.SetBufferActive(SkinningBufferIndex, Access.Read);

                bgfx.submit(viewID, program, 0, (byte)flags);
            }
        }

        bgfx.discard((byte)bgfx.DiscardFlags.All);
    }

    /// <summary>
    /// Updates an span of bone matrices. The span must have the same length as a mesh asset's BoneCount
    /// </summary>
    /// <param name="meshAsset">The mesh asset to get info from</param>
    /// <param name="boneMatrices">The bone matrices to update</param>
    /// <param name="nodes">The node we're updating from</param>
    public static void UpdateBoneMatrices(MeshAsset meshAsset, Span<Matrix4x4> boneMatrices, MeshAsset.Node[] nodes)
    {
        if(boneMatrices.Length != meshAsset.BoneCount)
        {
            return;
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

                    var renderNode = meshAsset.nodes[bone.nodeIndex];

                    var localIndex = Array.FindIndex(nodes, (x => x.name == renderNode.name));

                    var localNode = localIndex >= 0 && localIndex < nodes.Length ?
                        nodes[localIndex] : null;

                    boneMatrices[m.startBoneIndex + k] = localNode != null ?
                        bone.offsetMatrix * localNode.GlobalTransform : bone.offsetMatrix;
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

    /// <summary>
    /// Applies transforms to nodes. This lets you override the animation transforms.
    /// </summary>
    /// <param name="nodeCache">The node cache</param>
    /// <param name="transformCache">The transform cache</param>
    public static void ApplyTransformsToNodes(MeshAsset.Node[] nodeCache, Transform[] transformCache)
    {
        if(nodeCache == null ||
            transformCache == null ||
            nodeCache.Length != transformCache.Length)
        {
            return;
        }

        for (var i = 0; i < nodeCache.Length; i++)
        {
            var transform = transformCache[i];

            if (transform == null)
            {
                continue;
            }

            nodeCache[i].Transform = Math.TransformationMatrix(transform.LocalPosition, transform.LocalScale, transform.LocalRotation);
        }
    }
}
