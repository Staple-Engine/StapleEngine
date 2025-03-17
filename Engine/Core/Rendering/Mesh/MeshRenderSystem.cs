using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

    private readonly Dictionary<ushort, Dictionary<int, ExpandableContainer<InstanceInfo>>> instanceCache = [];

    public bool NeedsUpdate { get; set; }

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

            r.localBounds = r.mesh.bounds;
            r.bounds = new AABB(transform.Position + r.mesh.bounds.center, r.mesh.bounds.extents * 2 * transform.Scale);
        }
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        if (NeedsUpdate == false)
        {
            return;
        }

        instanceCache.Clear();

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

            if (instanceCache.TryGetValue(viewId, out var cache) == false)
            {
                cache = [];

                instanceCache.Add(viewId, cache);
            }

            void Add(Material material, int submeshIndex)
            {
                var key = r.mesh.Guid.GuidHash ^ material.StateHash ^ (int)r.lighting;

                if (cache.TryGetValue(key, out var meshCache) == false)
                {
                    meshCache = new();

                    cache.Add(key, meshCache);
                }

                meshCache.Add(new()
                {
                    mesh = r.mesh,
                    material = material,
                    lighting = r.lighting,
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
    }

    public Type RelatedComponent()
    {
        return typeof(MeshRenderer);
    }

    public void Submit()
    {
        bgfx.discard((byte)bgfx.DiscardFlags.All);

        var state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal;

        foreach(var (viewId, pairs) in instanceCache)
        {
            foreach (var (_, contents) in pairs)
            {
                if (contents.Length == 0)
                {
                    continue;
                }

                bgfx.discard((byte)bgfx.DiscardFlags.All);

                var material = contents.Contents[0].material;

                material.DisableShaderKeyword(Shader.SkinningKeyword);

                var lightSystem = RenderSystem.Instance.Get<LightSystem>();

                lightSystem?.ApplyMaterialLighting(material, contents.Contents[0].lighting);

                if (material.ShaderProgram.Valid == false)
                {
                    continue;
                }

                material.ApplyProperties(Material.ApplyMode.All);

                lightSystem?.ApplyLightProperties(contents.Contents[0].transform.Position, contents.Contents[0].transform.Matrix, material,
                    RenderSystem.CurrentCamera.Item2.Position, contents.Contents[0].lighting);

                material.EnableShaderKeyword(Shader.InstancingKeyword);

                if(material.Keywords.Contains(Shader.InstancingKeyword))
                {
                    bgfx.set_state((ulong)(state |
                        contents.Contents[0].mesh.PrimitiveFlag() |
                        material.shader.BlendingFlag |
                        material.CullingFlag), 0);

                    contents.Contents[0].mesh.SetActive(contents.Contents[0].submeshIndex);

                    var program = material.ShaderProgram;

                    var matrices = new Matrix4x4[contents.Length];

                    for (var i = 0; i < contents.Length; i++)
                    {
                        matrices[i] = contents.Contents[i].transform.Matrix;
                    }

                    var instanceBuffer = InstanceBuffer.Create(contents.Length, 16 * sizeof(float));

                    instanceBuffer.SetData(matrices.AsSpan());

                    instanceBuffer.Bind(0, instanceBuffer.count);

                    bgfx.submit(viewId, program, 0, (byte)bgfx.DiscardFlags.All);
                }
                else
                {
                    for (var i = 0; i < contents.Length; i++)
                    {
                        bgfx.set_state((ulong)(state |
                            contents.Contents[0].mesh.PrimitiveFlag() |
                            material.shader.BlendingFlag |
                            material.CullingFlag), 0);

                        var content = contents.Contents[i];

                        unsafe
                        {
                            var transform = content.transform.Matrix;

                            _ = bgfx.set_transform(&transform, 1);
                        }

                        content.mesh.SetActive(content.submeshIndex);

                        var program = material.ShaderProgram;

                        var flags = bgfx.DiscardFlags.State;

                        bgfx.submit(viewId, program, 0, (byte)flags);
                    }
                }
            }
        }

        bgfx.discard((byte)bgfx.DiscardFlags.All);
    }
}
