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
    private class InstanceInfo
    {
        public Mesh mesh;
        public int submeshIndex;
        public Material material;
        public Transform transform;
        public bool visible;
    }

    /// <summary>
    /// Contains a list of all instances for a specific mesh and material
    /// </summary>
    private class InstanceData
    {
        public MeshRenderer renderer;
        public int instanceCount;
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
        public int instanceCount;
    }

    /// <summary>
    /// Contains data per render index
    /// </summary>
    private class PerRenderIndexData
    {
        public readonly Dictionary<int, InstanceData> instanceCache = [];
        public readonly Dictionary<int, StaticInstanceData> staticInstanceCache = [];

        public readonly ExpandableContainer<InstanceData> instanceList = new(false);

        public readonly ExpandableContainer<StaticInstanceData> staticInstanceList = new(false);
    }

    private class RenderData : IRenderData
    {
        public readonly Dictionary<int, PerRenderIndexData> perRenderIndexData = [];

        public void Clear()
        {
            foreach (var pair in perRenderIndexData)
            {
                foreach (var p in pair.Value.instanceCache)
                {
                    p.Value.instanceInfos.Clear();
                }

                foreach (var p in pair.Value.staticInstanceCache)
                {
                    var info = p.Value;

                    info.instanceCount = 0;

                    foreach (var entry in info.entries.Contents)
                    {
                        entry.instanceCount = 0;

                        entry.transforms.Clear();
                    }

                    info.entries.Clear();

                    info.entryStateToEntries.Clear();
                }
            }
        }

        public void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
        {
            if (renderQueue is not GenericRenderQueue<MeshRenderer> queue)
            {
                return;
            }

            if (!perRenderIndexData.TryGetValue(renderIndex, out var renderData))
            {
                renderData = new();

                perRenderIndexData.Add(renderIndex, renderData);
            }

            var items = queue.Items;

            foreach (var entry in items)
            {
                var renderer = entry.component;

                if (renderer.mesh == null ||
                    renderer.materials == null)
                {
                    continue;
                }

                void AddStatic(Material material, int submeshIndex, int startIndex, int indexCount, int entryStateKey)
                {
                    if (!IsValidMaterial(material, renderIndex))
                    {
                        return;
                    }

                    var key = material.Guid.GuidHash;

                    if (!renderData.staticInstanceCache.TryGetValue(key, out var meshCache))
                    {
                        meshCache = new();

                        renderData.staticInstanceCache.Add(key, meshCache);

                        renderData.staticInstanceList.Add(meshCache);
                    }

                    meshCache.material = material;

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

                    drawEntry.transforms.Add(new(entry.transform, renderer.isVisible, renderer));

                    if(renderer.isVisible)
                    {
                        drawEntry.instanceCount++;
                    }
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
                        meshCache = new()
                        {
                            renderer = renderer,
                        };

                        renderData.instanceCache.Add(key, meshCache);
                        renderData.instanceList.Add(meshCache);
                    }

                    ref var instanceInfo = ref meshCache.instanceInfos.AddDefault();

                    instanceInfo ??= new();

                    instanceInfo.mesh = renderer.mesh;
                    instanceInfo.material = material;
                    instanceInfo.transform = entry.transform;
                    instanceInfo.submeshIndex = submeshIndex;
                    instanceInfo.visible = renderer.isVisible;
                }

                if (renderer.mesh.IsStaticMesh)
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

        public void UpdateVisibility()
        {
            foreach(var (_, renderData) in perRenderIndexData)
            {
                foreach(var (_, items) in renderData.instanceCache)
                {
                    items.instanceCount = 0;

                    foreach(var instance in items.instanceInfos.Contents)
                    {
                        instance.visible = items.renderer.isVisible;

                        if(instance.visible)
                        {
                            items.instanceCount++;
                        }
                    }
                }

                foreach(var (_, items) in renderData.staticInstanceCache)
                {
                    items.instanceCount = 0;

                    foreach(var entry in items.entries.Contents)
                    {
                        entry.instanceCount = 0;

                        foreach(var transform in entry.transforms.Contents)
                        {
                            transform.visible = transform.renderer.isVisible;

                            if(transform.visible)
                            {
                                entry.instanceCount++;
                                items.instanceCount++;
                            }
                        }
                    }
                }
            }
        }

        public void Sort(int renderIndex, Vector3 origin, RenderableSortMode sortMode)
        {
            if(sortMode == RenderableSortMode.None)
            {
                return;
            }

            if (!perRenderIndexData.TryGetValue(renderIndex, out var renderData))
            {
                return;
            }

            foreach (var (_, items) in renderData.instanceCache)
            {
                items.instanceInfos.Contents.Sort((a, b) =>
                {
                    var visibleA = a.visible;
                    var visibleB = b.visible;

                    if (!visibleA)
                    {
                        return visibleB ? -1 : 0;
                    }
                    else if (!visibleB)
                    {
                        return 1;
                    }

                    var distA = Vector3.DistanceSquared(origin, a.transform.Position);
                    var distB = Vector3.DistanceSquared(origin, b.transform.Position);

                    return sortMode switch
                    {
                        RenderableSortMode.BackToFront => distB.CompareTo(distA),
                        _ => distA.CompareTo(distB),
                    };
                });
            }

            foreach (var (_, items) in renderData.staticInstanceCache)
            {
                foreach (var entry in items.entries.Contents)
                {
                    entry.transforms.Contents.Sort((a, b) =>
                    {
                        var visibleA = a.visible;
                        var visibleB = b.visible;

                        if (!visibleA)
                        {
                            return visibleB ? -1 : 0;
                        }
                        else if (!visibleB)
                        {
                            return 1;
                        }

                        var distA = Vector3.DistanceSquared(origin, a.transform.Position);
                        var distB = Vector3.DistanceSquared(origin, b.transform.Position);

                        return sortMode switch
                        {
                            RenderableSortMode.BackToFront => distB.CompareTo(distA),
                            _ => distA.CompareTo(distB),
                        };
                    });
                }
            }
        }

        public void Submit(int renderIndex)
        {
            if(!perRenderIndexData.TryGetValue(renderIndex, out var renderData))
            {
                return;
            }

            var renderState = RenderState.Default;

            foreach (var contents in renderData.staticInstanceList.Contents)
            {
                if(contents.instanceCount == 0)
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

                RenderSystem.SubmitStatic(renderState, contents.entries.Contents);
            }

            foreach (var contents in renderData.instanceList.Contents)
            {
                if (contents.instanceCount == 0)
                {
                    continue;
                }

                renderState.ClearStorageBuffers();
                renderState.instanceTransforms?.Clear();

                renderState.instanceCount = contents.instanceCount;

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

                if (contents.instanceCount > 1)
                {
                    renderState.instanceTransforms ??= new();

                    foreach (var instance in contents.instanceInfos.Contents)
                    {
                        if (!instance.visible)
                        {
                            continue;
                        }

                        renderState.instanceTransforms.Add(instance.transform);
                    }

                    RenderSystem.Submit(renderState, instanceData.mesh.SubmeshTriangleCount(contents.instanceInfos.Contents[0].submeshIndex),
                        renderState.instanceCount);
                }
                else
                {
                    foreach (var item in contents.instanceInfos.Contents)
                    {
                        if (!item.visible)
                        {
                            continue;
                        }

                        renderState.world = item.transform.Matrix;

                        item.mesh.SetActive(ref renderState, item.submeshIndex);

                        RenderSystem.Submit(renderState, instanceData.mesh.SubmeshTriangleCount(item.submeshIndex), 1);
                    }
                }
            }
        }

        public void Submit()
        {
            foreach(var key in perRenderIndexData.Keys)
            {
                Submit(key);
            }
        }
    }

    private readonly ComponentVersionTracker<MeshRenderer> rendererTracker = new();

    private readonly RenderData renderData = new();

    public MeshRenderSystem() : base(false, typeof(MeshRenderer), typeof(GenericRenderQueue<MeshRenderer>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<MeshRenderer>();

    public override IRenderData CreateRenderData() => new RenderData();

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
        renderData.Clear();

        foreach (var renderer in rendererTracker.ChangedComponents)
        {
            if (renderer.mesh == null)
            {
                continue;
            }

            var transform = renderer.Transform;

            var localSize = renderer.mesh.bounds.size.Transformed(transform.LocalRotation).Abs();

            var globalSize = renderer.mesh.bounds.size.Transformed(transform.Rotation).Abs();

            var localCenter = transform.LocalPosition + renderer.mesh.bounds.center.Transformed(transform.LocalRotation) * transform.NormalizedLocalScale;

            var globalCenter = transform.Position + renderer.mesh.bounds.center.Transformed(transform.Rotation) *
                transform.NormalizedScale;

            renderer.localBounds = new(localCenter, localSize * transform.NormalizedLocalScale);

            renderer.bounds = new(globalCenter, globalSize * transform.NormalizedScale);
        }

        rendererTracker.ClearChanges();
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

            renderer.mesh?.UploadMeshData();
        }
    }

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
        renderData.Process(renderQueue, activeCamera, activeCameraTransform, renderIndex);
    }

    public override void PrepareRenderData(IRenderData renderData, IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform,
        int renderIndex)
    {
        if(renderData is not RenderData data)
        {
            return;
        }

        data.Process(renderQueue, activeCamera, activeCameraTransform, renderIndex);
    }

    public override void UpdateRenderDataVisibility(IRenderData renderData, Camera activeCamera, Transform activeCameraTransform)
    {
        if (renderData is not RenderData data)
        {
            return;
        }

        data.UpdateVisibility();
    }

    public override void Submit()
    {
        renderData.Submit();
    }
}
