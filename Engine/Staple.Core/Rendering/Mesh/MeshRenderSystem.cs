using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Mesh render system
/// </summary>
public sealed class MeshRenderSystem : RenderSystemBase
{
    /// <summary>
    /// Contains info on a mesh render instance
    /// </summary>
    private struct InstanceInfo
    {
        public Mesh mesh;
        public int submeshIndex;
        public Material material;
        public MaterialLighting lighting;
        public Transform transform;
    }

    /// <summary>
    /// Contains a list of all instances for a specific mesh and material
    /// </summary>
    private class InstanceData
    {
        public readonly ExpandableContainer<InstanceInfo> instanceInfos = new();
    }

    /// <summary>
    /// Contains a list of all instances for a specific mesh and material to render by multidraw and save drawcalls
    /// </summary>
    private class StaticInstanceData
    {
        public readonly ExpandableContainer<InstanceInfo> instanceInfos = new();
        public readonly ExpandableContainer<MultidrawEntry> entries = new();
        public int triangles;
    }

    /// <summary>
    /// Contains data per render index
    /// </summary>
    private class PerRenderIndexData
    {
        public readonly Dictionary<int, InstanceData> instanceCache = [];
        public readonly Dictionary<int, StaticInstanceData> staticInstanceCache = [];
    }

    private readonly ComponentVersionTracker<Transform> instanceTransformTracker = new();

    private readonly ExpandableContainer<Matrix4x4> transformMatrices = new();

    private readonly Dictionary<int, PerRenderIndexData> perRenderIndexData = [];

    private VertexBuffer instanceBuffer;
    private int instanceOffset;
    private int instanceCount;

    private readonly Lazy<VertexLayout> instanceLayout = new(() => VertexLayoutBuilder.CreateNew()
        .Add(VertexAttribute.TexCoord0, VertexAttributeType.Float4)
        .Add(VertexAttribute.TexCoord1, VertexAttributeType.Float4)
        .Add(VertexAttribute.TexCoord2, VertexAttributeType.Float4)
        .Add(VertexAttribute.TexCoord3, VertexAttributeType.Float4)
        .Build());

    private readonly ComponentVersionTracker<Transform> transformVersions = new();

    public MeshRenderSystem() : base(false, typeof(MeshRenderer), typeof(GenericRenderQueue<MeshRenderer>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<MeshRenderer>();

    #region Lifecycle
    public override void Startup()
    {
    }

    public override void Shutdown()
    {
    }
    #endregion

    /// <summary>
    /// Renders a mesh
    /// </summary>
    /// <param name="mesh">The mesh</param>
    /// <param name="position">The position of the mesh</param>
    /// <param name="rotation">The rotation of the mesh</param>
    /// <param name="scale">The scale of the mesh</param>
    /// <param name="material">The material to use</param>
    /// <param name="lighting">The lighting model to use</param>
    public static void RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material,
        MaterialLighting lighting)
    {
        if(mesh == null ||
            material is not { IsValid: true })
        {
            return;
        }

        mesh.UploadMeshData();

        var matrix = Matrix4x4.TRS(position, scale, rotation);

        var renderState = RenderState.Default;

        renderState.world = matrix;

        mesh.SetActive(ref renderState);

        material.DisableShaderKeyword(Shader.SkinningKeyword);

        material.ApplyProperties(ref renderState);

        LightSystem.Instance.ApplyMaterialLighting(material, lighting);

        if (material.ShaderProgram == null)
        {
            return;
        }

        LightSystem.Instance.ApplyLightProperties(material, RenderSystem.CurrentCamera.transform.Position, lighting);

        RenderSystem.Submit(renderState, Mesh.TriangleCount(mesh.MeshTopology, mesh.IndexCount), 1);
    }

    public override void Prepare()
    {
        foreach(var pair in perRenderIndexData)
        {
            foreach (var p in pair.Value.instanceCache)
            {
                p.Value.instanceInfos.Clear();
            }

            foreach (var p in pair.Value.staticInstanceCache)
            {
                p.Value.instanceInfos.Clear();

                p.Value.triangles = 0;

                foreach (var entry in p.Value.entries.Contents)
                {
                    entry.transforms.Clear();
                }
            }
        }
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
        if (renderQueue is not GenericRenderQueue<MeshRenderer> queue)
        {
            return;
        }

        var items = queue.Items;

        foreach (var entry in items)
        {
            var renderer = entry.component;

            if (renderer.mesh == null)
            {
                continue;
            }

            renderer.mesh.UploadMeshData();

            if (!transformVersions.ShouldUpdateComponent(entry.entity, in entry.transform))
            {
                continue;
            }
            
            var localSize = Vector3.Abs(renderer.mesh.bounds.size.Transformed(entry.transform.LocalRotation));

            var globalSize = Vector3.Abs(renderer.mesh.bounds.size.Transformed(entry.transform.Rotation));

            renderer.localBounds = new(entry.transform.LocalPosition +
                renderer.mesh.bounds.center.Transformed(entry.transform.LocalRotation) * entry.transform.LocalScale,
                localSize * entry.transform.LocalScale);

            renderer.UpdateBounds(new(entry.transform.Position +
                renderer.mesh.bounds.center.Transformed(entry.transform.Rotation) * entry.transform.Scale,
                globalSize * entry.transform.Scale));
        }
    }

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
        if (renderQueue is not GenericRenderQueue<MeshRenderer> queue)
        {
            return;
        }

        if(!perRenderIndexData.TryGetValue(renderIndex, out var renderData))
        {
            renderData = new();

            perRenderIndexData.Add(renderIndex, renderData);
        }

        var items = queue.Items;

        foreach (var entry in items)
        {
            var renderer = entry.component;

            if (!renderer.isVisible ||
                renderer.mesh == null ||
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

            var lighting = (renderer.overrideLighting ? renderer.lighting : renderer.mesh.meshAsset?.Lighting) ?? renderer.lighting;

            void AddStatic(Material material, int submeshIndex)
            {
                if(!IsValidMaterial(material, renderIndex))
                {
                    return;
                }

                var key = HashCode.Combine(material.Guid.GuidHash, lighting);

                if(!renderData.staticInstanceCache.TryGetValue(key, out var meshCache))
                {
                    meshCache = new();

                    renderData.staticInstanceCache.Add(key, meshCache);
                }

                meshCache.instanceInfos.Add(new()
                {
                    mesh = renderer.mesh,
                    material = material,
                    lighting = lighting,
                    transform = entry.transform,
                    submeshIndex = submeshIndex,
                });

                meshCache.triangles += renderer.mesh.SubmeshTriangleCount(submeshIndex);

                foreach (var item in meshCache.entries.Contents)
                {
                    if(item.entries == renderer.mesh.staticMeshEntries)
                    {
                        item.transforms.Add(entry.transform);

                        return;
                    }
                }

                meshCache.entries.Add(new()
                {
                    entries = renderer.mesh.staticMeshEntries,
                });

                meshCache.entries.Contents[^1].transforms.Add(entry.transform);
            }

            void Add(Material material, int submeshIndex)
            {
                if (!IsValidMaterial(material, renderIndex))
                {
                    return;
                }

                var key = HashCode.Combine(renderer.mesh.Guid.GuidHash, material.Guid.GuidHash, lighting, submeshIndex);

                if (!renderData.instanceCache.TryGetValue(key, out var meshCache))
                {
                    meshCache = new();

                    renderData.instanceCache.Add(key, meshCache);
                }

                meshCache.instanceInfos.Add(new()
                {
                    mesh = renderer.mesh,
                    material = material,
                    lighting = lighting,
                    transform = entry.transform,
                    submeshIndex = submeshIndex,
                });
            }

            if(renderer.mesh.IsStaticMesh)
            {
                if (renderer.mesh.submeshes.Count == 0)
                {
                    AddStatic(renderer.materials[0], 0);
                }
                else
                {
                    for (var i = 0; i < renderer.mesh.submeshes.Count; i++)
                    {
                        if (i >= renderer.materials.Count)
                        {
                            break;
                        }

                        AddStatic(renderer.materials[i], i);
                    }
                }
            }
            else
            {
                if (renderer.mesh.submeshes.Count == 0)
                {
                    Add(renderer.materials[0], 0);
                }
                else
                {
                    for (var i = 0; i < renderer.mesh.submeshes.Count; i++)
                    {
                        if (i >= renderer.materials.Count)
                        {
                            break;
                        }

                        Add(renderer.materials[i], i);
                    }
                }
            }
        }
    }

    public override void Submit()
    {
        if(instanceBuffer?.Disposed ?? true)
        {
            instanceBuffer = VertexBuffer.Create(new Matrix4x4[1], instanceLayout.Value, RenderBufferFlags.GraphicsRead);
        }

        foreach(var (renderIndex, renderData) in perRenderIndexData)
        {
            var renderState = RenderState.Default;

            instanceCount = instanceOffset = 0;

            foreach (var (_, contents) in renderData.instanceCache)
            {
                if (contents.instanceInfos.Length <= 1)
                {
                    continue;
                }

                instanceCount += contents.instanceInfos.Length;
            }

            if (instanceCount > 0)
            {
                if (instanceCount > transformMatrices.Length)
                {
                    transformMatrices.Resize(instanceCount, false);
                }

                var needsUpdate = false;
                var transformContents = transformMatrices.Contents;

                foreach (var (_, contents) in renderData.instanceCache)
                {
                    if (contents.instanceInfos.Length <= 1)
                    {
                        continue;
                    }

                    for (var i = 0; i < contents.instanceInfos.Length; i++)
                    {
                        ref var item = ref contents.instanceInfos.Contents[i];

                        var transform = item.transform;

                        if (!instanceTransformTracker.ShouldUpdateComponent(transform.Entity, in transform))
                        {
                            continue;
                        }

                        needsUpdate = true;

                        var matrix = item.transform.Matrix;

                        transformContents[instanceOffset++] = matrix;
                    }
                }

                instanceOffset = 0;

                if (needsUpdate)
                {
                    instanceBuffer.Update(transformContents[..instanceCount]);
                }
            }

            foreach (var (_, contents) in renderData.staticInstanceCache)
            {
                if (contents.instanceInfos.Length == 0)
                {
                    continue;
                }

                renderState.ClearStorageBuffers();

                renderState.instanceCount = 1;
                renderState.instanceOffset = 0;

                var material = contents.instanceInfos.Contents[0].material;

                material.DisableShaderKeyword(Shader.SkinningKeyword);

                LightSystem.Instance.ApplyMaterialLighting(material, contents.instanceInfos.Contents[0].lighting);

                if (material.ShaderProgram == null)
                {
                    continue;
                }

                material.ApplyProperties(ref renderState);

                LightSystem.Instance.ApplyLightProperties(material, RenderSystem.CurrentCamera.transform.Position,
                    contents.instanceInfos.Contents[0].lighting);

                renderState.world = Matrix4x4.Identity;

                RenderSystem.SubmitStatic(renderState, contents.entries.Contents, contents.triangles);
            }

            foreach (var (_, contents) in renderData.instanceCache)
            {
                if (contents.instanceInfos.Length == 0)
                {
                    continue;
                }

                renderState.ClearStorageBuffers();

                renderState.instanceCount = 1;
                renderState.instanceOffset = 0;

                var instanceData = contents.instanceInfos.Contents[0];

                var material = instanceData.material;

                material.DisableShaderKeyword(Shader.SkinningKeyword);

                LightSystem.Instance.ApplyMaterialLighting(material, contents.instanceInfos.Contents[0].lighting);

                if (material.ShaderProgram == null)
                {
                    continue;
                }

                material.ApplyProperties(ref renderState);

                LightSystem.Instance.ApplyLightProperties(material, RenderSystem.CurrentCamera.transform.Position,
                    contents.instanceInfos.Contents[0].lighting);

                var program = material.ShaderProgram;

                if (program == null)
                {
                    continue;
                }

                contents.instanceInfos.Contents[0].mesh.SetActive(ref renderState, contents.instanceInfos.Contents[0].submeshIndex);

                if (contents.instanceInfos.Length > 1 && instanceBuffer != null)
                {
                    renderState.instanceOffset = instanceOffset;
                    renderState.instanceCount = contents.instanceInfos.Length;

                    instanceOffset += renderState.instanceCount;

                    renderState.ApplyStorageBufferIfNeeded("StapleInstancingTransforms", instanceBuffer);

                    RenderSystem.Submit(renderState, instanceData.mesh.SubmeshTriangleCount(contents.instanceInfos.Contents[0].submeshIndex),
                        contents.instanceInfos.Length);
                }
                else
                {
                    for (var i = 0; i < contents.instanceInfos.Length; i++)
                    {
                        var content = contents.instanceInfos.Contents[i];

                        renderState.world = content.transform.Matrix;

                        content.mesh.SetActive(ref renderState, content.submeshIndex);

                        RenderSystem.Submit(renderState, instanceData.mesh.SubmeshTriangleCount(content.submeshIndex), 1);
                    }
                }
            }
        }
    }
}
