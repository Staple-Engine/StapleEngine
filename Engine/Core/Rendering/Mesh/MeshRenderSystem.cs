﻿using Bgfx;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Mesh render system
/// </summary>
public sealed class MeshRenderSystem : IRenderSystem
{
    private const int LightBufferIndex = 14;

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
        public Transform[] transforms;
        public Matrix4x4[] normalMatrices;
        public Matrix4x4[] transformMatrices;
        public VertexBuffer normalMatrixBuffer;
    }

    private readonly Dictionary<ushort, Dictionary<int, InstanceData>> instanceCache = [];

    public bool NeedsUpdate { get; set; }

    public bool UsesOwnRenderProcess => false;

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
    public static void RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material, MaterialLighting lighting, ushort viewID)
    {
        if(mesh == null ||
            material == null ||
            material.IsValid == false)
        {
            return;
        }

        bgfx.discard((byte)bgfx.DiscardFlags.All);

        mesh.UploadMeshData();

        var matrix = Math.TransformationMatrix(position, scale, rotation);

        bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal |
            mesh.PrimitiveFlag() |
            material.shader.BlendingFlag |
            material.CullingFlag;

        unsafe
        {
            _ = bgfx.set_transform(&matrix, 1);
        }

        bgfx.set_state((ulong)state, 0);

        mesh.SetActive();

        material.DisableShaderKeyword(Shader.SkinningKeyword);

        material.DisableShaderKeyword(Shader.InstancingKeyword);

        material.ApplyProperties(Material.ApplyMode.All);

        var lightSystem = RenderSystem.Instance.Get<LightSystem>();

        lightSystem?.ApplyMaterialLighting(material, lighting);

        var program = material.ShaderProgram;

        if (program.Valid)
        {
            lightSystem?.ApplyLightProperties(position, matrix, material, RenderSystem.CurrentCamera.Item2.Position, lighting);

            bgfx.submit(viewID, program, 0, (byte)bgfx.DiscardFlags.All);
        }
        else
        {
            bgfx.discard((byte)bgfx.DiscardFlags.All);
        }
    }

    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void ClearRenderData(ushort viewID)
    {
        if (instanceCache.TryGetValue(viewID, out var renderData))
        {
            foreach(var pair in renderData)
            {
                pair.Value.normalMatrixBuffer?.Destroy();
            }

            instanceCache.Remove(viewID);
        }
    }

    public void Prepare()
    {
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var (_, transform, relatedComponent) in entities)
        {
            var r = relatedComponent as MeshRenderer;

            if (r.mesh == null ||
                r.materials == null ||
                r.materials.Count == 0)
            {
                continue;
            }

            var skip = false;

            for (var i = 0; i < r.materials.Count; i++)
            {
                if ((r.materials[i]?.IsValid ?? false) == false)
                {
                    skip = true;

                    break;
                }
            }

            if(skip)
            {
                continue;
            }

            if (r.mesh.submeshes.Count > 0 && r.materials.Count != r.mesh.submeshes.Count)
            {
                continue;
            }

            r.mesh.UploadMeshData();

            r.localBounds = new(transform.LocalPosition + r.mesh.bounds.center * transform.LocalScale, r.mesh.bounds.size * transform.LocalScale);
            r.bounds = new(transform.Position + r.mesh.bounds.center * transform.Scale, r.mesh.bounds.size * transform.Scale);
        }
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
        if (NeedsUpdate == false)
        {
            return;
        }

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
            var r = relatedComponent as MeshRenderer;

            if (r.isVisible == false ||
                r.mesh == null ||
                r.materials == null ||
                r.materials.Count == 0)
            {
                continue;
            }

            var skip = false;

            for (var i = 0; i < r.materials.Count; i++)
            {
                if ((r.materials[i]?.IsValid ?? false) == false)
                {
                    skip = true;

                    break;
                }
            }

            if (skip)
            {
                continue;
            }

            if (r.mesh.submeshes.Count > 0 && r.materials.Count != r.mesh.submeshes.Count)
            {
                continue;
            }

            if (instanceCache.TryGetValue(viewID, out var cache) == false)
            {
                cache = [];

                instanceCache.Add(viewID, cache);
            }

            var lighting = (r.overrideLighting ? r.lighting : r.mesh.meshAsset?.lighting) ?? r.lighting;

            void Add(Material material, int submeshIndex)
            {
                var key = r.mesh.Guid.GuidHash ^ material.StateHash ^ (int)lighting;

                if (cache.TryGetValue(key, out var meshCache) == false)
                {
                    meshCache = new();

                    cache.Add(key, meshCache);
                }

                meshCache.instanceInfos.Add(new()
                {
                    mesh = r.mesh,
                    material = material,
                    lighting = lighting,
                    transform = transform,
                    submeshIndex = submeshIndex,
                });
            }

            if (r.mesh.submeshes.Count == 0)
            {
                Add(r.materials[0], 0);
            }
            else
            {
                for (var i = 0; i < r.mesh.submeshes.Count; i++)
                {
                    Add(r.materials[0], i);
                }
            }
        }

        foreach (var pair in instanceCache)
        {
            foreach (var p in pair.Value)
            {
                if(p.Value.instanceInfos.Length == 0)
                {
                    p.Value.normalMatrixBuffer?.Destroy();
                    p.Value.transforms = [];
                    p.Value.normalMatrices = [];
                    p.Value.transformMatrices = [];
                }
            }
        }
    }

    public Type RelatedComponent()
    {
        return typeof(MeshRenderer);
    }

    public void Submit(ushort viewID)
    {
        if(instanceCache.TryGetValue(viewID, out var instance) == false)
        {
            return;
        }

        bgfx.discard((byte)bgfx.DiscardFlags.All);

        var state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal;

        foreach (var (_, contents) in instance)
        {
            if (contents.instanceInfos.Length == 0)
            {
                continue;
            }

            bgfx.discard((byte)bgfx.DiscardFlags.All);

            var material = contents.instanceInfos.Contents[0].material;

            material.DisableShaderKeyword(Shader.SkinningKeyword);

            var lightSystem = RenderSystem.Instance.Get<LightSystem>();

            lightSystem?.ApplyMaterialLighting(material, contents.instanceInfos.Contents[0].lighting);

            if (material.ShaderProgram.Valid == false)
            {
                continue;
            }

            material.ApplyProperties(Material.ApplyMode.All);

            material.EnableShaderKeyword(Shader.InstancingKeyword);

            if (material.IsShaderKeywordEnabled(Shader.InstancingKeyword))
            {
                bgfx.set_state((ulong)(state |
                    contents.instanceInfos.Contents[0].mesh.PrimitiveFlag() |
                    material.shader.BlendingFlag |
                    material.CullingFlag), 0);

                contents.instanceInfos.Contents[0].mesh.SetActive(contents.instanceInfos.Contents[0].submeshIndex);

                var program = material.ShaderProgram;

                if ((contents.transforms?.Length ?? 0) != contents.instanceInfos.Length)
                {
                    contents.transforms = new Transform[contents.instanceInfos.Length];
                    contents.transformMatrices = new Matrix4x4[contents.instanceInfos.Length];
                    contents.normalMatrices = new Matrix4x4[contents.instanceInfos.Length];

                    for (var i = 0; i < contents.transforms.Length; i++)
                    {
                        contents.transforms[i] = contents.instanceInfos.Contents[i].transform;
                    }
                }

                lightSystem?.ApplyInstancedLightProperties(contents.transforms, contents.normalMatrices, material,
                    RenderSystem.CurrentCamera.Item2.Position, contents.instanceInfos.Contents[0].lighting);

                if (contents.normalMatrixBuffer?.Disposed ?? true)
                {
                    //TODO: Support Matrix3x3 instead for this
                    contents.normalMatrixBuffer = VertexBuffer.CreateDynamic(new VertexLayoutBuilder()
                        .Add(VertexAttribute.TexCoord0, 4, VertexAttributeType.Float)
                        .Add(VertexAttribute.TexCoord1, 4, VertexAttributeType.Float)
                        .Add(VertexAttribute.TexCoord2, 4, VertexAttributeType.Float)
                        .Add(VertexAttribute.TexCoord3, 4, VertexAttributeType.Float)
                        .Build(),
                        RenderBufferFlags.ComputeRead, true, (uint)contents.transforms.Length);
                }

                contents.normalMatrixBuffer.Update(contents.normalMatrices.AsSpan(), 0, true);

                for (var i = 0; i < contents.transforms.Length; i++)
                {
                    contents.transformMatrices[i] = contents.transforms[i].Matrix;
                }

                var instanceBuffer = InstanceBuffer.Create(contents.transformMatrices.Length, Marshal.SizeOf<Matrix4x4>());

                if (instanceBuffer != null)
                {
                    instanceBuffer.SetData(contents.transformMatrices.AsSpan());

                    instanceBuffer.Bind(0, instanceBuffer.count);

                    contents.normalMatrixBuffer.SetBufferActive(LightBufferIndex, Access.Read);

                    bgfx.submit(viewID, program, 0, (byte)bgfx.DiscardFlags.All);
                }
                else
                {
                    Log.Error($"[MeshRenderSystem] Failed to render {contents.instanceInfos.Contents[0].mesh.Guid}: Instance buffer creation failed");

                    bgfx.discard((byte)bgfx.DiscardFlags.All);
                }
            }
            else
            {
                for (var i = 0; i < contents.instanceInfos.Length; i++)
                {
                    bgfx.set_state((ulong)(state |
                        contents.instanceInfos.Contents[0].mesh.PrimitiveFlag() |
                        material.shader.BlendingFlag |
                        material.CullingFlag), 0);

                    var content = contents.instanceInfos.Contents[i];

                    unsafe
                    {
                        var transform = content.transform.Matrix;

                        _ = bgfx.set_transform(&transform, 1);
                    }

                    content.mesh.SetActive(content.submeshIndex);

                    lightSystem?.ApplyLightProperties(content.transform.Position, content.transform.Matrix, material,
                        RenderSystem.CurrentCamera.Item2.Position, content.lighting);

                    var program = material.ShaderProgram;

                    var flags = bgfx.DiscardFlags.State;

                    bgfx.submit(viewID, program, 0, (byte)flags);
                }
            }
        }

        bgfx.discard((byte)bgfx.DiscardFlags.All);
    }
}
