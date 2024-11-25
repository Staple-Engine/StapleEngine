using Bgfx;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Skinned mesh render system
/// </summary>
public class SkinnedMeshRenderSystem : IRenderSystem
{
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
        /// The current position of the object
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The transform of the object
        /// </summary>
        public Matrix4x4 transform;

        /// <summary>
        /// The render view ID
        /// </summary>
        public ushort viewID;
    }

    private readonly ExpandableContainer<RenderInfo> renderers = new();

    private readonly Dictionary<int, Matrix4x4[]> cachedBoneMatrices = [];

    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void Prepare()
    {
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        renderers.Clear();

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

            renderers.Add(new()
            {
                renderer = renderer,
                position = transform.Position,
                transform = transform.Matrix,
                viewID = viewId
            });

            var animator = renderer.animator.Content;
            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var meshAssetMesh = meshAsset.meshes[mesh.meshAssetIndex];

            var useAnimator = animator != null && animator.evaluator != null;

            Matrix4x4[] boneMatrices;

            if (useAnimator)
            {
                if((animator?.cachedBoneMatrices?.Length ?? 0) == 0)
                {
                    animator.cachedBoneMatrices = new Matrix4x4[meshAsset.BoneCount];
                }

                boneMatrices = animator.cachedBoneMatrices;
            }
            else if (cachedBoneMatrices.TryGetValue(meshAsset.Guid.GetHashCode(), out boneMatrices) == false)
            {
                boneMatrices = new Matrix4x4[meshAsset.BoneCount];

                cachedBoneMatrices.Add(meshAsset.Guid.GetHashCode(), boneMatrices);

                UpdateBoneMatrices(meshAsset, boneMatrices, meshAsset.nodes);
            }

            for (var i = 0; i < renderer.mesh.submeshes.Count; i++)
            {
                if (useAnimator == false || (renderer.boneMatrixBuffer?.Disposed ?? true))
                {
                    if (renderer.boneMatrixBuffer?.Disposed ?? true)
                    {
                        renderer.boneMatrixBuffer = VertexBuffer.CreateDynamic(new VertexLayoutBuilder()
                            .Add(VertexAttribute.TexCoord0, 4, VertexAttributeType.Float)
                            .Add(VertexAttribute.TexCoord1, 4, VertexAttributeType.Float)
                            .Add(VertexAttribute.TexCoord2, 4, VertexAttributeType.Float)
                            .Add(VertexAttribute.TexCoord3, 4, VertexAttributeType.Float)
                            .Build(), RenderBufferFlags.ComputeRead, true, (uint)boneMatrices.Length);

                        renderer.boneMatrixBuffer.Update(boneMatrices.AsSpan(), 0, true);
                    }
                }
            }
        }
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

        Material lastMaterial = null;
        int lastMeshAsset = 0;
        SkinnedMeshAnimator lastAnimator = null;
        MaterialLighting lastLighting = MaterialLighting.Unlit;

        bgfx.discard((byte)bgfx.DiscardFlags.All);

        foreach(var pair in renderers.Contents)
        {
            var renderer = pair.renderer;
            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var animator = renderer.animator.Content;

            for (var j = 0; j < renderer.mesh.submeshes.Count; j++)
            {
                var assetGuid = meshAsset.Guid.GetHashCode();

                var material = renderer.materials[j];

                var useAnimator = animator != null && animator.evaluator != null;

                var needsChange = assetGuid != lastMeshAsset ||
                    material.Guid != (lastMaterial?.Guid ?? "") ||
                    lastAnimator != animator ||
                    lastLighting != renderer.lighting;

                if (needsChange)
                {
                    lastMeshAsset = assetGuid;
                    lastMaterial = material;
                    lastAnimator = animator;
                    lastLighting = renderer.lighting;

                    bgfx.discard((byte)bgfx.DiscardFlags.All);

                    material.EnableShaderKeyword(Shader.SkinningKeyword);

                    var lightSystem = RenderSystem.Instance.Get<LightSystem>();

                    lightSystem?.ApplyMaterialLighting(material, pair.renderer.lighting);

                    if(material.ShaderProgram.Valid == false)
                    {
                        continue;
                    }

                    bgfx.set_state((ulong)(state |
                        renderer.mesh.PrimitiveFlag() |
                        material.shader.BlendingFlag |
                        material.CullingFlag), 0);

                    material.ApplyProperties(Material.ApplyMode.All);

                    lightSystem?.ApplyLightProperties(pair.position, pair.transform, material,
                        RenderSystem.CurrentCamera.Item2.Position, pair.renderer.lighting);
                }

                unsafe
                {
                    var transform = pair.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                renderer.mesh.SetActive(j);

                var program = material.ShaderProgram;

                var flags = bgfx.DiscardFlags.VertexStreams |
                    bgfx.DiscardFlags.IndexBuffer |
                    bgfx.DiscardFlags.Transform;

                if (useAnimator)
                {
                    animator.boneMatrixBuffer?.SetBufferActive(15, Access.Read);
                }
                else
                {
                    renderer.boneMatrixBuffer?.SetBufferActive(15, Access.Read);
                }

                bgfx.submit(pair.viewID, program, 0, (byte)flags);
            }
        }
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

            for (var j = 0; j < m.bones.Count; j++)
            {
                for (var k = 0; k < m.bones[j].Length; k++)
                {
                    var bone = m.bones[j][k];
                    var nodeIndex = bone.index;

                    var localNode = nodeIndex >= 0 && nodeIndex < nodes.Length ?
                        nodes[nodeIndex] : null;

                    boneMatrices[m.startBoneIndex + k] = localNode != null ?
                        bone.offsetMatrix * localNode.GlobalTransform : bone.offsetMatrix;
                }
            }
        }
    }

    /// <summary>
    /// Gets all transforms related to animation nodes
    /// </summary>
    /// <param name="parent">The parent transform</param>
    /// <param name="transformCache">The transform cache</param>
    /// <param name="nodes">The nodes</param>
    public static void GatherNodeTransforms(Transform parent, Dictionary<int, Transform> transformCache, MeshAsset.Node[] nodes)
    {
        if (parent == null || transformCache == null)
        {
            return;
        }

        transformCache.Clear();

        for(var i = 0; i < nodes.Length; i++)
        {
            var childTransform = parent.SearchChild(nodes[i].name);

            if(childTransform == null)
            {
                continue;
            }

            transformCache.AddOrSetKey(i, childTransform);
        }
    }

    /// <summary>
    /// Applies the transforms of a node cache into its related entity transforms
    /// </summary>
    /// <param name="nodeCache">The node cache</param>
    /// <param name="transformCache">The transform cache</param>
    /// <param name="original">Whether we want the original transforms (before animating)</param>
    public static void ApplyNodeTransform(MeshAsset.Node[] nodeCache, Dictionary<int, Transform> transformCache, bool original = false)
    {
        foreach (var pair in transformCache)
        {
            var node = nodeCache[pair.Key];

            pair.Value.LocalPosition = original ? node.OriginalPosition : node.Position;
            pair.Value.LocalRotation = original ? node.OriginalRotation : node.Rotation;
            pair.Value.LocalScale = original ? node.OriginalScale : node.Scale;
        }
    }

    /// <summary>
    /// Applies transforms to nodes. This lets you override the animation transforms.
    /// </summary>
    /// <param name="nodeCache">The node cache</param>
    /// <param name="transformCache">The transform cache</param>
    public static void ApplyTransformsToNodes(MeshAsset.Node[] nodeCache, Dictionary<int, Transform> transformCache)
    {
        for(var i = 0; i < nodeCache.Length; i++)
        {
            if (transformCache.TryGetValue(i, out var transform) == false)
            {
                continue;
            }

            nodeCache[i].Transform = Math.TransformationMatrix(transform.LocalPosition, transform.LocalScale, transform.LocalRotation);
        }
    }
}
