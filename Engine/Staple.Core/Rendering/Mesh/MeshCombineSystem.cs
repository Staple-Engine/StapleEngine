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

    private readonly ExpandableContainer<RenderInfo> renderers = new();

    private readonly ComponentVersionTracker<Transform> transformVersions = new();

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

    public void Preprocess(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var entry in renderQueue)
        {
            var combine = (MeshCombine)entry.component;

            combine.renderers ??= new(entry.entity, EntityQueryMode.SelfAndChildren, true);

            foreach (var (_, renderer) in combine.renderers.Contents)
            {
                renderer.cullingState = CullingState.Invisible;
            }

            if (!combine.processed)
            {
                combine.processed = true;

                var combinableMeshes = new Dictionary<(MeshAssetComponent, MeshTopology, MaterialLighting, int), List<(Mesh, Transform, Material)>>();

                Matrix4x4.Invert(entry.transform.Matrix, out var worldTransform);

                foreach (var (e, t, renderer) in combine.renderers.ContentEntities)
                {
                    //For now support only one material
                    if (!renderer.enabled ||
                        renderer.mesh == null ||
                        (renderer.materials?.Count ?? 0) == 0 ||
                        !(renderer.materials[0]?.IsValid ?? false) ||
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

                    if (!combinableMeshes.TryGetValue(key, out var container))
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
                            vertices.Add(position.Transformed(matrix));
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

            if (transformVersions.ShouldUpdateComponent(entry.entity, in entry.transform))
            {
                var localSize = Vector3.Abs(combine.combinedMeshBounds.size.Transformed(entry.transform.LocalRotation));

                var globalSize = Vector3.Abs(combine.combinedMeshBounds.size.Transformed(entry.transform.Rotation));

                combine.localBounds = new(entry.transform.LocalPosition +
                    combine.combinedMeshBounds.center.Transformed(entry.transform.LocalRotation) * entry.transform.LocalScale,
                    localSize * entry.transform.LocalScale);

                combine.bounds = new(entry.transform.Position +
                    combine.combinedMeshBounds.center.Transformed(entry.transform.Rotation) * entry.transform.Scale,
                    globalSize * entry.transform.Scale);
            }
        }
    }

    public void Process(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
        renderers.Clear();

        foreach (var entry in renderQueue)
        {
            if (entry.component is not MeshCombine combine ||
                combine.meshes.Count == 0 ||
                combine.materials.Count != combine.meshes.Count)
            {
                continue;
            }

            renderers.Add(new()
            {
                renderer = combine,
                transform = entry.transform,
            });
        }
    }

    public void Submit()
    {
        Material lastMaterial = null;

        var lastLighting = MaterialLighting.Unlit;
        var lastTopology = MeshTopology.Triangles;

        var l = renderers.Length;

        for (var i = 0; i < l; i++)
        {
            var item = renderers.Contents[i];

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

                var renderState = new RenderState()
                {
                    cull = material.CullingMode,
                    primitiveType = mesh.MeshTopology,
                    depthWrite = true,
                    enableDepth = true,
                    indexBuffer = mesh.indexBuffer,
                    vertexBuffer = mesh.vertexBuffer,
                    indexCount = mesh.IndexCount,
                    world = item.transform.Matrix,
                };

                if (needsChange)
                {
                    lastMaterial = material;
                    lastLighting = lighting;
                    lastTopology = mesh.MeshTopology;

                    SetupMaterial();

                    if (material.ShaderProgram == null)
                    {
                        continue;
                    }

                    material.ApplyProperties(ref renderState);
                }

                SetupMaterial();

                if (material.ShaderProgram == null)
                {
                    continue;
                }

                lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position, lighting);

                RenderSystem.Submit(renderState, mesh.SubmeshTriangleCount(0), 1);
            }
        }
    }
}
