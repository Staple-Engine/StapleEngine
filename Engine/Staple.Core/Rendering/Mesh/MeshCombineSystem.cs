using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Mesh Combine system that merges child mesh renderers inside it
/// </summary>
public sealed class MeshCombineSystem : RenderSystemBase
{
    private readonly SceneQuery<MeshCombine> combines = new(false);

    public MeshCombineSystem() : base(true, typeof(MeshCombine), typeof(GenericRenderQueue<MeshCombine>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<MeshCombine>();

    public override IRenderData CreateRenderData() => null;

    #region Lifecycle
    public override void Startup()
    {
    }

    public override void Shutdown()
    {
    }

    public override void PrepareRenderData(IRenderData renderData, IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform,
        int renderIndex)
    {
    }

    public override void UpdateRenderDataVisibility(IRenderData renderData, Camera activeCamera, Transform activeCameraTransform)
    {
    }
    #endregion

    public override void Prepare()
    {
        if(!Platform.IsPlaying)
        {
            return;
        }

        foreach(var combine in combines.Contents)
        {
            combine.renderers ??= new(combine.Entity, EntityQueryMode.SelfAndChildren);

            foreach (var renderer in combine.renderers.Contents)
            {
                renderer.cullingState = CullingState.Invisible;
            }

            if (!combine.processed)
            {
                combine.processed = true;

                combine.combinedMeshes.Clear();
                combine.combinedMaterials.Clear();

                var combinableMeshes = new Dictionary<(MeshAssetComponent, MeshTopology, int), List<(Mesh, Transform, Material)>>();

                Matrix4x4.Invert(combine.Transform.Matrix, out var worldTransform);

                foreach (var renderer in combine.renderers.Contents)
                {
                    //For now support only one material
                    if (!renderer.Enabled ||
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

                    var key = (components, renderer.mesh.MeshTopology, renderer.materials[0].Guid.GuidHash);

                    if (!combinableMeshes.TryGetValue(key, out var container))
                    {
                        container = [];

                        combinableMeshes.Add(key, container);
                    }

                    container.Add((renderer.mesh, renderer.Transform, renderer.materials[0]));
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

                        foreach (var position in mesh.VerticesInternal)
                        {
                            vertices.Add(position.Transformed(in matrix));
                        }

                        if ((mesh.NormalsInternal?.Length ?? 0) > 0)
                        {
                            Matrix4x4.Invert(matrix, out var normalMatrix);

                            normalMatrix = Matrix4x4.Transpose(normalMatrix);

                            foreach (var normal in mesh.NormalsInternal)
                            {
                                normals.Add(Vector3.TransformNormal(normal, normalMatrix));
                            }
                        }

                        void AddIfValid<T>(List<T> target, T[] source)
                        {
                            if (source == null)
                            {
                                return;
                            }

                            target.AddRange(source);
                        }

                        AddIfValid(tangents, mesh.TangentsInternal);
                        AddIfValid(bitangents, mesh.BitangentsInternal);
                        AddIfValid(color0, mesh.ColorsInternal);
                        AddIfValid(color1, mesh.Colors2Internal);
                        AddIfValid(color2, mesh.Colors3Internal);
                        AddIfValid(color3, mesh.Colors4Internal);
                        AddIfValid(uv0, mesh.UVInternal);
                        AddIfValid(uv1, mesh.UV2Internal);
                        AddIfValid(uv2, mesh.UV3Internal);
                        AddIfValid(uv3, mesh.UV4Internal);
                        AddIfValid(uv4, mesh.UV5Internal);
                        AddIfValid(uv5, mesh.UV6Internal);
                        AddIfValid(uv6, mesh.UV7Internal);
                        AddIfValid(uv7, mesh.UV8Internal);

                        indices.AddRange(mesh.IndicesInternal.Select(x => x + startVertex));
                    }

                    combinedMesh.VerticesInternal = [.. vertices];
                    combinedMesh.NormalsInternal = [.. normals];
                    combinedMesh.TangentsInternal = [.. tangents];
                    combinedMesh.BitangentsInternal = [.. bitangents];
                    combinedMesh.ColorsInternal = [.. color0];
                    combinedMesh.Colors2Internal = [.. color1];
                    combinedMesh.Colors3Internal = [.. color2];
                    combinedMesh.Colors4Internal = [.. color3];
                    combinedMesh.UVInternal = [.. uv0];
                    combinedMesh.UV2Internal = [.. uv1];
                    combinedMesh.UV3Internal = [.. uv2];
                    combinedMesh.UV4Internal = [.. uv3];
                    combinedMesh.UV5Internal = [.. uv4];
                    combinedMesh.UV6Internal = [.. uv5];
                    combinedMesh.UV7Internal = [.. uv6];
                    combinedMesh.UV8Internal = [.. uv7];
                    combinedMesh.IndicesInternal = [.. indices];

                    combinedMesh.UpdateBounds();

                    if (combinedMesh.bounds.size != Vector3.Zero)
                    {
                        combinedMeshBounds.Add(combinedMesh.bounds.min);
                        combinedMeshBounds.Add(combinedMesh.bounds.max);
                    }

                    combine.combinedMeshes.Add(combinedMesh);
                    combine.combinedMaterials.Add(material);

                    var child = Entity.Create(out Transform subTransform, out MeshRenderer subRenderer);

                    subTransform.SetParent(combine.Transform);

                    subRenderer.mesh = combinedMesh;
                    subRenderer.materials = [material];

                    child.HierarchyVisibility = EntityHierarchyVisibility.HideAndDontSave;
                }

                if (combinedMeshBounds.Count > 0)
                {
                    combine.combinedMeshBounds = AABB.CreateFromPoints(CollectionsMarshal.AsSpan(combinedMeshBounds));
                }

                switch (combine.childMode)
                {
                    case MeshCombine.MeshCombineChildMode.DestroyRenderers:

                        foreach (var renderer in combine.renderers.Contents)
                        {
                            renderer.Entity.RemoveComponent<MeshRenderer>();
                        }

                        break;

                    case MeshCombine.MeshCombineChildMode.DisableRendering:

                        foreach (var renderer in combine.renderers.Contents)
                        {
                            renderer.Enabled = false;
                        }

                        break;
                }
            }
        }
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
    }

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
    }

    public override void Submit()
    {
    }
}
