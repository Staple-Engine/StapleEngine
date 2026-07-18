using Staple.Internal;
using Staple.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
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

    internal static readonly string LogTag = "Mesh";

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
    private Vector3[] vertices;

    /// <summary>
    /// List of normals. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector3[] normals;

    /// <summary>
    /// List of tangents. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector3[] tangents;

    /// <summary>
    /// List of bitangents. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector3[] bitangents;

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Color[] colors;

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Color[] colors2;

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Color[] colors3;

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Color[] colors4;

    /// <summary>
    /// List of UVs in the first channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector2[] uv;

    /// <summary>
    /// List of UVs in the second channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector2[] uv2;

    /// <summary>
    /// List of UVs in the third channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector2[] uv3;

    /// <summary>
    /// List of UVs in the fourth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector2[] uv4;

    /// <summary>
    /// List of UVs in the fifth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector2[] uv5;

    /// <summary>
    /// List of UVs in the sixth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector2[] uv6;

    /// <summary>
    /// List of UVs in the seventh channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector2[] uv7;

    /// <summary>
    /// List of UVs in the eighth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector2[] uv8;

    /// <summary>
    /// List of indices
    /// </summary>
    private int[] indices;

    /// <summary>
    /// List of bone indices. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector4[] boneIndices;

    /// <summary>
    /// List of bone weights. This is only valid if you don't use SetMeshData.
    /// </summary>
    private Vector4[] boneWeights;

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
            if(meshAsset == null || meshAssetIndex < 0 || meshAssetIndex >= meshAsset.Meshes.Length)
            {
                return null;
            }

            return meshAsset.Meshes[meshAssetIndex];
        }
    }

    /// <summary>
    /// List of vertices. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector3[] VerticesInternal
    {
        get
        {
            if(MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.vertices;
            }

            return vertices ?? [];
        }

        set
        {
            vertices = value;
        }
    }

    /// <summary>
    /// List of normals. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector3[] NormalsInternal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.normals;
            }

            return normals ?? [];
        }

        set
        {
            normals = value;
        }
    }

    /// <summary>
    /// List of tangents. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector3[] TangentsInternal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.tangents;
            }

            return tangents ?? [];
        }

        set
        {
            tangents = value;
        }
    }

    /// <summary>
    /// List of bitangents. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector3[] BitangentsInternal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.bitangents;
            }

            return bitangents ?? [];
        }

        set
        {
            bitangents = value;
        }
    }

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color[] ColorsInternal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.colors;
            }

            return colors ?? [];
        }

        set
        {
            colors = value;
        }
    }

    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color[] Colors2Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.colors2;
            }

            return colors2 ?? [];
        }

        set
        {
            colors2 = value;
        }
    }


    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color[] Colors3Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.colors3;
            }

            return colors3 ?? [];
        }

        set
        {
            colors3 = value;
        }
    }


    /// <summary>
    /// List of colors. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Color[] Colors4Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.colors4;
            }

            return colors4 ?? [];
        }

        set
        {
            colors4 = value;
        }
    }

    /// <summary>
    /// List of UVs in the first channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] UVInternal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.UV1;
            }

            return uv ?? [];
        }

        set
        {
            uv = value;
        }
    }

    /// <summary>
    /// List of UVs in the second channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] UV2Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.UV2;
            }

            return uv2 ?? [];
        }

        set
        {
            uv2 = value;
        }
    }

    /// <summary>
    /// List of UVs in the third channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] UV3Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.UV3;
            }

            return uv3 ?? [];
        }

        set
        {
            uv3 = value;
        }
    }

    /// <summary>
    /// List of UVs in the fourth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] UV4Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.UV4;
            }

            return uv4 ?? [];
        }

        set
        {
            uv4 = value;
        }
    }

    /// <summary>
    /// List of UVs in the fifth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] UV5Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.UV5;
            }

            return uv5 ?? [];
        }

        set
        {
            uv5 = value;
        }
    }

    /// <summary>
    /// List of UVs in the sixth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] UV6Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.UV6;
            }

            return uv6 ?? [];
        }

        set
        {
            uv6 = value;
        }
    }

    /// <summary>
    /// List of UVs in the seventh channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] UV7Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.UV7;
            }

            return uv7 ?? [];
        }

        set
        {
            uv7 = value;
        }
    }

    /// <summary>
    /// List of UVs in the eighth channel. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector2[] UV8Internal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.UV8;
            }

            return uv8 ?? [];
        }

        set
        {
            uv8 = value;
        }
    }

    /// <summary>
    /// List of indices
    /// </summary>
    internal int[] IndicesInternal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.indices;
            }

            return indices ?? [];
        }

        set
        {
            indices = value;
        }
    }

    /// <summary>
    /// List of bone indices. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector4[] BoneIndicesInternal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.boneIndices;
            }

            return boneIndices ?? [];
        }

        set
        {
            boneIndices = value;
        }
    }

    /// <summary>
    /// List of bone weights. This is only valid if you don't use SetMeshData.
    /// </summary>
    internal Vector4[] BoneWeightsInternal
    {
        get
        {
            if (MeshAssetMesh is MeshAsset.MeshInfo mesh)
            {
                return mesh.boneWeights;
            }

            return boneWeights ?? [];
        }

        set
        {
            boneWeights = value;
        }
    }

    /// <summary>
    /// Internal list of vertex layouts for each a unique key
    /// </summary>
    internal static Dictionary<string, VertexLayout> vertexLayouts = [];

    internal bool HasNormals => (NormalsInternal?.Length ?? 0) > 0;

    internal bool HasTangents => (TangentsInternal?.Length ?? 0) > 0;

    internal bool HasBitangents => (BitangentsInternal?.Length ?? 0) > 0;

    internal bool HasColors => (ColorsInternal?.Length ?? 0) > 0;

    internal bool HasColors2 => (Colors2Internal?.Length ?? 0) > 0;

    internal bool HasColors3 => (Colors3Internal?.Length ?? 0) > 0;

    internal bool HasColors4 => (Colors4Internal?.Length ?? 0) > 0;

    internal bool HasUV => (UVInternal?.Length ?? 0) > 0;

    internal bool HasUV2 => (UV2Internal?.Length ?? 0) > 0;

    internal bool HasUV3 => (UV3Internal?.Length ?? 0) > 0;

    internal bool HasUV4 => (UV4Internal?.Length ?? 0) > 0;

    internal bool HasUV5 => (UV5Internal?.Length ?? 0) > 0;

    internal bool HasUV6 => (UV6Internal?.Length ?? 0) > 0;

    internal bool HasUV7 => (UV7Internal?.Length ?? 0) > 0;

    internal bool HasUV8 => (UV8Internal?.Length ?? 0) > 0;

    internal bool HasBoneIndices => (BoneIndicesInternal?.Length ?? 0) > 0;

    internal bool HasBoneWeights => (BoneWeightsInternal?.Length ?? 0) > 0;

    internal bool IsStaticMesh;

    internal BufferAttributeContainer.Entries staticMeshEntries;

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

    private static Mesh _cube;

    private static Mesh _sphere;

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

                if (i == (stackCount - 1))
                {
                    continue;
                }
                
                indices.Add(k1 + 1);
                indices.Add(k2);
                indices.Add(k2 + 1);
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

        ResourceManager.instance.userCreatedMeshes.Add(new(this));
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

        if (mesh.HasColors)
        {
            builder.Add(VertexAttribute.Color0, VertexAttributeType.Float4);
        }

        if (mesh.HasColors2)
        {
            builder.Add(VertexAttribute.Color1, VertexAttributeType.Float4);
        }

        if (mesh.HasColors3)
        {
            builder.Add(VertexAttribute.Color2, VertexAttributeType.Float4);
        }

        if (mesh.HasColors4)
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
            builder.Add(VertexAttribute.BlendIndices, VertexAttributeType.Float4);
        }

        if(mesh.HasBoneWeights)
        {
            builder.Add(VertexAttribute.BlendWeights, VertexAttributeType.Float4);
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
        var vertices = VerticesInternal;
        var normals = NormalsInternal;
        var tangents = TangentsInternal;
        var bitangents = BitangentsInternal;
        var colors = ColorsInternal;
        var colors2 = Colors2Internal;
        var colors3 = Colors3Internal;
        var colors4 = Colors4Internal;
        var uv = UVInternal;
        var uv2 = UV2Internal;
        var uv3 = UV3Internal;
        var uv4 = UV4Internal;
        var uv5 = UV5Internal;
        var uv6 = UV6Internal;
        var uv7 = UV7Internal;
        var uv8 = UV8Internal;
        var boneIndices = BoneIndicesInternal;
        var boneWeights = BoneWeightsInternal;

        var size = layout.Stride * vertices.Length;

        var buffer = new byte[size];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                var src = (byte*)&source;

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

            if (HasColors2)
            {
                Copy(colors2[i], ref index);
            }

            if (HasColors3)
            {
                Copy(colors3[i], ref index);
            }

            if (HasColors4)
            {
                Copy(colors4[i], ref index);
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
        if(!IsStaticMesh && (vertexBuffer == null || indexBuffer == null))
        {
            return false;
        }

        state.staticMeshEntries = null;

        state.primitiveType = MeshTopology;

        if(submeshes.Count == 0)
        {
            state.vertexBuffer = vertexBuffer;
            state.indexBuffer = indexBuffer;
            state.staticMeshEntries = staticMeshEntries;
            state.indexCount = indices.Length;
        }
        else if(submeshIndex >= 0 && submeshIndex < submeshes.Count)
        {
            var submesh = submeshes[submeshIndex];

            state.vertexBuffer = vertexBuffer;
            state.indexBuffer = indexBuffer;
            state.staticMeshEntries = staticMeshEntries;
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
        return defaultMeshes.GetValueOrDefault(path);
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

    internal void MarkStaticMesh()
    {
        if(IsStaticMesh ||
            (VerticesInternal?.Length ?? 0) == 0 ||
            (IndicesInternal?.Length ?? 0) == 0)
        {
            return;
        }

        IsStaticMesh = true;

        UpdateStaticMeshData();
    }

    internal void UpdateStaticMeshData()
    {
        if (staticMeshEntries == null)
        {
            var vertexCount = VerticesInternal.Length;
            var indexCount = IndicesInternal.Length;

            staticMeshEntries = RenderSystem.Backend.StaticMeshData.Allocate(vertexCount, indexCount);
        }

        if ((VerticesInternal?.Length ?? 0) > staticMeshEntries.positionEntry.length ||
            (IndicesInternal?.Length ?? 0) > staticMeshEntries.indicesEntry.length)
        {
            RenderSystem.Backend.StaticMeshData.Free(staticMeshEntries);

            staticMeshEntries = RenderSystem.Backend.StaticMeshData.Allocate(VerticesInternal.Length, IndicesInternal.Length);
        }

        if (RenderSystem.Backend.StaticMeshData.TryGetPositions(staticMeshEntries, out var positions, true))
        {
            var source = VerticesInternal.AsSpan();

            source.CopyTo(positions);
        }

        if (HasNormals && RenderSystem.Backend.StaticMeshData.TryGetNormals(staticMeshEntries, out var normals, true))
        {
            var source = NormalsInternal.AsSpan();

            source.CopyTo(normals);
        }

        if (HasTangents && RenderSystem.Backend.StaticMeshData.TryGetTangents(staticMeshEntries, out var tangents, true))
        {
            var source = TangentsInternal.AsSpan();

            source.CopyTo(tangents);
        }

        if (HasBitangents && RenderSystem.Backend.StaticMeshData.TryGetBitangents(staticMeshEntries, out var bitangents, true))
        {
            var source = BitangentsInternal.AsSpan();

            source.CopyTo(bitangents);
        }

        if (HasColors && RenderSystem.Backend.StaticMeshData.TryGetColor0(staticMeshEntries, out var colors, true))
        {
            var source = ColorsInternal.AsSpan();

            source.CopyTo(colors);
        }

        if (HasColors2 && RenderSystem.Backend.StaticMeshData.TryGetColor1(staticMeshEntries, out var colors2, true))
        {
            var source = Colors2Internal.AsSpan();

            source.CopyTo(colors2);
        }

        if (HasColors3 && RenderSystem.Backend.StaticMeshData.TryGetColor2(staticMeshEntries, out var colors3, true))
        {
            var source = Colors3Internal.AsSpan();

            source.CopyTo(colors3);
        }

        if (HasColors4 && RenderSystem.Backend.StaticMeshData.TryGetColor3(staticMeshEntries, out var colors4, true))
        {
            var source = Colors4Internal.AsSpan();

            source.CopyTo(colors4);
        }

        if (HasUV && RenderSystem.Backend.StaticMeshData.TryGetTexCoord0(staticMeshEntries, out var uv0, true))
        {
            var source = UVInternal.AsSpan();

            source.CopyTo(uv0);
        }

        if (HasUV2 && RenderSystem.Backend.StaticMeshData.TryGetTexCoord1(staticMeshEntries, out var uv1, true))
        {
            var source = UV2Internal.AsSpan();

            source.CopyTo(uv1);
        }

        if (HasUV3 && RenderSystem.Backend.StaticMeshData.TryGetTexCoord2(staticMeshEntries, out var uv2, true))
        {
            var source = UV3Internal.AsSpan();

            source.CopyTo(uv2);
        }

        if (HasUV4 && RenderSystem.Backend.StaticMeshData.TryGetTexCoord3(staticMeshEntries, out var uv3, true))
        {
            var source = UV4Internal.AsSpan();

            source.CopyTo(uv3);
        }

        if (HasUV5 && RenderSystem.Backend.StaticMeshData.TryGetTexCoord4(staticMeshEntries, out var uv4, true))
        {
            var source = UV5Internal.AsSpan();

            source.CopyTo(uv4);
        }

        if (HasUV6 && RenderSystem.Backend.StaticMeshData.TryGetTexCoord5(staticMeshEntries, out var uv5, true))
        {
            var source = UV6Internal.AsSpan();

            source.CopyTo(uv5);
        }

        if (HasUV7 && RenderSystem.Backend.StaticMeshData.TryGetTexCoord6(staticMeshEntries, out var uv6, true))
        {
            var source = UV7Internal.AsSpan();

            source.CopyTo(uv6);
        }

        if (HasUV8 && RenderSystem.Backend.StaticMeshData.TryGetTexCoord7(staticMeshEntries, out var uv7, true))
        {
            var source = UV8Internal.AsSpan();

            source.CopyTo(uv7);
        }

        if(RenderSystem.Backend.StaticMeshData.TryGetIndices(staticMeshEntries, out var meshIndices, true))
        {
            var from = IndicesInternal.AsSpan();

            from.CopyTo(meshIndices);

            var start = staticMeshEntries.positionEntry.start;

            for (var i = 0; i < meshIndices.Length; i++)
            {
                meshIndices[i] += start;
            }
        }
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
