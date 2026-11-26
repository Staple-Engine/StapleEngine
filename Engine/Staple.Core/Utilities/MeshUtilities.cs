using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple;

public static class MeshUtilities
{
    private readonly struct CombineMeshKey(MeshAssetComponent components, MeshTopology topology, Material material) : IEquatable<CombineMeshKey>
    {
        public readonly MeshAssetComponent components = components;
        public readonly MeshTopology topology = topology;
        public readonly int materialHash = material.Guid.GuidHash;
        public readonly Material material = material;

        public static bool operator ==(CombineMeshKey lhs, CombineMeshKey rhs)
        {
            return lhs.components == rhs.components &&
                lhs.topology == rhs.topology &&
                lhs.materialHash == rhs.materialHash;
        }

        public static bool operator !=(CombineMeshKey lhs, CombineMeshKey rhs)
        {
            return lhs.components != rhs.components ||
                lhs.topology != rhs.topology ||
                lhs.materialHash != rhs.materialHash;
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj is not CombineMeshKey key)
            {
                return false;
            }

            return this == key;
        }

        public bool Equals(CombineMeshKey key)
        {
            return components == key.components && topology == key.topology && materialHash == key.materialHash;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(components, topology, materialHash);
        }

        public override string ToString()
        {
            return $"{components} {topology} {AssetDatabase.GetAssetName(material.Guid.Guid)} {materialHash}";
        }
    }

    private readonly struct CombineMeshItem(Mesh mesh, int submeshIndex, Transform transform, Material material)
    {
        public readonly Mesh mesh = mesh;
        public readonly int submeshIndex = submeshIndex;
        public readonly Transform transform = transform;
        public readonly Material material = material;
    }

    public class CombinedMeshData
    {
        public readonly List<Mesh> combinedMeshes = [];
        public readonly List<Material> combinedMaterials = [];
        public AABB combinedMeshBounds;

        public CombinedMeshAsset ToAsset()
        {
            var outValue = new CombinedMeshAsset()
            {
                combinedBoundsCenter = combinedMeshBounds.center,
                combinedBoundsSize = combinedMeshBounds.size,
            };

            if (combinedMeshes.Count != combinedMaterials.Count ||
                combinedMeshes.Count == 0)
            {
                return null;
            }

            for (var i = 0; i < combinedMeshes.Count; i++)
            {
                var mesh = combinedMeshes[i];
                var material = combinedMaterials[i];

                var components = mesh.Components;

                var entry = new CombinedMeshAsset.MeshData()
                {
                    materialGuid = material.Guid.Guid,
                    vertices = mesh.VerticesInternal,
                    indices = mesh.IndicesInternal,
                };

                if(components.HasFlag(MeshAssetComponent.UV1))
                {
                    entry.uv = mesh.UVInternal;
                }

                if (components.HasFlag(MeshAssetComponent.UV2))
                {
                    entry.uv2 = mesh.UV2Internal;
                }

                if (components.HasFlag(MeshAssetComponent.UV3))
                {
                    entry.uv3 = mesh.UV3Internal;
                }

                if (components.HasFlag(MeshAssetComponent.UV4))
                {
                    entry.uv4 = mesh.UV4Internal;
                }

                if (components.HasFlag(MeshAssetComponent.UV5))
                {
                    entry.uv5 = mesh.UV5Internal;
                }

                if (components.HasFlag(MeshAssetComponent.UV6))
                {
                    entry.uv6 = mesh.UV6Internal;
                }

                if (components.HasFlag(MeshAssetComponent.UV7))
                {
                    entry.uv7 = mesh.UV7Internal;
                }

                if (components.HasFlag(MeshAssetComponent.UV8))
                {
                    entry.uv8 = mesh.UV8Internal;
                }

                if (components.HasFlag(MeshAssetComponent.Normal))
                {
                    entry.normals = mesh.NormalsInternal;
                }

                if (components.HasFlag(MeshAssetComponent.Tangent))
                {
                    entry.tangents = mesh.TangentsInternal;
                }

                if (components.HasFlag(MeshAssetComponent.Bitangent))
                {
                    entry.bitangents = mesh.BitangentsInternal;
                }

                if (components.HasFlag(MeshAssetComponent.Color1))
                {
                    entry.colors = mesh.ColorsInternal;
                }

                if (components.HasFlag(MeshAssetComponent.Color2))
                {
                    entry.colors2 = mesh.Colors2Internal;
                }

                if (components.HasFlag(MeshAssetComponent.Color3))
                {
                    entry.colors3 = mesh.Colors3Internal;
                }

                if (components.HasFlag(MeshAssetComponent.Color4))
                {
                    entry.colors4 = mesh.Colors4Internal;
                }

                outValue.entries.Add(entry);
            }

            return outValue;
        }

        public static CombinedMeshData FromAsset(CombinedMeshAsset asset)
        {
            if(asset == null)
            {
                return null;
            }

            var outValue = new CombinedMeshData()
            {
                combinedMeshBounds = new(asset.combinedBoundsCenter, asset.combinedBoundsSize),
            };

            foreach(var entry in asset.entries)
            {
                var material = ResourceManager.instance.LoadMaterial(entry.materialGuid);

                if(material == null)
                {
                    continue;
                }

                var mesh = entry.ToMesh();

                outValue.combinedMeshes.Add(mesh);
                outValue.combinedMaterials.Add(material);
            }

            return outValue;
        }
    }

    public static CombinedMeshData CombineMeshes(Transform meshContainer)
    {
        var renderers = meshContainer.Entity.GetComponentsInChildren<MeshRenderer>(true);

        var outValue = new CombinedMeshData();

        var combinableMeshes = new Dictionary<CombineMeshKey, List<CombineMeshItem>>();

        foreach (var renderer in renderers)
        {
            if (!renderer.Entity.EnabledInHierarchy ||
                !renderer.Enabled ||
                renderer.forceRenderingOff ||
                renderer.mesh == null ||
                (renderer.materials?.Count ?? 0) == 0)
            {
                continue;
            }

            var components = renderer.mesh.Components;

            if (components == MeshAssetComponent.None || components.HasFlag(MeshAssetComponent.BoneIndicesWeights))
            {
                continue;
            }

            for (var i = 0; i < renderer.materials.Count; i++)
            {
                if (i > 0 && i >= renderer.mesh.submeshes.Count ||
                    !(renderer.materials[i]?.IsValid ?? false))
                {
                    continue;
                }

                var key = new CombineMeshKey(components, renderer.mesh.MeshTopology, renderer.materials[i]);

                if (!combinableMeshes.TryGetValue(key, out var container))
                {
                    container = [];

                    combinableMeshes.Add(key, container);
                }

                container.Add(new(renderer.mesh, i, renderer.Transform, renderer.materials[i]));
            }
        }

        var combinedMeshBounds = new List<Vector3>();

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

        foreach (var pair in combinableMeshes)
        {
            var combinedMesh = new Mesh(true, false)
            {
                meshTopology = pair.Key.topology,
                indexFormat = MeshIndexFormat.UInt32,
            };

            vertices.Clear();
            normals.Clear();
            tangents.Clear();
            bitangents.Clear();
            color0.Clear();
            color1.Clear();
            color2.Clear();
            color3.Clear();
            color4.Clear();
            color32.Clear();
            color322.Clear();
            color323.Clear();
            color324.Clear();
            uv0.Clear();
            uv1.Clear();
            uv2.Clear();
            uv3.Clear();
            uv4.Clear();
            uv5.Clear();
            uv6.Clear();
            uv7.Clear();
            indices.Clear();

            Material material = null;

            foreach (var item in pair.Value)
            {
                var mesh = item.mesh;
                var submeshIndex = item.submeshIndex;
                var transform = item.transform;
                material = item.material;

                var startVertex = vertices.Count;

                var matrix = transform.Matrix;

                var sliceStartVertex = 0;
                var sliceStartIndex = 0;
                var sliceIndexCount = mesh.IndicesInternal?.Length ?? 0;

                if (mesh.submeshes.Count > 0)
                {
                    var submesh = mesh.submeshes[submeshIndex];

                    sliceStartVertex = submesh.startVertex;
                    sliceStartIndex = submesh.startIndex;
                    sliceIndexCount = submesh.indexCount;
                }

                var uniqueIndices = new HashSet<int>();
                var minimumIndex = 99999;

                var sliceIndices = mesh.IndicesInternal.AsSpan(sliceStartIndex, sliceIndexCount);

                {
                    foreach (var index in sliceIndices)
                    {
                        uniqueIndices.Add(index);

                        if (index < minimumIndex)
                        {
                            minimumIndex = index;
                        }
                    }
                }

                Matrix4x4.Invert(matrix, out var normalMatrix);

                normalMatrix = Matrix4x4.Transpose(normalMatrix);

                foreach (var index in uniqueIndices)
                {
                    vertices.Add(mesh.VerticesInternal[index].Transformed(in matrix));

                    if ((mesh.NormalsInternal?.Length ?? 0) > 0)
                    {
                        normals.Add(Vector3.TransformNormal(mesh.NormalsInternal[index], normalMatrix));
                    }

                    void AddIfValid<T>(List<T> target, T[] source)
                    {
                        if ((source?.Length ?? 0) == 0)
                        {
                            return;
                        }

                        target.Add(source[index]);
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
                }

                foreach (var index in sliceIndices)
                {
                    indices.Add(index - minimumIndex + startVertex);
                }
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

            outValue.combinedMeshes.Add(combinedMesh);
            outValue.combinedMaterials.Add(material);
        }

        if (combinedMeshBounds.Count > 0)
        {
            outValue.combinedMeshBounds = AABB.CreateFromPoints(CollectionsMarshal.AsSpan(combinedMeshBounds));
        }

        return outValue;
    }

    public static bool InstanceCombinedMesh(CombinedMeshData data, Transform target)
    {
        if(data == null ||
            (data.combinedMeshes?.Count ?? 0) == 0 ||
            (data.combinedMeshes?.Count ?? 0) != (data.combinedMaterials?.Count ?? 0))
        {
            return false;
        }

        for(var i = 0; i < data.combinedMeshes.Count; i++)
        {
            var child = Entity.Create(out Transform subTransform, out MeshRenderer subRenderer);

            subTransform.SetParent(target);

            subRenderer.mesh = data.combinedMeshes[i];
            subRenderer.materials = [data.combinedMaterials[i]];

            child.HierarchyVisibility = EntityHierarchyVisibility.HideAndDontSave;
        }

        return true;
    }
}
