using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Mesh render system
/// </summary>
public sealed class MeshRenderSystem : IRenderSystem
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

    private class InstanceData
    {
        public ExpandableContainer<InstanceInfo> instanceInfos = new();
        public Matrix4x4[] transformMatrices = [];
    }

    private readonly Dictionary<ushort, Dictionary<int, InstanceData>> instanceCache = [];

    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => typeof(MeshRenderer);

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

    /// <summary>
    /// Renders a mesh
    /// </summary>
    /// <param name="mesh">The mesh</param>
    /// <param name="position">The position of the mesh</param>
    /// <param name="rotation">The rotation of the mesh</param>
    /// <param name="scale">The scale of the mesh</param>
    /// <param name="material">The material to use</param>
    /// <param name="lighting">The lighting model to use</param>
    /// <param name="viewID">The view ID to render to</param>
    public static void RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material,
        MaterialLighting lighting, ushort viewID)
    {
        if(mesh == null ||
            material == null ||
            material.IsValid == false)
        {
            return;
        }

        mesh.UploadMeshData();

        var matrix = Matrix4x4.TRS(position, scale, rotation);

        var renderState = new RenderState()
        {
            enableDepth = true,
            depthWrite = true,
            primitiveType = mesh.MeshTopology,
            cull = material.CullingMode,
            indexBuffer = mesh.indexBuffer,
            vertexBuffer = mesh.vertexBuffer,
            indexCount = mesh.IndexCount,
            world = matrix,
        };

        mesh.SetActive(ref renderState);

        material.DisableShaderKeyword(Shader.SkinningKeyword);

        material.DisableShaderKeyword(Shader.InstancingKeyword);

        material.ApplyProperties(Material.ApplyMode.All, ref renderState);

        var lightSystem = RenderSystem.Instance.Get<LightSystem>();

        lightSystem?.ApplyMaterialLighting(material, lighting);

        var program = material.ShaderProgram;

        if (program != null)
        {
            renderState.program = program;

            lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position, lighting);

            RenderSystem.Submit(RenderSystem.GetViewPass(viewID), renderState, Mesh.TriangleCount(mesh.MeshTopology, mesh.IndexCount), 1);
        }
    }

    public void ClearRenderData(ushort viewID)
    {
        instanceCache.Remove(viewID);
    }

    public void Preprocess(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var (_, transform, relatedComponent) in entities)
        {
            var renderer = relatedComponent as MeshRenderer;

            if (renderer.mesh == null ||
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

            if(transform.ChangedThisFrame || renderer.localBounds.size == Vector3.Zero)
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

    public void Process(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
        if (instanceCache.TryGetValue(viewID, out var instance) == false)
        {
            instance = [];

            instanceCache.Add(viewID, instance);
        }
        else
        {
            foreach (var p in instance)
            {
                p.Value.instanceInfos.Clear();
            }
        }

        foreach (var (_, transform, relatedComponent) in entities)
        {
            var renderer = relatedComponent as MeshRenderer;

            if (renderer.isVisible == false ||
                renderer.mesh == null ||
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

            if (instanceCache.TryGetValue(viewID, out var cache) == false)
            {
                cache = [];

                instanceCache.Add(viewID, cache);
            }

            var lighting = (renderer.overrideLighting ? renderer.lighting : renderer.mesh.meshAsset?.lighting) ?? renderer.lighting;

            void Add(Material material, int submeshIndex)
            {
                var key = HashCode.Combine(renderer.mesh.Guid.GuidHash, material.Guid.GuidHash, lighting, submeshIndex);

                if (cache.TryGetValue(key, out var meshCache) == false)
                {
                    meshCache = new();

                    cache.Add(key, meshCache);
                }

                meshCache.instanceInfos.Add(new()
                {
                    mesh = renderer.mesh,
                    material = material,
                    lighting = lighting,
                    transform = transform,
                    submeshIndex = submeshIndex,
                });
            }

            if (renderer.mesh.submeshes.Count == 0)
            {
                Add(renderer.materials[0], 0);
            }
            else
            {
                for (var i = 0; i < renderer.mesh.submeshes.Count; i++)
                {
                    if(i >= renderer.materials.Count)
                    {
                        break;
                    }

                    Add(renderer.materials[i], i);
                }
            }
        }

        foreach (var pair in instanceCache)
        {
            foreach (var p in pair.Value)
            {
                if(p.Value.instanceInfos.Length != p.Value.transformMatrices.Length)
                {
                    Array.Resize(ref p.Value.transformMatrices, p.Value.instanceInfos.Length);
                }
            }
        }
    }

    public void Submit(ushort viewID)
    {
        if(instanceCache.TryGetValue(viewID, out var instance) == false)
        {
            return;
        }

        Material lastMaterial = null;
        MaterialLighting lastLighting = MaterialLighting.Unlit;
        var lastInstances = 0;
        var forceDiscard = false;

        foreach (var (_, contents) in instance)
        {
            if (contents.instanceInfos.Length == 0)
            {
                continue;
            }

            var renderData = contents.instanceInfos.Contents[0];

            var needsDiscard = forceDiscard ||
                lastMaterial != renderData.material ||
                lastLighting != renderData.lighting ||
                (contents.instanceInfos.Contents.Length == 1) != (lastInstances == 1);

            var material = renderData.material;

            var lightSystem = RenderSystem.Instance.Get<LightSystem>();

            var renderState = new RenderState()
            {
                cull = material.CullingMode,
                depthWrite = true,
                enableDepth = true,
            };

            if (needsDiscard)
            {
                lastMaterial = material;
                lastLighting = renderData.lighting;
                lastInstances = contents.instanceInfos.Contents.Length;
                forceDiscard = false;

                material.DisableShaderKeyword(Shader.SkinningKeyword);

                lightSystem?.ApplyMaterialLighting(material, contents.instanceInfos.Contents[0].lighting);

                if (material.ShaderProgram == null)
                {
                    continue;
                }

                material.ApplyProperties(Material.ApplyMode.All, ref renderState);

                lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position,
                    contents.instanceInfos.Contents[0].lighting);
            }

            if (contents.instanceInfos.Contents.Length > 1)
            {
                material.EnableShaderKeyword(Shader.InstancingKeyword);
            }
            else
            {
                material.DisableShaderKeyword(Shader.InstancingKeyword);
            }

            if (material.IsShaderKeywordEnabled(Shader.InstancingKeyword) && false)
            {
                /*
                bgfx.set_state((ulong)(material.shader.StateFlags |
                    contents.instanceInfos.Contents[0].mesh.PrimitiveFlag() |
                    material.CullingFlag), 0);

                contents.instanceInfos.Contents[0].mesh.SetActive(contents.instanceInfos.Contents[0].submeshIndex);

                var program = material.ShaderProgram;

                for (var i = 0; i < contents.instanceInfos.Length; i++)
                {
                    contents.transformMatrices[i] = contents.instanceInfos.Contents[i].transform.Matrix;
                }

                var instanceBuffer = InstanceBuffer.Create(contents.transformMatrices.Length, Marshal.SizeOf<Matrix4x4>());

                if (instanceBuffer != null)
                {
                    instanceBuffer.SetData(contents.transformMatrices.AsSpan());

                    instanceBuffer.Bind(0, instanceBuffer.count);

                    var flags = bgfx.DiscardFlags.State;

                    RenderSystem.Submit(viewID, program, flags,
                        renderData.mesh.SubmeshTriangleCount(contents.instanceInfos.Contents[0].submeshIndex),
                        instanceBuffer.count);
                }
                else
                {
                    Log.Error($"[MeshRenderSystem] Failed to render {contents.instanceInfos.Contents[0].mesh.Guid}: Instance buffer creation failed");

                    forceDiscard = true;

                    bgfx.discard((byte)bgfx.DiscardFlags.All);
                }
                */
            }
            else
            {
                for (var i = 0; i < contents.instanceInfos.Length; i++)
                {
                    var content = contents.instanceInfos.Contents[i];

                    renderState.vertexBuffer = content.mesh.vertexBuffer;
                    renderState.indexBuffer = content.mesh.indexBuffer;
                    renderState.primitiveType = content.mesh.MeshTopology;
                    renderState.world = content.transform.Matrix;

                    content.mesh.SetActive(ref renderState, content.submeshIndex);

                    var program = material.ShaderProgram;

                    if(program != null)
                    {
                        renderState.program = program;

                        RenderSystem.Submit(RenderSystem.GetViewPass(viewID), renderState, renderData.mesh.SubmeshTriangleCount(content.submeshIndex), 1);
                    }
                }
            }
        }
    }
}
