using MeshOptimizer;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Tooling;

public static class MeshOptimization
{
    public const int TargetTriangleCountHigh = 400000;
    public const int TargetTriangleCountNormal = 120000;
    public const int TargetTriangleCountLow = 40000;

    private struct PlaceholderVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 tangent;
        public Vector3 bitangent;
        public Vector4 color;
        public Vector4 color2;
        public Vector4 color3;
        public Vector4 color4;
        public Vector2 uv1;
        public Vector2 uv2;
        public Vector2 uv3;
        public Vector2 uv4;
        public Vector2 uv5;
        public Vector2 uv6;
        public Vector2 uv7;
        public Vector2 uv8;
        public Vector4 boneIndices;
        public Vector4 boneWeights;
    }

    public static MeshAssetMeshInfo SimplifyMesh(MeshAssetMeshInfo mesh, MeshSimplifyTarget target, int customPolyCount)
    {
        unsafe
        {
            if(mesh.topology != MeshTopology.Triangles ||
                mesh.vertices.Length == 0)
            {
                return mesh;
            }

            var newMesh = new MeshAssetMeshInfo()
            {
                bones = mesh.bones,
                boundsCenter = mesh.boundsCenter,
                boundsExtents = mesh.boundsExtents,
                materialGuid = mesh.materialGuid,
                name = mesh.name,
                topology = mesh.topology,
                type = mesh.type,
            };

            var tempVertices = new PlaceholderVertex[mesh.vertices.Length];

            var hasNormals = (mesh.normals?.Length ?? 0) == tempVertices.Length;
            var hasTangents = (mesh.tangents?.Length ?? 0) == tempVertices.Length;
            var hasBitangents = (mesh.bitangents?.Length ?? 0) == tempVertices.Length;
            var hasColors = (mesh.colors?.Length ?? 0) == tempVertices.Length;
            var hasColors2 = (mesh.colors2?.Length ?? 0) == tempVertices.Length;
            var hasColors3 = (mesh.colors3?.Length ?? 0) == tempVertices.Length;
            var hasColors4 = (mesh.colors4?.Length ?? 0) == tempVertices.Length;
            var hasUV1 = (mesh.UV1?.Length ?? 0) == tempVertices.Length;
            var hasUV2 = (mesh.UV2?.Length ?? 0) == tempVertices.Length;
            var hasUV3 = (mesh.UV3?.Length ?? 0) == tempVertices.Length;
            var hasUV4 = (mesh.UV4?.Length ?? 0) == tempVertices.Length;
            var hasUV5 = (mesh.UV5?.Length ?? 0) == tempVertices.Length;
            var hasUV6 = (mesh.UV6?.Length ?? 0) == tempVertices.Length;
            var hasUV7 = (mesh.UV7?.Length ?? 0) == tempVertices.Length;
            var hasUV8 = (mesh.UV8?.Length ?? 0) == tempVertices.Length;
            var hasBoneIndices = (mesh.boneIndices?.Length ?? 0) == tempVertices.Length;
            var hasBoneWeights = (mesh.boneWeights?.Length ?? 0) == tempVertices.Length;

            for (var i = 0; i < tempVertices.Length; i++)
            {
                tempVertices[i].position = mesh.vertices[i].ToVector3();

                if (hasNormals)
                {
                    tempVertices[i].normal = mesh.normals[i].ToVector3();
                }

                if (hasTangents)
                {
                    tempVertices[i].tangent = mesh.tangents[i].ToVector3();
                }

                if (hasBitangents)
                {
                    tempVertices[i].bitangent = mesh.bitangents[i].ToVector3();
                }

                if (hasColors)
                {
                    tempVertices[i].color = mesh.colors[i].ToVector4();
                }

                if (hasColors2)
                {
                    tempVertices[i].color2 = mesh.colors2[i].ToVector4();
                }

                if (hasColors3)
                {
                    tempVertices[i].color3 = mesh.colors3[i].ToVector4();
                }

                if (hasColors4)
                {
                    tempVertices[i].color4 = mesh.colors4[i].ToVector4();
                }

                if (hasUV1)
                {
                    tempVertices[i].uv1 = mesh.UV1[i].ToVector2();
                }

                if (hasUV2)
                {
                    tempVertices[i].uv2 = mesh.UV2[i].ToVector2();
                }

                if (hasUV3)
                {
                    tempVertices[i].uv3 = mesh.UV3[i].ToVector2();
                }

                if (hasUV4)
                {
                    tempVertices[i].uv4 = mesh.UV4[i].ToVector2();
                }

                if (hasUV5)
                {
                    tempVertices[i].uv5 = mesh.UV5[i].ToVector2();
                }

                if (hasUV6)
                {
                    tempVertices[i].uv6 = mesh.UV6[i].ToVector2();
                }

                if (hasUV7)
                {
                    tempVertices[i].uv7 = mesh.UV7[i].ToVector2();
                }

                if (hasUV8)
                {
                    tempVertices[i].uv8 = mesh.UV8[i].ToVector2();
                }

                if (hasBoneIndices)
                {
                    tempVertices[i].boneIndices = mesh.boneIndices[i].ToVector4();
                }

                if (hasBoneWeights)
                {
                    tempVertices[i].boneWeights = mesh.boneWeights[i].ToVector4();
                }
            }

            var remap = new uint[mesh.indices.Length];

            var originalIndices = mesh.indices.Select(x => (uint)x).ToArray();

            var newIndexCount = target switch
            {
                MeshSimplifyTarget.Lowpoly => TargetTriangleCountLow * 3,
                MeshSimplifyTarget.Normal => TargetTriangleCountNormal * 3,
                MeshSimplifyTarget.Highpoly => TargetTriangleCountHigh * 3,
                MeshSimplifyTarget.CustomPolyCount => customPolyCount * 3,
                _ => originalIndices.Length,
            };

            if (newIndexCount > originalIndices.Length)
            {
                newIndexCount = originalIndices.Length;
            }

            if (newIndexCount > 0 && newIndexCount != originalIndices.Length)
            {
                var error = 1e-2f;

                var resultError = 0.0f;

                var simplifiedIndices = new uint[mesh.indices.Length];

                fixed (float* vptr = &tempVertices[0].position.X)
                {
                    fixed (uint* si = simplifiedIndices)
                    {
                        fixed (uint* ni = originalIndices)
                        {
                            var simplifiedIndexCount = Meshopt.Simplify(si, ni, (nuint)originalIndices.Length,
                                vptr, (nuint)mesh.vertices.Length, (nuint)sizeof(PlaceholderVertex), (nuint)newIndexCount, error,
                                SimplificationOptions.None, &resultError);

                            originalIndices = simplifiedIndices.Take((int)simplifiedIndexCount).ToArray();
                        }
                    }
                }
            }

            var vertexCount = Meshopt.GenerateVertexRemap(remap.AsSpan(),
                new ReadOnlySpan<uint>(originalIndices),
                new ReadOnlySpan<PlaceholderVertex>(tempVertices));

            var newVertices = new PlaceholderVertex[vertexCount];

            var newIndices = new uint[originalIndices.Length];

            Meshopt.RemapIndexBuffer(newIndices.AsSpan(), new ReadOnlySpan<uint>(originalIndices), new ReadOnlySpan<uint>(remap));

            Meshopt.RemapVertexBuffer(newVertices.AsSpan(), tempVertices, remap);

            Meshopt.OptimizeVertexCache(newIndices.AsSpan(), new ReadOnlySpan<uint>(newIndices), vertexCount);

            fixed (float* vptr = &newVertices[0].position.X)
            {
                fixed (uint* ni = newIndices)
                {
                    Meshopt.OptimizeOverdraw(ni, ni, (nuint)newIndices.Length, vptr, vertexCount, (nuint)sizeof(PlaceholderVertex), 1.05f);
                }
            }

            fixed (void* nv = newVertices)
            {
                fixed (uint* ni = newIndices)
                {
                    vertexCount = Meshopt.OptimizeVertexFetch(nv, ni, (nuint)newIndices.Length, nv, vertexCount,
                        (nuint)sizeof(PlaceholderVertex));

                    newVertices = newVertices.Take((int)vertexCount).ToArray();
                }
            }

            newMesh.indices = new int[newIndices.Length];

            for(var i = 0; i < newIndices.Length; i++)
            {
                newMesh.indices[i] = (int)newIndices[i];
            }

            newMesh.vertices = new Vector3Holder[vertexCount];

            if (hasNormals)
            {
                newMesh.normals = new Vector3Holder[vertexCount];
            }

            if (hasTangents)
            {
                newMesh.tangents = new Vector3Holder[vertexCount];
            }

            if (hasBitangents)
            {
                newMesh.bitangents = new Vector3Holder[vertexCount];
            }

            if (hasColors)
            {
                newMesh.colors = new Vector4Holder[vertexCount];
            }

            if (hasColors2)
            {
                newMesh.colors2 = new Vector4Holder[vertexCount];
            }

            if (hasColors3)
            {
                newMesh.colors3 = new Vector4Holder[vertexCount];
            }

            if (hasColors4)
            {
                newMesh.colors4 = new Vector4Holder[vertexCount];
            }

            if (hasUV1)
            {
                newMesh.UV1 = new Vector2Holder[vertexCount];
            }

            if (hasUV2)
            {
                newMesh.UV2 = new Vector2Holder[vertexCount];
            }

            if (hasUV3)
            {
                newMesh.UV3 = new Vector2Holder[vertexCount];
            }

            if (hasUV4)
            {
                newMesh.UV4 = new Vector2Holder[vertexCount];
            }

            if (hasUV5)
            {
                newMesh.UV5 = new Vector2Holder[vertexCount];
            }

            if (hasUV6)
            {
                newMesh.UV6 = new Vector2Holder[vertexCount];
            }

            if (hasUV7)
            {
                newMesh.UV7 = new Vector2Holder[vertexCount];
            }

            if (hasUV8)
            {
                newMesh.UV8 = new Vector2Holder[vertexCount];
            }

            if (hasBoneIndices)
            {
                newMesh.boneIndices = new Vector4Holder[vertexCount];
            }

            if (hasBoneWeights)
            {
                newMesh.boneWeights = new Vector4Holder[vertexCount];
            }

            for (var i = 0; i < (int)vertexCount; i++)
            {
                var v = newVertices[i];

                newMesh.vertices[i] = new(v.position);

                if (hasNormals)
                {
                    newMesh.normals[i] = new(v.normal);
                }

                if (hasTangents)
                {
                    newMesh.tangents[i] = new(v.tangent);
                }

                if (hasBitangents)
                {
                    newMesh.bitangents[i] = new(v.bitangent);
                }

                if (hasColors)
                {
                    newMesh.colors[i] = new(v.color);
                }

                if (hasColors2)
                {
                    newMesh.colors2[i] = new(v.color2);
                }

                if (hasColors3)
                {
                    newMesh.colors3[i] = new(v.color3);
                }

                if (hasColors4)
                {
                    newMesh.colors4[i] = new(v.color4);
                }

                if (hasUV1)
                {
                    newMesh.UV1[i] = new(v.uv1);
                }

                if (hasUV2)
                {
                    newMesh.UV2[i] = new(v.uv2);
                }

                if (hasUV3)
                {
                    newMesh.UV3[i] = new(v.uv3);
                }

                if (hasUV4)
                {
                    newMesh.UV4[i] = new(v.uv4);
                }

                if (hasUV5)
                {
                    newMesh.UV5[i] = new(v.uv5);
                }

                if (hasUV6)
                {
                    newMesh.UV6[i] = new(v.uv6);
                }

                if (hasUV7)
                {
                    newMesh.UV7[i] = new(v.uv7);
                }

                if (hasUV8)
                {
                    newMesh.UV8[i] = new(v.uv8);
                }

                if (hasBoneIndices)
                {
                    newMesh.boneIndices[i] = new(v.boneIndices);
                }

                if (hasBoneWeights)
                {
                    newMesh.boneWeights[i] = new(v.boneWeights);
                }
            }

            return newMesh;
        }
    }

    public static SerializableMeshAsset OptimizeMeshAsset(SerializableMeshAsset meshAsset)
    {
        unsafe
        {
            var outValue = new SerializableMeshAsset()
            {
                animations = meshAsset.animations,
                metadata = meshAsset.metadata,
                nodes = meshAsset.nodes,
            };

            var meshes = new List<MeshAssetMeshInfo>();

            foreach (var mesh in meshAsset.meshes)
            {
                if(mesh.topology != MeshTopology.Triangles ||
                    mesh.vertices.Length == 0)
                {
                    meshes.Add(mesh);

                    continue;
                }

                var newMesh = new MeshAssetMeshInfo()
                {
                    bones = mesh.bones,
                    boundsCenter = mesh.boundsCenter,
                    boundsExtents = mesh.boundsExtents,
                    materialGuid = mesh.materialGuid,
                    name = mesh.name,
                    topology = mesh.topology,
                    type = mesh.type,
                };

                meshes.Add(newMesh);

                var tempVertices = new PlaceholderVertex[mesh.vertices.Length];

                var hasNormals = (mesh.normals?.Length ?? 0) == tempVertices.Length;
                var hasTangents = (mesh.tangents?.Length ?? 0) == tempVertices.Length;
                var hasBitangents = (mesh.bitangents?.Length ?? 0) == tempVertices.Length;
                var hasColors = (mesh.colors?.Length ?? 0) == tempVertices.Length;
                var hasColors2 = (mesh.colors2?.Length ?? 0) == tempVertices.Length;
                var hasColors3 = (mesh.colors3?.Length ?? 0) == tempVertices.Length;
                var hasColors4 = (mesh.colors4?.Length ?? 0) == tempVertices.Length;
                var hasUV1 = (mesh.UV1?.Length ?? 0) == tempVertices.Length;
                var hasUV2 = (mesh.UV2?.Length ?? 0) == tempVertices.Length;
                var hasUV3 = (mesh.UV3?.Length ?? 0) == tempVertices.Length;
                var hasUV4 = (mesh.UV4?.Length ?? 0) == tempVertices.Length;
                var hasUV5 = (mesh.UV5?.Length ?? 0) == tempVertices.Length;
                var hasUV6 = (mesh.UV6?.Length ?? 0) == tempVertices.Length;
                var hasUV7 = (mesh.UV7?.Length ?? 0) == tempVertices.Length;
                var hasUV8 = (mesh.UV8?.Length ?? 0) == tempVertices.Length;
                var hasBoneIndices = (mesh.boneIndices?.Length ?? 0) == tempVertices.Length;
                var hasBoneWeights = (mesh.boneWeights?.Length ?? 0) == tempVertices.Length;

                for (var i = 0; i < tempVertices.Length; i++)
                {
                    tempVertices[i].position = mesh.vertices[i].ToVector3();

                    if (hasNormals)
                    {
                        tempVertices[i].normal = mesh.normals[i].ToVector3();
                    }

                    if (hasTangents)
                    {
                        tempVertices[i].tangent = mesh.tangents[i].ToVector3();
                    }

                    if (hasBitangents)
                    {
                        tempVertices[i].bitangent = mesh.bitangents[i].ToVector3();
                    }

                    if (hasColors)
                    {
                        tempVertices[i].color = mesh.colors[i].ToVector4();
                    }

                    if (hasColors2)
                    {
                        tempVertices[i].color2 = mesh.colors2[i].ToVector4();
                    }

                    if (hasColors3)
                    {
                        tempVertices[i].color3 = mesh.colors3[i].ToVector4();
                    }

                    if (hasColors4)
                    {
                        tempVertices[i].color4 = mesh.colors4[i].ToVector4();
                    }

                    if (hasUV1)
                    {
                        tempVertices[i].uv1 = mesh.UV1[i].ToVector2();
                    }

                    if (hasUV2)
                    {
                        tempVertices[i].uv2 = mesh.UV2[i].ToVector2();
                    }

                    if (hasUV3)
                    {
                        tempVertices[i].uv3 = mesh.UV3[i].ToVector2();
                    }

                    if (hasUV4)
                    {
                        tempVertices[i].uv4 = mesh.UV4[i].ToVector2();
                    }

                    if (hasUV5)
                    {
                        tempVertices[i].uv5 = mesh.UV5[i].ToVector2();
                    }

                    if (hasUV6)
                    {
                        tempVertices[i].uv6 = mesh.UV6[i].ToVector2();
                    }

                    if (hasUV7)
                    {
                        tempVertices[i].uv7 = mesh.UV7[i].ToVector2();
                    }

                    if (hasUV8)
                    {
                        tempVertices[i].uv8 = mesh.UV8[i].ToVector2();
                    }

                    if (hasBoneIndices)
                    {
                        tempVertices[i].boneIndices = mesh.boneIndices[i].ToVector4();
                    }

                    if (hasBoneWeights)
                    {
                        tempVertices[i].boneWeights = mesh.boneWeights[i].ToVector4();
                    }
                }

                var remap = new uint[mesh.indices.Length];

                var originalIndices = mesh.indices.Select(x => (uint)x).ToArray();

                var newIndexCount = meshAsset.metadata.simplify switch
                {
                    MeshSimplifyTarget.Lowpoly => TargetTriangleCountLow * 3,
                    MeshSimplifyTarget.Normal => TargetTriangleCountNormal * 3,
                    MeshSimplifyTarget.Highpoly => TargetTriangleCountHigh * 3,
                    MeshSimplifyTarget.CustomPolyCount => meshAsset.metadata.targetPolyCount * 3,
                    _ => originalIndices.Length,
                };

                if(newIndexCount > originalIndices.Length)
                {
                    newIndexCount = originalIndices.Length;
                }

                if (newIndexCount > 0 && newIndexCount != originalIndices.Length)
                {
                    var error = 1e-2f;

                    var resultError = 0.0f;

                    var simplifiedIndices = new uint[mesh.indices.Length];

                    fixed (float* vptr = &tempVertices[0].position.X)
                    {
                        fixed (uint* si = simplifiedIndices)
                        {
                            fixed (uint* ni = originalIndices)
                            {
                                var simplifiedIndexCount = Meshopt.Simplify(si, ni, (nuint)originalIndices.Length,
                                    vptr, (nuint)mesh.vertices.Length, (nuint)sizeof(PlaceholderVertex), (nuint)newIndexCount, error,
                                    SimplificationOptions.None, &resultError);

                                originalIndices = simplifiedIndices.Take((int)simplifiedIndexCount).ToArray();
                            }
                        }
                    }
                }

                var vertexCount = Meshopt.GenerateVertexRemap(remap.AsSpan(),
                    new ReadOnlySpan<uint>(originalIndices),
                    new ReadOnlySpan<PlaceholderVertex>(tempVertices));

                var newVertices = new PlaceholderVertex[vertexCount];

                var newIndices = new uint[originalIndices.Length];

                Meshopt.RemapIndexBuffer(newIndices.AsSpan(), new ReadOnlySpan<uint>(originalIndices), new ReadOnlySpan<uint>(remap));

                Meshopt.RemapVertexBuffer(newVertices.AsSpan(), tempVertices, remap);

                Meshopt.OptimizeVertexCache(newIndices.AsSpan(), new ReadOnlySpan<uint>(newIndices), vertexCount);

                fixed (float* vptr = &newVertices[0].position.X)
                {
                    fixed (uint* ni = newIndices)
                    {
                        Meshopt.OptimizeOverdraw(ni, ni, (nuint)newIndices.Length, vptr, vertexCount, (nuint)sizeof(PlaceholderVertex), 1.05f);
                    }
                }

                fixed (void* nv = newVertices)
                {
                    fixed (uint* ni = newIndices)
                    {
                        vertexCount = Meshopt.OptimizeVertexFetch(nv, ni, (nuint)newIndices.Length, nv, vertexCount,
                            (nuint)sizeof(PlaceholderVertex));

                        newVertices = newVertices.Take((int)vertexCount).ToArray();
                    }
                }

                newMesh.indices = new int[newIndices.Length];

                for (var i = 0; i < newIndices.Length; i++)
                {
                    newMesh.indices[i] = (int)newIndices[i];
                }

                newMesh.vertices = new Vector3Holder[vertexCount];

                if (hasNormals)
                {
                    newMesh.normals = new Vector3Holder[vertexCount];
                }

                if (hasTangents)
                {
                    newMesh.tangents = new Vector3Holder[vertexCount];
                }

                if (hasBitangents)
                {
                    newMesh.bitangents = new Vector3Holder[vertexCount];
                }

                if (hasColors)
                {
                    newMesh.colors = new Vector4Holder[vertexCount];
                }

                if (hasColors2)
                {
                    newMesh.colors2 = new Vector4Holder[vertexCount];
                }

                if (hasColors3)
                {
                    newMesh.colors3 = new Vector4Holder[vertexCount];
                }

                if (hasColors4)
                {
                    newMesh.colors4 = new Vector4Holder[vertexCount];
                }

                if (hasUV1)
                {
                    newMesh.UV1 = new Vector2Holder[vertexCount];
                }

                if (hasUV2)
                {
                    newMesh.UV2 = new Vector2Holder[vertexCount];
                }

                if (hasUV3)
                {
                    newMesh.UV3 = new Vector2Holder[vertexCount];
                }

                if (hasUV4)
                {
                    newMesh.UV4 = new Vector2Holder[vertexCount];
                }

                if (hasUV5)
                {
                    newMesh.UV5 = new Vector2Holder[vertexCount];
                }

                if (hasUV6)
                {
                    newMesh.UV6 = new Vector2Holder[vertexCount];
                }

                if (hasUV7)
                {
                    newMesh.UV7 = new Vector2Holder[vertexCount];
                }

                if (hasUV8)
                {
                    newMesh.UV8 = new Vector2Holder[vertexCount];
                }

                if (hasBoneIndices)
                {
                    newMesh.boneIndices = new Vector4Holder[vertexCount];
                }

                if (hasBoneWeights)
                {
                    newMesh.boneWeights = new Vector4Holder[vertexCount];
                }

                for (var i = 0; i < (int)vertexCount; i++)
                {
                    var v = newVertices[i];

                    newMesh.vertices[i] = new(v.position);

                    if (hasNormals)
                    {
                        newMesh.normals[i] = new(v.normal);
                    }

                    if (hasTangents)
                    {
                        newMesh.tangents[i] = new(v.tangent);
                    }

                    if (hasBitangents)
                    {
                        newMesh.bitangents[i] = new(v.bitangent);
                    }

                    if (hasColors)
                    {
                        newMesh.colors[i] = new(v.color);
                    }

                    if (hasColors2)
                    {
                        newMesh.colors2[i] = new(v.color2);
                    }

                    if (hasColors3)
                    {
                        newMesh.colors3[i] = new(v.color3);
                    }

                    if (hasColors4)
                    {
                        newMesh.colors4[i] = new(v.color4);
                    }

                    if (hasUV1)
                    {
                        newMesh.UV1[i] = new(v.uv1);
                    }

                    if (hasUV2)
                    {
                        newMesh.UV2[i] = new(v.uv2);
                    }

                    if (hasUV3)
                    {
                        newMesh.UV3[i] = new(v.uv3);
                    }

                    if (hasUV4)
                    {
                        newMesh.UV4[i] = new(v.uv4);
                    }

                    if (hasUV5)
                    {
                        newMesh.UV5[i] = new(v.uv5);
                    }

                    if (hasUV6)
                    {
                        newMesh.UV6[i] = new(v.uv6);
                    }

                    if (hasUV7)
                    {
                        newMesh.UV7[i] = new(v.uv7);
                    }

                    if (hasUV8)
                    {
                        newMesh.UV8[i] = new(v.uv8);
                    }

                    if (hasBoneIndices)
                    {
                        newMesh.boneIndices[i] = new(v.boneIndices);
                    }

                    if (hasBoneWeights)
                    {
                        newMesh.boneWeights[i] = new(v.boneWeights);
                    }
                }
            }

            outValue.meshes = meshes.ToArray();

            return outValue;
        }
    }
}
