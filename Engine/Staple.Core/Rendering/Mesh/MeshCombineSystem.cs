using Bgfx;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

public sealed class MeshCombineSystem : IRenderSystem
{
    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => typeof(MeshCombine);

    /// <summary>
    /// Info for rendering
    /// </summary>
    private struct RenderInfo
    {
        /// <summary>
        /// The renderer
        /// </summary>
        public MeshCombine renderer;

        /// <summary>
        /// The transform of the object
        /// </summary>
        public Transform transform;
    }

    private readonly Dictionary<ushort, ExpandableContainer<RenderInfo>> renderers = [];

    #region Lifecycle
    public void Prepare()
    {
    }

    public void Startup()
    {
    }

    public void Shutdown()
    {
    }
    #endregion

    public void Preprocess(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var (entity, transform, relatedComponent) in entities)
        {
            if (relatedComponent is not MeshCombine combine)
            {
                continue;
            }

            combine.renderers ??= new(entity, EntityQueryMode.SelfAndChildren, true);

            foreach (var (_, renderer) in combine.renderers.Contents)
            {
                renderer.cullingState = CullingState.Invisible;
            }

            if (combine.processed == false)
            {
                combine.processed = true;

                var combinableMeshes = new Dictionary<(MeshAssetComponent, MeshTopology, MaterialLighting, int), List<(Mesh, Transform, Material)>>();

                Matrix4x4.Invert(transform.Matrix, out var worldTransform);

                foreach (var (e, t, renderer) in combine.renderers.ContentEntities)
                {
                    //For now support only one material
                    if (renderer.enabled == false ||
                        renderer.mesh == null ||
                        (renderer.materials?.Count ?? 0) == 0 ||
                        (renderer.materials[0]?.IsValid ?? false) == false ||
                        renderer.mesh.submeshes.Count > 1)
                    {
                        continue;
                    }

                    var components = renderer.mesh.Components;

                    if (components == MeshAssetComponent.None || components.HasFlag(MeshAssetComponent.BoneIndicesWeights))
                    {
                        continue;
                    }

                    var lighting = renderer.overrideLighting ? renderer.lighting :
                        renderer.mesh.meshAsset?.lighting ?? renderer.lighting;

                    var key = (components, renderer.mesh.MeshTopology, lighting, renderer.materials[0].Guid.GuidHash);

                    if (combinableMeshes.TryGetValue(key, out var container) == false)
                    {
                        container = [];

                        combinableMeshes.Add(key, container);
                    }

                    container.Add((renderer.mesh, t, renderer.materials[0]));
                }

                var combinedMeshBounds = new List<Vector3>();

                foreach (var pair in combinableMeshes)
                {
                    var combinedMesh = new Mesh(true, false)
                    {
                        meshTopology = pair.Key.Item2,
                        indexFormat = MeshIndexFormat.UInt32,
                    };

                    var vertices = new List<Vector3>();
                    var normals = new List<Vector3>();
                    var tangents = new List<Vector3>();
                    var bitangents = new List<Vector3>();
                    var color0 = new List<Color>();
                    var color1 = new List<Color>();
                    var color2 = new List<Color>();
                    var color3 = new List<Color>();
                    var color4 = new List<Color>();
                    var color32 = new List<Color32>();
                    var color322 = new List<Color32>();
                    var color323 = new List<Color32>();
                    var color324 = new List<Color32>();
                    var uv0 = new List<Vector2>();
                    var uv1 = new List<Vector2>();
                    var uv2 = new List<Vector2>();
                    var uv3 = new List<Vector2>();
                    var uv4 = new List<Vector2>();
                    var uv5 = new List<Vector2>();
                    var uv6 = new List<Vector2>();
                    var uv7 = new List<Vector2>();
                    var indices = new List<int>();

                    Material material = null;

                    foreach (var (mesh, t, m) in pair.Value)
                    {
                        material = m;

                        var startVertex = vertices.Count;

                        var matrix = t.Matrix * worldTransform;

                        foreach (var position in mesh.vertices)
                        {
                            vertices.Add(Vector3.Transform(position, matrix));
                        }

                        if((mesh.normals?.Length ?? 0) > 0)
                        {
                            Matrix4x4.Invert(matrix, out var normalMatrix);

                            normalMatrix = Matrix4x4.Transpose(normalMatrix);

                            foreach(var normal in mesh.normals)
                            {
                                normals.Add(Vector3.TransformNormal(normal, normalMatrix));
                            }
                        }

                        void AddIfValid<T>(List<T> target, T[] source)
                        {
                            if(source == null)
                            {
                                return;
                            }

                            target.AddRange(source);
                        }

                        AddIfValid(tangents, mesh.tangents);
                        AddIfValid(bitangents, mesh.bitangents);
                        AddIfValid(color0, mesh.colors);
                        AddIfValid(color1, mesh.colors2);
                        AddIfValid(color2, mesh.colors3);
                        AddIfValid(color3, mesh.colors4);
                        AddIfValid(color32, mesh.colors32);
                        AddIfValid(color322, mesh.colors322);
                        AddIfValid(color323, mesh.colors323);
                        AddIfValid(color324, mesh.colors324);
                        AddIfValid(uv0, mesh.uv);
                        AddIfValid(uv1, mesh.uv2);
                        AddIfValid(uv2, mesh.uv3);
                        AddIfValid(uv3, mesh.uv4);
                        AddIfValid(uv4, mesh.uv5);
                        AddIfValid(uv5, mesh.uv6);
                        AddIfValid(uv6, mesh.uv7);
                        AddIfValid(uv7, mesh.uv8);

                        indices.AddRange(mesh.indices.Select(x => x + startVertex));
                    }

                    void ApplyIfValid<T>(ref T[] target, List<T> source)
                    {
                        if(source.Count == 0)
                        {
                            return;
                        }

                        target = source.ToArray();
                    }

                    ApplyIfValid(ref combinedMesh.vertices, vertices);
                    ApplyIfValid(ref combinedMesh.normals, normals);
                    ApplyIfValid(ref combinedMesh.tangents, tangents);
                    ApplyIfValid(ref combinedMesh.bitangents, bitangents);
                    ApplyIfValid(ref combinedMesh.colors, color0);
                    ApplyIfValid(ref combinedMesh.colors2, color1);
                    ApplyIfValid(ref combinedMesh.colors3, color2);
                    ApplyIfValid(ref combinedMesh.colors4, color3);
                    ApplyIfValid(ref combinedMesh.colors32, color32);
                    ApplyIfValid(ref combinedMesh.colors322, color322);
                    ApplyIfValid(ref combinedMesh.colors323, color323);
                    ApplyIfValid(ref combinedMesh.colors324, color324);
                    ApplyIfValid(ref combinedMesh.uv, uv0);
                    ApplyIfValid(ref combinedMesh.uv2, uv1);
                    ApplyIfValid(ref combinedMesh.uv3, uv2);
                    ApplyIfValid(ref combinedMesh.uv4, uv3);
                    ApplyIfValid(ref combinedMesh.uv5, uv4);
                    ApplyIfValid(ref combinedMesh.uv6, uv5);
                    ApplyIfValid(ref combinedMesh.uv7, uv6);
                    ApplyIfValid(ref combinedMesh.uv8, uv7);
                    ApplyIfValid(ref combinedMesh.indices, indices);

                    combinedMesh.UpdateBounds();

                    if (combinedMesh.bounds.size != Vector3.Zero)
                    {
                        combinedMeshBounds.Add(combinedMesh.bounds.min);
                        combinedMeshBounds.Add(combinedMesh.bounds.max);
                    }

                    combine.meshes.Add((combinedMesh, pair.Key.Item3));
                    combine.materials.Add(material);
                }

                if (combinedMeshBounds.Count > 0)
                {
                    combine.combinedMeshBounds = AABB.CreateFromPoints(CollectionsMarshal.AsSpan(combinedMeshBounds));
                }

                switch(combine.childMode)
                {
                    case MeshCombine.MeshCombineChildMode.DestroyRenderers:

                        if(Platform.IsPlaying)
                        {
                            foreach(var (e, _, _) in combine.renderers.ContentEntities)
                            {
                                e.RemoveComponent<MeshRenderer>();
                            }
                        }

                        break;
                }
            }

            if (transform.ChangedThisFrame || combine.localBounds.size == Vector3.Zero)
            {
                var localSize = Vector3.Abs(Vector3.Transform(combine.combinedMeshBounds.size, transform.LocalRotation));

                var globalSize = Vector3.Abs(Vector3.Transform(combine.combinedMeshBounds.size, transform.Rotation));

                combine.localBounds = new(transform.LocalPosition + Vector3.Transform(combine.combinedMeshBounds.center, transform.LocalRotation) * transform.LocalScale,
                    localSize * transform.LocalScale);

                combine.bounds = new(transform.Position + Vector3.Transform(combine.combinedMeshBounds.center, transform.Rotation) * transform.Scale,
                    globalSize * transform.Scale);
            }
        }
    }

    public void Process(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
        if (renderers.TryGetValue(viewID, out var container) == false)
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
            if (relatedComponent is not MeshCombine combine ||
                combine.meshes.Count == 0 ||
                combine.materials.Count != combine.meshes.Count)
            {
                continue;
            }

            container.Add(new()
            {
                renderer = combine,
                transform = transform,
            });
        }
    }

    public void ClearRenderData(ushort viewID)
    {
        renderers.Remove(viewID);
    }

    public void Submit(ushort viewID)
    {
        if (renderers.TryGetValue(viewID, out var content) == false)
        {
            return;
        }

        Material lastMaterial = null;

        var lastLighting = MaterialLighting.Unlit;
        var lastTopology = MeshTopology.Triangles;

        bgfx.discard((byte)bgfx.DiscardFlags.All);

        var l = content.Length;

        for (var i = 0; i < l; i++)
        {
            var item = content.Contents[i];

            var renderer = item.renderer;

            var meshCount = renderer.meshes.Count;

            for(var j = 0; j < meshCount; j++)
            {
                var (mesh, lighting) = renderer.meshes[j];
                var material = renderer.materials[j];

                var needsChange = material.StateHash != (lastMaterial?.StateHash ?? 0) ||
                    lastLighting != lighting ||
                    lastTopology != mesh.MeshTopology;

                var lightSystem = RenderSystem.Instance.Get<LightSystem>();

                void SetupMaterial()
                {
                    material.DisableShaderKeyword(Shader.SkinningKeyword);
                    material.DisableShaderKeyword(Shader.InstancingKeyword);

                    lightSystem?.ApplyMaterialLighting(material, lighting);
                }

                if (needsChange)
                {
                    lastMaterial = material;
                    lastLighting = lighting;
                    lastTopology = mesh.MeshTopology;

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

                mesh.SetActive(0);

                lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position, lighting);

                var program = material.ShaderProgram;

                bgfx.set_state((ulong)(material.shader.StateFlags |
                    mesh.PrimitiveFlag() |
                    material.CullingFlag), 0);

                var flags = bgfx.DiscardFlags.State;

                RenderSystem.Submit(viewID, program, flags, mesh.SubmeshTriangleCount(0), 1);
            }
        }
    }
}
