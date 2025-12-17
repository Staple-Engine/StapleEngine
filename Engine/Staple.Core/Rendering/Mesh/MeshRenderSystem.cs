using System;
using System.Collections.Generic;
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

    private class InstanceData
    {
        public ExpandableContainer<InstanceInfo> instanceInfos = new();
        public Matrix4x4[] transformMatrices = [];
    }

    private readonly Dictionary<int, InstanceData> instanceCache = [];

    private readonly Dictionary<int, VertexBuffer> instanceBuffers = [];

    private readonly Lazy<VertexLayout> instanceLayout = new(() =>
    {
        return VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.TexCoord0, VertexAttributeType.Float4)
            .Add(VertexAttribute.TexCoord1, VertexAttributeType.Float4)
            .Add(VertexAttribute.TexCoord2, VertexAttributeType.Float4)
            .Add(VertexAttribute.TexCoord3, VertexAttributeType.Float4)
            .Build();
    });

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

    private VertexBuffer GetInstanceBuffer(int count)
    {
        if (instanceBuffers.TryGetValue(count, out var buffer) && buffer.Disposed == false)
        {
            return buffer;
        }

        buffer = VertexBuffer.Create(new Matrix4x4[count], instanceLayout.Value, RenderBufferFlags.GraphicsRead);

        if((buffer?.Disposed ?? true))
        {
            instanceBuffers.Remove(count);

            return null;
        }

        instanceBuffers.AddOrSetKey(count, buffer);

        return buffer;
    }

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
            world = matrix,
        };

        mesh.SetActive(ref renderState);

        material.DisableShaderKeyword(Shader.SkinningKeyword);

        material.DisableShaderKeyword(Shader.InstancingKeyword);

        material.ApplyProperties(Material.ApplyMode.All, ref renderState);

        var lightSystem = RenderSystem.Instance.Get<LightSystem>();

        lightSystem?.ApplyMaterialLighting(material, lighting);

        if (material.ShaderProgram == null)
        {
            return;
        }

        lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position, lighting, ref renderState);

        RenderSystem.Submit(renderState, Mesh.TriangleCount(mesh.MeshTopology, mesh.IndexCount), 1);
    }

    public void Preprocess(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var entry in renderQueue)
        {
            var renderer = entry.component as MeshRenderer;

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

            if (entry.transform.ChangedThisFrame || renderer.localBounds.size == Vector3.Zero)
            {
                var localSize = Vector3.Abs(renderer.mesh.bounds.size.Transformed(entry.transform.LocalRotation));

                var globalSize = Vector3.Abs(renderer.mesh.bounds.size.Transformed(entry.transform.Rotation));

                renderer.localBounds = new(entry.transform.LocalPosition +
                    renderer.mesh.bounds.center.Transformed(entry.transform.LocalRotation) * entry.transform.LocalScale,
                    localSize * entry.transform.LocalScale);

                renderer.bounds = new(entry.transform.Position +
                    renderer.mesh.bounds.center.Transformed(entry.transform.Rotation) * entry.transform.Scale,
                    globalSize * entry.transform.Scale);
            }
        }
    }

    public void Process(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var p in instanceCache)
        {
            p.Value.instanceInfos.Clear();
        }

        foreach (var entry in renderQueue)
        {
            var renderer = entry.component as MeshRenderer;

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

            var lighting = (renderer.overrideLighting ? renderer.lighting : renderer.mesh.meshAsset?.lighting) ?? renderer.lighting;

            void Add(Material material, int submeshIndex)
            {
                var key = HashCode.Combine(renderer.mesh.Guid.GuidHash, material.Guid.GuidHash, lighting, submeshIndex);

                if (instanceCache.TryGetValue(key, out var meshCache) == false)
                {
                    meshCache = new();

                    instanceCache.Add(key, meshCache);
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

        foreach (var p in instanceCache)
        {
            if(p.Value.instanceInfos.Length != p.Value.transformMatrices.Length)
            {
                Array.Resize(ref p.Value.transformMatrices, p.Value.instanceInfos.Length);
            }
        }
    }

    public void Submit()
    {
        var renderState = new RenderState()
        {
            depthWrite = true,
            enableDepth = true,
        };

        foreach (var (_, contents) in instanceCache)
        {
            if (contents.instanceInfos.Length == 0)
            {
                continue;
            }

            renderState.ClearStorageBuffers();

            var renderData = contents.instanceInfos.Contents[0];

            var material = renderData.material;

            var lightSystem = RenderSystem.Instance.Get<LightSystem>();

            material.DisableShaderKeyword(Shader.SkinningKeyword);

            lightSystem?.ApplyMaterialLighting(material, contents.instanceInfos.Contents[0].lighting);

            if (contents.instanceInfos.Contents.Length > 1)
            {
                material.EnableShaderKeyword(Shader.InstancingKeyword);
            }
            else
            {
                material.DisableShaderKeyword(Shader.InstancingKeyword);
            }

            if (material.ShaderProgram == null)
            {
                continue;
            }

            material.ApplyProperties(Material.ApplyMode.All, ref renderState);

            lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position,
                contents.instanceInfos.Contents[0].lighting, ref renderState);

            if (material.IsShaderKeywordEnabled(Shader.InstancingKeyword))
            {
                var program = material.ShaderProgram;

                if(program == null)
                {
                    continue;
                }

                contents.instanceInfos.Contents[0].mesh.SetActive(ref renderState, contents.instanceInfos.Contents[0].submeshIndex);

                for (var i = 0; i < contents.instanceInfos.Length; i++)
                {
                    contents.transformMatrices[i] = contents.instanceInfos.Contents[i].transform.Matrix;
                }

                var buffer = GetInstanceBuffer(contents.transformMatrices.Length);

                if(buffer == null)
                {
                    continue;
                }

                buffer.Update(contents.transformMatrices);

                if (buffer != null)
                {
                    renderState.instanceCount = contents.transformMatrices.Length;

                    renderState.ApplyStorageBufferIfNeeded("StapleInstancingTransforms", buffer);

                    RenderSystem.Submit(renderState, renderData.mesh.SubmeshTriangleCount(contents.instanceInfos.Contents[0].submeshIndex),
                        contents.instanceInfos.Length);
                }
                else
                {
                    Log.Error($"[MeshRenderSystem] Failed to render {contents.instanceInfos.Contents[0].mesh.Guid}: Instance buffer creation failed");
                }
            }
            else
            {
                for (var i = 0; i < contents.instanceInfos.Length; i++)
                {
                    var content = contents.instanceInfos.Contents[i];

                    renderState.world = content.transform.Matrix;

                    content.mesh.SetActive(ref renderState, content.submeshIndex);

                    var program = material.ShaderProgram;

                    if(program != null)
                    {
                        RenderSystem.Submit(renderState, renderData.mesh.SubmeshTriangleCount(content.submeshIndex), 1);
                    }
                }
            }
        }
    }
}
