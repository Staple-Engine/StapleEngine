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
        public Transform transform;
    }

    /// <summary>
    /// Contains a list of all instances for a specific mesh and material
    /// </summary>
    private class InstanceData
    {
        public readonly ExpandableContainer<InstanceInfo> instanceInfos = new(false);
    }

    /// <summary>
    /// Contains a list of all instances for a specific mesh and material to render by multidraw and save drawcalls
    /// </summary>
    private class StaticInstanceData
    {
        public readonly ExpandableContainer<MultidrawEntry> entries = new(false);
        public readonly Dictionary<int, MultidrawEntry> entryStateToEntries = [];
        public Material material;
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

    private readonly Dictionary<int, PerRenderIndexData> perRenderIndexData = [];

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
    /// <param name="disableLighting">Whether lighting should be disabled</param>
    public static void RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material,
        bool disableLighting = false)
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

        if (material.ShaderProgram == null)
        {
            return;
        }

        LightSystem.Instance.ApplyLightProperties(ref renderState);

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
                var info = p.Value;

                info.triangles = 0;

                foreach (var entry in info.entries.Contents)
                {
                    entry.transforms.Clear();
                }

                info.entries.Clear();

                info.entryStateToEntries.Clear();
            }
        }

        foreach (var transform in transformVersions.ChangedComponents)
        {
            if (!transform.Entity.TryGetComponent<MeshRenderer>(out var renderer) ||
                renderer.mesh == null)
            {
                continue;
            }

            var localSize = renderer.mesh.bounds.size.Transformed(transform.LocalRotation).Abs();

            var globalSize = renderer.mesh.bounds.size.Transformed(transform.Rotation).Abs();

            var localCenter = transform.LocalPosition + renderer.mesh.bounds.center.Transformed(transform.LocalRotation) * transform.NormalizedLocalScale;

            var globalCenter = transform.Position + renderer.mesh.bounds.center.Transformed(transform.Rotation) *
                transform.NormalizedScale;

            renderer.localBounds = new(localCenter, localSize * transform.NormalizedLocalScale);

            renderer.bounds = new(globalCenter, globalSize * transform.NormalizedScale);
        }

        transformVersions.ClearChanges();
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
                renderer.materials == null)
            {
                continue;
            }

            void AddStatic(Material material, int submeshIndex, int startIndex, int indexCount, int entryStateKey)
            {
                if(!IsValidMaterial(material, renderIndex))
                {
                    return;
                }

                var key = material.Guid.GuidHash;

                if(!renderData.staticInstanceCache.TryGetValue(key, out var meshCache))
                {
                    meshCache = new();

                    renderData.staticInstanceCache.Add(key, meshCache);
                }

                meshCache.material = material;

                meshCache.triangles += renderer.mesh.SubmeshTriangleCount(submeshIndex);

                var meshKey = HashCode.Combine(entryStateKey, startIndex, indexCount);

                if (!meshCache.entryStateToEntries.TryGetValue(meshKey, out var drawEntry))
                {
                    ref var entries = ref meshCache.entries.AddDefault();

                    entries ??= new();

                    entries.entries = renderer.mesh.staticMeshEntries;
                    entries.startIndex = startIndex;
                    entries.indexCount = indexCount;

                    meshCache.entryStateToEntries.AddOrSetKey(meshKey, entries);

                    drawEntry = entries;
                }

                drawEntry.transforms.Add(entry.transform);
            }

            void Add(Material material, int submeshIndex)
            {
                if (!IsValidMaterial(material, renderIndex))
                {
                    return;
                }

                var key = HashCode.Combine(renderer.mesh.Guid.GuidHash, material.Guid.GuidHash, submeshIndex);

                if (!renderData.instanceCache.TryGetValue(key, out var meshCache))
                {
                    meshCache = new();

                    renderData.instanceCache.Add(key, meshCache);
                }

                ref var instanceInfo = ref meshCache.instanceInfos.AddDefault();

                instanceInfo.mesh = renderer.mesh;
                instanceInfo.material = material;
                instanceInfo.transform = entry.transform;
                instanceInfo.submeshIndex = submeshIndex;
            }

            if(renderer.mesh.IsStaticMesh)
            {
                var stateKey = renderer.mesh.staticMeshEntries.StateKey;

                if (renderer.mesh.submeshes.Count == 0)
                {
                    AddStatic(renderer.materials[0], 0, 0, renderer.mesh.IndexCount, stateKey);
                }
                else
                {
                    for (var i = 0; i < renderer.mesh.submeshes.Count; i++)
                    {
                        if (i >= renderer.materials.Count)
                        {
                            break;
                        }

                        var submesh = renderer.mesh.submeshes[i];

                        AddStatic(renderer.materials[i], i, submesh.startIndex, submesh.indexCount, stateKey);
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
        foreach(var (renderIndex, renderData) in perRenderIndexData)
        {
            var renderState = RenderState.Default;

            foreach (var (_, contents) in renderData.staticInstanceCache)
            {
                if(contents.triangles == 0)
                {
                    continue;
                }

                renderState.ClearStorageBuffers();

                renderState.instanceCount = 1;
                renderState.instanceOffset = 0;

                var material = contents.material;

                material.DisableShaderKeyword(Shader.SkinningKeyword);

                if (material.ShaderProgram == null)
                {
                    continue;
                }

                material.ApplyProperties(ref renderState);

                LightSystem.Instance.ApplyLightProperties(ref renderState);

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
                renderState.instanceTransforms?.Clear();

                renderState.instanceCount = 1;

                var instanceData = contents.instanceInfos.Contents[0];

                var material = instanceData.material;

                material.DisableShaderKeyword(Shader.SkinningKeyword);

                if (material.ShaderProgram == null)
                {
                    continue;
                }

                material.ApplyProperties(ref renderState);

                LightSystem.Instance.ApplyLightProperties(ref renderState);

                var program = material.ShaderProgram;

                if (program == null)
                {
                    continue;
                }

                contents.instanceInfos.Contents[0].mesh.SetActive(ref renderState, contents.instanceInfos.Contents[0].submeshIndex);

                if (contents.instanceInfos.Length > 1)
                {
                    renderState.instanceCount = contents.instanceInfos.Length;

                    renderState.instanceTransforms ??= new();

                    foreach (var instance in contents.instanceInfos.Contents)
                    {
                        renderState.instanceTransforms.Add(instance.transform);
                    }

                    RenderSystem.Submit(renderState, instanceData.mesh.SubmeshTriangleCount(contents.instanceInfos.Contents[0].submeshIndex),
                        renderState.instanceCount);
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
