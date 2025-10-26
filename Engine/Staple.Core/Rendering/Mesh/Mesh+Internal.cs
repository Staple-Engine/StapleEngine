using Staple.Internal;
using Staple.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple;

public sealed partial class Mesh
{
    /// <summary>
    /// Contains info for a submesh
    /// </summary>
    internal class SubmeshInfo
    {
        /// <summary>
        /// The vertex to start rendering from
        /// </summary>
        public int startVertex;

        /// <summary>
        /// How many vertices to render
        /// </summary>
        public int vertexCount;

        /// <summary>
        /// The index to start rendering from
        /// </summary>
        public int startIndex;

        /// <summary>
        /// How many indices to render
        /// </summary>
        public int indexCount;

        /// <summary>
        /// The topology of the mesh
        /// </summary>
        public MeshTopology topology;
    }

    /// <summary>
    /// Whether the mesh was changed
    /// </summary>
    internal bool changed;

    /// <summary>
    /// Whether this mesh is dynamic
    /// </summary>
    internal bool isDynamic = false;

    /// <summary>
    /// List of vertices. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector3[] vertices;

    /// <summary>
    /// List of normals. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector3[] normals;

    /// <summary>
    /// List of tangents. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector3[] tangents;

    /// <summary>
    /// List of bitangents. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector3[] bitangents;

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color[] colors;

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color[] colors2;

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color[] colors3;

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color[] colors4;

    /// <summary>
    /// List of colors (byte version). This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color32[] colors32;

    /// <summary>
    /// List of colors (byte version). This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color32[] colors322;

    /// <summary>
    /// List of colors (byte version). This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color32[] colors323;

    /// <summary>
    /// List of colors (byte version). This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color32[] colors324;

    /// <summary>
    /// List of UVs in the first channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] uv;

    /// <summary>
    /// List of UVs in the second channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] uv2;

    /// <summary>
    /// List of UVs in the third channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] uv3;

    /// <summary>
    /// List of UVs in the fourth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] uv4;

    /// <summary>
    /// List of UVs in the fifth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] uv5;

    /// <summary>
    /// List of UVs in the sixth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] uv6;

    /// <summary>
    /// List of UVs in the seventh channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] uv7;

    /// <summary>
    /// List of UVs in the eighth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] uv8;

    /// <summary>
    /// List of indices
    /// </summary>
    internal int[] indices;

    /// <summary>
    /// List of bone indices. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector4[] boneIndices;

    /// <summary>
    /// List of bone weights. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector4[] boneWeights;

    /// <summary>
    /// The index format of the mesh
    /// </summary>
    internal MeshIndexFormat indexFormat = MeshIndexFormat.UInt16;

    /// <summary>
    /// The topology (geometry type) of the mesh
    /// </summary>
    internal MeshTopology meshTopology = MeshTopology.TriangleStrip;

    /// <summary>
    /// Internal vertex buffer
    /// </summary>
    internal VertexBuffer vertexBuffer;

    /// <summary>
    /// Internal index buffer
    /// </summary>
    internal IndexBuffer indexBuffer;

    /// <summary>
    /// Contains the last received mesh data blob. Received from SetMeshData.
    /// </summary>
    internal byte[] meshDataBlob = null;

    /// <summary>
    /// The vertex layout of this mesh from the mesh data
    /// </summary>
    internal VertexLayout meshDataVertexLayout = null;

    /// <summary>
    /// The mesh asset this mesh belongs to, if any.
    /// </summary>
    internal MeshAsset meshAsset;

    /// <summary>
    /// The mesh index of the mesh asset, if any.
    /// </summary>
    internal int meshAssetIndex;

    /// <summary>
    /// A list of all submeshes in this mesh
    /// </summary>
    internal List<SubmeshInfo> submeshes = [];

    /// <summary>
    /// Gets the mesh asset mesh of this mesh
    /// </summary>
    internal MeshAsset.MeshInfo MeshAssetMesh
    {
        get
        {
            if(meshAsset == null || meshAssetIndex < 0 || meshAssetIndex >= meshAsset.meshes.Count)
            {
                return null;
            }

            return meshAsset.meshes[meshAssetIndex];
        }
    }

    /// <summary>
    /// Internal list of vertex layouts for each a unique key
    /// </summary>
    internal static Dictionary<string, VertexLayout> vertexLayouts = [];

    internal bool HasNormals => (normals?.Length ?? 0) > 0;

    internal bool HasTangents => (tangents?.Length ?? 0) > 0;

    internal bool HasBitangents => (bitangents?.Length ?? 0) > 0;

    internal bool HasColors => (colors?.Length ?? 0) > 0;

    internal bool HasColors2 => (colors2?.Length ?? 0) > 0;

    internal bool HasColors3 => (colors3?.Length ?? 0) > 0;

    internal bool HasColors4 => (colors4?.Length ?? 0) > 0;

    internal bool HasColors32 => (colors32?.Length ?? 0) > 0;

    internal bool HasColors322 => (colors322?.Length ?? 0) > 0;

    internal bool HasColors323 => (colors323?.Length ?? 0) > 0;

    internal bool HasColors324 => (colors324?.Length ?? 0) > 0;

    internal bool HasUV => (uv?.Length ?? 0) > 0;

    internal bool HasUV2 => (uv2?.Length ?? 0) > 0;

    internal bool HasUV3 => (uv3?.Length ?? 0) > 0;

    internal bool HasUV4 => (uv4?.Length ?? 0) > 0;

    internal bool HasUV5 => (uv5?.Length ?? 0) > 0;

    internal bool HasUV6 => (uv6?.Length ?? 0) > 0;

    internal bool HasUV7 => (uv7?.Length ?? 0) > 0;

    internal bool HasUV8 => (uv8?.Length ?? 0) > 0;

    internal bool HasBoneIndices => (boneIndices?.Length ?? 0) > 0;

    internal bool HasBoneWeights => (boneWeights?.Length ?? 0) > 0;

    /// <summary>
    /// List of default meshes
    /// </summary>
    internal static readonly Dictionary<string, Mesh> defaultMeshes = new()
    {
        { "Internal/Quad", Quad },
        { "Internal/Cube", Cube },
        { "Internal/Sphere", Sphere },
    };

    private static Mesh _quad;

    /// <summary>
    /// Gets the quad default mesh, and generates it if it's not built yet.
    /// </summary>
    internal static Mesh Quad
    {
        get
        {
            if(_quad == null)
            {
                var builder = new CubicMeshBuilder();

                builder.QuadVertices(Vector3.Zero, 1);
                builder.CubeTexture(new(0, 1, 0, 1));
                builder.CubeFaces();

                _quad = builder.BuildMesh(true);

                _quad.colors = Enumerable.Repeat(Color.White, _quad.vertices.Length).ToArray();

                GenerateTangents(_quad.vertices.AsSpan(), _quad.uv.AsSpan(), _quad.normals.AsSpan(), _quad.indices.AsSpan(),
                    out _quad.tangents, out _quad.bitangents);

                _quad.Guid.Guid = "Internal/Quad";

                _quad.UpdateBounds();
            }

            return _quad;
        }
    }

    private static Mesh _cube;

    /// <summary>
    /// Gets the cube default mesh, and generates it if it's not built yet.
    /// </summary>
    internal static Mesh Cube
    {
        get
        {
            if(_cube == null)
            {
                var builder = new CubicMeshBuilder();

                foreach(var direction in Enum.GetValues<CubicMeshBuilder.Direction>())
                {
                    builder.CubeVertices(Vector3.Zero, 1, direction);
                    builder.CubeTexture(new RectFloat(0, 1, 0, 1));
                    builder.CubeFaces();
                }

                _cube = builder.BuildMesh(true);

                _cube.colors = Enumerable.Repeat(Color.White, _cube.vertices.Length).ToArray();

                GenerateTangents(_cube.vertices.AsSpan(), _cube.uv.AsSpan(), _cube.normals.AsSpan(), _cube.indices.AsSpan(),
                    out _cube.tangents, out _cube.bitangents);

                _cube.Guid.Guid = "Internal/Cube";

                _cube.UpdateBounds();
            }

            return _cube;
        }
    }

    private static Mesh _sphere;

    /// <summary>
    /// Gets the sphere default mesh, and generates it if it's not built yet.
    /// </summary>
    internal static Mesh Sphere
    {
        get
        {
            if (_sphere == null)
            {
                _sphere = GenerateSphere(36, 18, 0.5f, false);

                _sphere.colors = Enumerable.Repeat(Color.White, _sphere.vertices.Length).ToArray();

                GenerateTangents(_sphere.vertices.AsSpan(), _sphere.uv.AsSpan(), _sphere.normals.AsSpan(), _sphere.indices.AsSpan(),
                    out _sphere.tangents, out _sphere.bitangents);

                _sphere.Guid.Guid = "Internal/Sphere";

                _sphere.UpdateBounds();
            }

            return _sphere;
        }
    }

    /// <summary>
    /// Generates a sphere mesh
    /// </summary>
    /// <param name="sectorCount">The amount of sectors</param>
    /// <param name="stackCount">The amount of stacks</param>
    /// <param name="radius">The radius of the mesh</param>
    /// <param name="writable">Whether the sphere is writable</param>
    /// <returns>The mesh</returns>
    internal static Mesh GenerateSphere(int sectorCount, int stackCount, float radius, bool writable)
    {
        //Based on https://www.songho.ca/opengl/gl_sphere.html
        var outValue = new Mesh(false, writable)
        {
            meshTopology = MeshTopology.Triangles,
            indexFormat = MeshIndexFormat.UInt32,
            changed = true,
        };

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var texCoords = new List<Vector2>();
        var indices = new List<int>();

        var sectorStep = 2 * Math.PI / sectorCount;
        var stackStep = Math.PI / stackCount;
        var lengthInv = 1.0f / radius;

        for(var i = 0; i <= stackCount; i++)
        {
            var stackAngle = Math.PI / 2 - i * stackStep;
            var xy = radius * Math.Cos(stackAngle);
            var z = radius * Math.Sin(stackAngle);

            for(var j = 0; j <= sectorCount; j++)
            {
                var sectorAngle = j * sectorStep;

                var x = xy * Math.Cos(sectorAngle);
                var y = xy * Math.Sin(sectorAngle);

                vertices.Add(new(x, y, z));

                normals.Add(new(x * lengthInv, y * lengthInv, z * lengthInv));

                var s = (float)j / sectorCount;
                var t = (float)i / stackCount;

                texCoords.Add(new(s, t));
            }
        }

        for(var i = 0; i < stackCount; i++)
        {
            var k1 = i * (sectorCount + 1);
            var k2 = k1 + sectorCount + 1;

            for(var j = 0; j < sectorCount; j++, k1++, k2++)
            {
                if(i != 0)
                {
                    indices.Add(k1);
                    indices.Add(k2);
                    indices.Add(k1 + 1);
                }

                if (i != (stackCount - 1))
                {
                    indices.Add(k1 + 1);
                    indices.Add(k2);
                    indices.Add(k2 + 1);
                }
            }
        }

        outValue.vertices = vertices.ToArray();
        outValue.normals = normals.ToArray();
        outValue.uv = texCoords.ToArray();
        outValue.indices = indices.ToArray();

        return outValue;
    }

    /// <summary>
    /// Creates a mesh that may be readable, writable, or neither
    /// </summary>
    /// <param name="readable">Whether we can read the mesh data</param>
    /// <param name="writable">Whether we can write the mesh data</param>
    internal Mesh(bool readable, bool writable)
    {
        isReadable = readable;
        isWritable = writable;
    }

    /// <summary>
    /// Destroys this mesh's resources
    /// </summary>
    public void Destroy()
    {
        vertexBuffer?.Destroy();
        indexBuffer?.Destroy();

        vertexBuffer = null;
        indexBuffer = null;
    }

    /// <summary>
    /// Generates a vertex layout for a mesh
    /// </summary>
    /// <param name="mesh">The mesh to generate the layout for</param>
    /// <returns>The layout</returns>
    internal static VertexLayout GetVertexLayout(Mesh mesh)
    {
        var keyBuilder = new StringBuilder();

        if (mesh.HasNormals)
        {
            keyBuilder.Append('n');
        }

        if(mesh.HasTangents)
        {
            keyBuilder.Append('t');
        }

        if(mesh.HasBitangents)
        {
            keyBuilder.Append("bt");
        }

        if (mesh.HasColors)
        {
            keyBuilder.Append('c');
        }

        if (mesh.HasColors2)
        {
            keyBuilder.Append("c2");
        }

        if (mesh.HasColors3)
        {
            keyBuilder.Append("c3");
        }

        if (mesh.HasColors4)
        {
            keyBuilder.Append("c4");
        }

        if (mesh.HasColors32)
        {
            keyBuilder.Append("c32");
        }

        if (mesh.HasColors322)
        {
            keyBuilder.Append("c322");
        }

        if (mesh.HasColors323)
        {
            keyBuilder.Append("c323");
        }

        if (mesh.HasColors324)
        {
            keyBuilder.Append("c324");
        }

        if (mesh.HasUV)
        {
            keyBuilder.Append('u');
        }

        if (mesh.HasUV2)
        {
            keyBuilder.Append("u2");
        }

        if (mesh.HasUV3)
        {
            keyBuilder.Append("u3");
        }

        if (mesh.HasUV4)
        {
            keyBuilder.Append("u4");
        }

        if (mesh.HasUV5)
        {
            keyBuilder.Append("u5");
        }

        if (mesh.HasUV6)
        {
            keyBuilder.Append("u6");
        }

        if (mesh.HasUV7)
        {
            keyBuilder.Append("u7");
        }

        if (mesh.HasUV8)
        {
            keyBuilder.Append("u8");
        }

        if(mesh.HasBoneIndices)
        {
            keyBuilder.Append("bi");
        }

        if (mesh.HasBoneWeights)
        {
            keyBuilder.Append("bw");
        }

        var key = keyBuilder.ToString();

        if(vertexLayouts.TryGetValue(key, out var layout) && layout != null)
        {
            return layout;
        }

        var builder = VertexLayoutBuilder.CreateNew();

        builder.Add(VertexAttribute.Position, VertexAttributeType.Float3);

        if(mesh.HasNormals)
        {
            builder.Add(VertexAttribute.Normal, VertexAttributeType.Float3);
        }

        if (mesh.HasTangents)
        {
            builder.Add(VertexAttribute.Tangent, VertexAttributeType.Float3);
        }

        if (mesh.HasBitangents)
        {
            builder.Add(VertexAttribute.Bitangent, VertexAttributeType.Float3);
        }

        if (mesh.HasColors || mesh.HasColors32)
        {
            builder.Add(VertexAttribute.Color0, VertexAttributeType.Float4);
        }

        if (mesh.HasColors2 || mesh.HasColors322)
        {
            builder.Add(VertexAttribute.Color1, VertexAttributeType.Float4);
        }

        if (mesh.HasColors3 || mesh.HasColors323)
        {
            builder.Add(VertexAttribute.Color2, VertexAttributeType.Float4);
        }

        if (mesh.HasColors4 || mesh.HasColors324)
        {
            builder.Add(VertexAttribute.Color3, VertexAttributeType.Float4);
        }

        if (mesh.HasUV)
        {
            builder.Add(VertexAttribute.TexCoord0, VertexAttributeType.Float2);
        }

        if (mesh.HasUV2)
        {
            builder.Add(VertexAttribute.TexCoord1, VertexAttributeType.Float2);
        }

        if (mesh.HasUV3)
        {
            builder.Add(VertexAttribute.TexCoord2, VertexAttributeType.Float2);
        }

        if (mesh.HasUV4)
        {
            builder.Add(VertexAttribute.TexCoord3, VertexAttributeType.Float2);
        }

        if (mesh.HasUV5)
        {
            builder.Add(VertexAttribute.TexCoord4, VertexAttributeType.Float2);
        }

        if (mesh.HasUV6)
        {
            builder.Add(VertexAttribute.TexCoord5, VertexAttributeType.Float2);
        }

        if (mesh.HasUV7)
        {
            builder.Add(VertexAttribute.TexCoord6, VertexAttributeType.Float2);
        }

        if (mesh.HasUV8)
        {
            builder.Add(VertexAttribute.TexCoord7, VertexAttributeType.Float2);
        }

        if(mesh.HasBoneIndices)
        {
            builder.Add(VertexAttribute.BoneIndices, VertexAttributeType.Float4);
        }

        if(mesh.HasBoneWeights)
        {
            builder.Add(VertexAttribute.BoneWeight, VertexAttributeType.Float4);
        }

        layout = builder.Build();

        if(layout != null)
        {
            vertexLayouts.Add(key, layout);
        }

        return layout;
    }

    /// <summary>
    /// Generates a vertex data blob from the data sent to the mesh and a layout
    /// </summary>
    /// <param name="layout">The vertex layout</param>
    /// <returns>The byte blob, or null</returns>
    /// <exception cref="InvalidOperationException">Thrown if the mesh data is invalid</exception>
    internal byte[] MakeVertexDataBlob(VertexLayout layout)
    {
        var size = layout.Stride * vertices.Length;

        var buffer = new byte[size];

        void Copy<T>(T source, ref int index) where T: unmanaged
        {
            var sourceSize = TypeCache.SizeOf(source.GetType().ToString());

            if(index + sourceSize > buffer.Length)
            {
                throw new InvalidOperationException($"[Mesh] Buffer Overrun while generating vertex data blob: {index} -> {index + sourceSize} "
                    + $"is larger than buffer {buffer.Length}");
            }

            unsafe
            {
                byte* src = (byte*)&source;

                Marshal.Copy((nint)src, buffer, index, sourceSize);
            }

            index += sourceSize;
        }

        for (int i = 0, index = 0; i < vertices.Length; i++)
        {
            if(index % layout.Stride != 0)
            {
                throw new InvalidOperationException("[Mesh] Exceeded expected byte count while generating vertex data blob");
            }

            Copy(vertices[i], ref index);

            if(HasNormals)
            {
                Copy(normals[i], ref index);
            }

            if(HasTangents)
            {
                Copy(tangents[i], ref index);
            }

            if (HasBitangents)
            {
                Copy(bitangents[i], ref index);
            }

            if(HasColors)
            {
                Copy(colors[i], ref index);
            }
            else if(HasColors32)
            {
                var c = (Color)colors32[i];

                Copy(c, ref index);
            }

            if (HasColors2)
            {
                Copy(colors2[i], ref index);
            }
            else if (HasColors322)
            {
                var c = (Color)colors322[i];

                Copy(c, ref index);
            }

            if (HasColors3)
            {
                Copy(colors3[i], ref index);
            }
            else if (HasColors323)
            {
                var c = (Color)colors323[i];

                Copy(c, ref index);
            }

            if (HasColors4)
            {
                Copy(colors4[i], ref index);
            }
            else if (HasColors324)
            {
                var c = (Color)colors324[i];

                Copy(c, ref index);
            }

            if (HasUV)
            {
                Copy(uv[i], ref index);
            }

            if (HasUV2)
            {
                Copy(uv2[i], ref index);
            }

            if (HasUV3)
            {
                Copy(uv3[i], ref index);
            }

            if (HasUV4)
            {
                Copy(uv4[i], ref index);
            }

            if (HasUV5)
            {
                Copy(uv5[i], ref index);
            }

            if (HasUV6)
            {
                Copy(uv6[i], ref index);
            }

            if (HasUV7)
            {
                Copy(uv7[i], ref index);
            }

            if (HasUV8)
            {
                Copy(uv8[i], ref index);
            }

            if(HasBoneIndices)
            {
                Copy(boneIndices[i], ref index);
            }

            if(HasBoneWeights)
            {
                Copy(boneWeights[i], ref index);
            }
        }

        return buffer;
    }

    /// <summary>
    /// Makes this mesh active for rendering
    /// </summary>
    /// <param name="state">The render state</param>
    /// <param name="submeshIndex">A submesh index, or 0</param>
    /// <returns>Whether it was set active</returns>
    internal bool SetActive(ref RenderState state, int submeshIndex = 0)
    {
        UploadMeshData();

        if(vertexBuffer == null || indexBuffer == null)
        {
            return false;
        }

        if(submeshes.Count == 0)
        {
            state.indexCount = indices.Length;
        }
        else if(submeshIndex >= 0 && submeshIndex < submeshes.Count)
        {
            var submesh = submeshes[submeshIndex];

            state.startVertex = submesh.startVertex;
            state.startIndex = submesh.startIndex;
            state.indexCount = submesh.indexCount;
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a default mesh for a path
    /// </summary>
    /// <param name="path">The default mesh path</param>
    /// <returns>The mesh, or null</returns>
    internal static Mesh GetDefaultMesh(string path)
    {
        if(defaultMeshes.TryGetValue(path, out var mesh))
        {
            return mesh;
        }

        return null;
    }

    internal static int TriangleCount(MeshTopology topology, int indexCount)
    {
        return topology switch
        {
            MeshTopology.Triangles => indexCount / 3,
            MeshTopology.TriangleStrip => indexCount - 2,
            //Can't calculate for other modes
            _ => indexCount,
        };
    }

    internal int SubmeshTriangleCount(int submeshIndex)
    {
        if(submeshIndex < 0)
        {
            return 0;
        }

        if(submeshes.Count == 0)
        {
            if(submeshIndex == 0)
            {
                return TriangleCount(MeshTopology, Indices.Length);
            }

            return 0;
        }

        if(submeshIndex >= submeshes.Count)
        {
            return 0;
        }

        var submesh = submeshes[submeshIndex];

        return TriangleCount(MeshTopology, submesh.indexCount);
    }
}
