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

    public MeshCombineSystem() : base(false, typeof(MeshCombine), typeof(GenericRenderQueue<MeshCombine>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<MeshCombine>();

    #region Lifecycle
    public override void Startup()
    {
    }

    public override void Shutdown()
    {
    }
    #endregion

    public override void Prepare()
    {
        renderers.Clear();
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
        if (renderQueue is not GenericRenderQueue<MeshCombine> queue)
        {
            return;
        }

        var items = queue.Items;

        foreach (var entry in items)
        {
            var combine = entry.component;

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
                        renderer.mesh.meshAsset?.Lighting ?? renderer.lighting;

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

                        foreach (var position in mesh.VerticesInternal)
                        {
                            vertices.Add(position.Transformed(matrix));
                        }

                        if((mesh.NormalsInternal?.Length ?? 0) > 0)
                        {
                            Matrix4x4.Invert(matrix, out var normalMatrix);

                            normalMatrix = Matrix4x4.Transpose(normalMatrix);

                            foreach(var normal in mesh.NormalsInternal)
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

                    combinedMesh.VerticesInternal = [..vertices];
                    combinedMesh.NormalsInternal = [..normals];
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

                    combine.combinedMeshes.Add((combinedMesh, pair.Key.Item3));
                    combine.combinedMaterials.Add(material);
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

            if (!transformVersions.ShouldUpdateComponent(entry.entity, in entry.transform))
            {
                continue;
            }
            
            var localSize = Vector3.Abs(combine.combinedMeshBounds.size.Transformed(entry.transform.LocalRotation));

            var globalSize = Vector3.Abs(combine.combinedMeshBounds.size.Transformed(entry.transform.Rotation));

            combine.localBounds = new(entry.transform.LocalPosition +
                combine.combinedMeshBounds.center.Transformed(entry.transform.LocalRotation) * entry.transform.LocalScale,
                localSize * entry.transform.LocalScale);

            combine.UpdateBounds(new(entry.transform.Position +
                combine.combinedMeshBounds.center.Transformed(entry.transform.Rotation) * entry.transform.Scale,
                globalSize * entry.transform.Scale));
        }
    }

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
        if (renderQueue is not GenericRenderQueue<MeshCombine> queue)
        {
            return;
        }

        var items = queue.Items;

        foreach (var entry in items)
        {
            var combine = entry.component;

            if (combine.combinedMeshes.Count == 0 ||
                combine.combinedMaterials.Count != combine.combinedMeshes.Count)
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

    public override void Submit()
    {
        Material lastMaterial = null;

        var lastLighting = MaterialLighting.Unlit;
        var lastTopology = MeshTopology.Triangles;

        var l = renderers.Length;

        for (var i = 0; i < l; i++)
        {
            var item = renderers.Contents[i];

            var renderer = item.renderer;

            var meshCount = renderer.combinedMeshes.Count;

            for(var j = 0; j < meshCount; j++)
            {
                var (mesh, lighting) = renderer.combinedMeshes[j];
                var material = renderer.combinedMaterials[j];

                var needsChange = material.StateHash != (lastMaterial?.StateHash ?? 0) ||
                    lastLighting != lighting ||
                    lastTopology != mesh.MeshTopology;

                void SetupMaterial()
                {
                    material.DisableShaderKeyword(Shader.SkinningKeyword);

                    LightSystem.Instance.ApplyMaterialLighting(material, lighting);
                }

                var renderState = RenderState.Default;

                renderState.cull = material.CullingMode;
                renderState.primitiveType = mesh.MeshTopology;
                renderState.indexBuffer = mesh.indexBuffer;
                renderState.vertexBuffer = mesh.vertexBuffer;
                renderState.indexCount = mesh.IndexCount;
                renderState.world = item.transform.Matrix;

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

                LightSystem.Instance.ApplyLightProperties(material, RenderSystem.CurrentCamera.transform.Position, lighting);

                RenderSystem.Submit(renderState, mesh.SubmeshTriangleCount(0), 1);
            }
        }
    }
}
