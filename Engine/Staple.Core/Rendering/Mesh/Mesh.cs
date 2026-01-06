using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// 3D data container (Mesh)
/// </summary>
public sealed partial class Mesh : IGuidAsset
{
    public struct StandardVertex
    {
        public Vector3 position;
        public Vector2 uv;
        public Vector3 normal;
        public Vector3 tangent;
        public Vector3 bitangent;
        public Color color;
    }

    public static Lazy<VertexLayout> StandardVertexLayout = new(() =>
    {
        return VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.Position, VertexAttributeType.Float3)
            .Add(VertexAttribute.TexCoord0, VertexAttributeType.Float2)
            .Add(VertexAttribute.Normal, VertexAttributeType.Float3)
            .Add(VertexAttribute.Tangent, VertexAttributeType.Float3)
            .Add(VertexAttribute.Bitangent, VertexAttributeType.Float3)
            .Add(VertexAttribute.Color0, VertexAttributeType.Float4)
            .Build();
    });

    /// <summary>
    /// Whether this mesh is readable by the CPU
    /// </summary>
    public readonly bool isReadable = true;

    /// <summary>
    /// Whether this mesh is writable
    /// </summary>
    public readonly bool isWritable = true;

    /// <summary>
    /// The bounds of the mesh
    /// </summary>
    public AABB bounds { get; internal set; }

    /// <summary>
    /// The format of the indices for this mesh
    /// </summary>
    public MeshIndexFormat IndexFormat
    {
        get => indexFormat;

        set
        {
            if (isWritable == false)
            {
                return;
            }

            changed = true;

            indexFormat = value;

            indices = [];
        }
    }

    /// <summary>
    /// The mesh's primitive type
    /// </summary>
    public MeshTopology MeshTopology
    {
        get => meshTopology;

        set
        {
            if(isWritable == false)
            {
                return;
            }

            changed = true;

            meshTopology = value;
        }
    }

    /// <summary>
    /// Sets or gets the current vertices.
    /// Getting depends on isReadable.
    /// Note: When setting, if the vertice count is different than previous, it'll reset all other vertex data fields.
    /// </summary>
    public Vector3[] Vertices
    {
        get
        {
            if(isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return vertices ?? [];
        }

        set
        {
            if(isWritable == false || meshDataBlob != null)
            {
                return;
            }

            var needsReset = vertices == null || vertices.Length != value.Length;

            vertices = value;
            changed = true;

            if(needsReset)
            {
                normals = null;
                tangents = null;
                bitangents = null;
                colors = null;
                colors2 = null;
                colors3 = null;
                colors4 = null;
                colors32 = null;
                colors322 = null;
                colors323 = null;
                colors323 = null;
                uv = null;
                uv2 = null;
                uv3 = null;
                uv4 = null;
                uv5 = null;
                uv6 = null;
                uv7 = null;
                uv8 = null;
                indices = null;
            }
        }
    }

    /// <summary>
    /// Sets or gets the current normals.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector3[] Normals
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return normals ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            normals = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current tangents.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector3[] Tangents
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return tangents ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            tangents = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current bitangents.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector3[] Bitangents
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return bitangents ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            bitangents = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current colors.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Color[] Colors
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return colors ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            colors = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current colors.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Color[] Colors2
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return colors2 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            colors2 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current colors.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Color[] Colors3
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return colors3 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            colors3 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current colors.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Color[] Colors4
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return colors4 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            colors4 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current colors as Color32.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Color32[] Colors32
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return colors32 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            colors32 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current colors as Color32.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Color32[] Colors322
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return colors322 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            colors322 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current colors as Color32.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Color32[] Colors323
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return colors323 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            colors323 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current colors as Color32.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Color32[] Colors324
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return colors324 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            colors324 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current UVs for channel 1.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector2[] UV
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return uv ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            uv = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current UVs for channel 2.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector2[] UV2
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return uv2 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            uv2 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current UVs for channel 3.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector2[] UV3
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return uv3 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            uv3 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current UVs for channel 4.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector2[] UV4
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return uv4 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            uv4 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current UVs for channel 5.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector2[] UV5
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return uv5 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            uv5 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current UVs for channel 7.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector2[] UV6
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return uv6 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            uv6 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current UVs for channel 7.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector2[] UV7
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return uv7 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            uv7 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the current UVs for channel 8.
    /// Getting depends on isReadable.
    /// Note: When setting, must have the same size as the current vertices.
    /// </summary>
    public Vector2[] UV8
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return uv8 ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            uv8 = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the bone indices for the mesh.
    /// Getting depends on isReadable.
    /// </summary>
    public Vector4[] BoneIndices
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return boneIndices ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            boneIndices = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the bone weights for the mesh.
    /// Getting depends on isReadable.
    /// </summary>
    public Vector4[] BoneWeights
    {
        get
        {
            if (isReadable == false || meshDataBlob != null)
            {
                return [];
            }

            return boneWeights ?? [];
        }

        set
        {
            if (isWritable == false || meshDataBlob != null)
            {
                return;
            }

            if (value == null || value.Length == 0 || value.Length != (vertices?.Length ?? 0))
            {
                throw new ArgumentException("Array length should match vertices length");
            }

            boneWeights = value;
            changed = true;
        }
    }

    /// <summary>
    /// Sets or gets the geometry indices for the mesh.
    /// Getting depends on isReadable.
    /// </summary>
    public int[] Indices
    {
        get
        {
            if (isReadable == false)
            {
                return [];
            }

            return indices ?? [];
        }

        set
        {
            if (isWritable == false)
            {
                return;
            }

            indices = value;
            changed = true;
        }
    }

    /// <summary>
    /// Total amount of vertices
    /// </summary>
    public int VertexCount
    {
        get
        {
            if(meshDataBlob != null && meshDataVertexLayout != null)
            {
                return meshDataBlob.Length / meshDataVertexLayout.Stride;
            }

            return vertices?.Length ?? 0;
        }
    }

    /// <summary>
    /// Total amount of indices
    /// </summary>
    public int IndexCount => indices?.Length ?? 0;

    /// <summary>
    /// Gets the mesh components for this mesh
    /// </summary>
    /// <remarks>Doesn't work if you use <see cref="SetMeshData(Span{byte}, VertexLayout)"/> or
    /// <see cref="SetMeshData{T}(Span{T}, VertexLayout)"/> unless the mesh comes from a mesh asset</remarks>
    public MeshAssetComponent Components
    {
        get
        {
            if(MeshAssetMesh != null)
            {
                return MeshAssetMesh.components;
            }

            var result = MeshAssetComponent.None;

            if ((UV?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.UV1;
            }

            if ((UV2?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.UV2;
            }

            if ((UV3?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.UV3;
            }

            if ((UV4?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.UV4;
            }

            if ((UV5?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.UV5;
            }

            if ((UV6?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.UV6;
            }

            if ((UV7?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.UV7;
            }

            if ((UV8?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.UV8;
            }

            if ((normals?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.Normal;
            }

            if ((tangents?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.Tangent;
            }

            if ((bitangents?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.Bitangent;
            }

            if ((colors?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.Color1;
            }

            if ((colors2?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.Color2;
            }

            if ((colors3?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.Color3;
            }

            if ((colors4?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.Color4;
            }

            if ((boneIndices?.Length ?? 0) > 0 || (boneWeights?.Length ?? 0) > 0)
            {
                result |= MeshAssetComponent.BoneIndicesWeights;
            }

            return result;
        }
    }

    private readonly GuidHasher guidHasher = new();

    public GuidHasher Guid => guidHasher;

    /// <summary>
    /// Loads a mesh from a guid
    /// </summary>
    /// <param name="guid">The asset's guid</param>
    /// <returns>The mesh, or null</returns>
    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadMesh(guid);
    }

    public Mesh()
    {
        ResourceManager.instance.userCreatedMeshes.Add(new(this));
    }

    /// <summary>
    /// Clears all data in this mesh
    /// </summary>
    public void Clear()
    {
        vertices = null;
        normals = null;
        colors = null;
        colors2 = null;
        colors3 = null;
        colors4 = null;
        colors32 = null;
        colors322 = null;
        colors323 = null;
        colors324 = null;
        uv = null;
        uv2 = null;
        uv3 = null;
        uv4 = null;
        uv5 = null;
        uv6 = null;
        uv7 = null;
        uv8 = null;
        indices = null;
        tangents = null;
        bitangents = null;
        boneIndices = null;
        boneWeights = null;
        meshAsset = null;
        meshAssetIndex = 0;
        meshDataBlob = null;
        meshDataVertexLayout = null;

        submeshes.Clear();

        changed = true;

        vertexBuffer?.Destroy();
        indexBuffer?.Destroy();

        vertexBuffer = null;
        indexBuffer = null;
    }

    /// <summary>
    /// Sets the mesh data for this mesh all at once. Useful if you have your data formatted through a struct.
    /// </summary>
    /// <param name="meshData">The mesh data as bytes</param>
    /// <param name="vertexLayout">The vertex layout</param>
    /// <remarks>If the vertex buffer is valid and is dynamic, this will immediately update the vertex buffer</remarks>
    public void SetMeshData(Span<byte> meshData, VertexLayout vertexLayout)
    {
        if(vertexLayout == null)
        {
            throw new Exception("Vertex Layout is null");
        }

        if(meshData.Length % vertexLayout.Stride != 0)
        {
            throw new Exception($"Mesh Data Blob has misaligned data (has {meshData.Length} bytes, should be multiples of {vertexLayout.Stride})");
        }

        meshDataBlob = meshData.ToArray();
        meshDataVertexLayout = vertexLayout;

        if(vertexBuffer != null && vertexBuffer.Disposed == false && isDynamic)
        {
            vertexBuffer.Update(meshDataBlob);
        }
        else
        {
            changed = true;
        }
    }

    /// <summary>
    /// Sets the mesh data for this mesh all at once. Useful if you have your data formatted through a struct.
    /// </summary>
    /// <param name="meshData">The mesh data vertex elements</param>
    /// <param name="vertexLayout">The vertex layout</param>
    /// <remarks>If the vertex buffer is valid and is dynamic, this will immediately update the vertex buffer</remarks>
    public void SetMeshData<T>(Span<T> meshData, VertexLayout vertexLayout) where T : unmanaged
    {
        if (vertexLayout == null)
        {
            throw new Exception("Vertex Layout is null");
        }

        var size = Marshal.SizeOf<T>();

        if(size != vertexLayout.Stride)
        {
            throw new Exception($"Mesh Data element size does not equal vertex layout size (has: {size}, needs: {vertexLayout.Stride})");
        }

        unsafe
        {
            fixed(void *ptr = meshData)
            {
                var buffer = new byte[meshData.Length * size];

                Marshal.Copy((nint)ptr, buffer, 0, buffer.Length);

                meshDataBlob = buffer;
            }
        }

        meshDataVertexLayout = vertexLayout;

        if (vertexBuffer != null && vertexBuffer.Disposed == false && isDynamic)
        {
            vertexBuffer.Update(meshDataBlob);
        }
        else
        {
            changed = true;
        }
    }

    /// <summary>
    /// Uploads the mesh data to the GPU
    /// </summary>
    internal void UploadMeshData()
    {
        if (changed == false ||
            (IsStaticMesh == false && (vertexBuffer?.Disposed ?? true) == false && (indexBuffer?.Disposed ?? true) == false))
        {
            return;
        }

        changed = false;

        if(IsStaticMesh)
        {
            UpdateStaticMeshData();

            return;
        }

        vertexBuffer?.Destroy();
        indexBuffer?.Destroy();

        vertexBuffer = null;
        indexBuffer = null;

        if (meshDataBlob == null && (vertices == null || vertices.Length == 0))
        {
            throw new InvalidOperationException($"Mesh has no vertices");
        }

        if (indices == null || indices.Length == 0)
        {
            throw new InvalidOperationException($"Mesh has no indices");
        }

        switch(meshTopology)
        {
            case MeshTopology.Triangles:

                if(indices.Length % 3 != 0)
                {
                    throw new InvalidOperationException($"Triangle mesh doesn't have the right amount of indices. Has: {indices.Length}. Should be a multiple of 3");
                }

                break;

            case MeshTopology.TriangleStrip:

                if(indices.Length < 3)
                {
                    throw new InvalidOperationException($"Triangle Strip mesh doesn't have the right amount of indices. Has: {indices.Length}. Should have at least 3");
                }

                break;

            case MeshTopology.Lines:

                if (indices.Length % 2 != 0)
                {
                    throw new InvalidOperationException($"Line mesh doesn't have the right amount of indices. Has: {indices.Length}. Should be a multiple of 2");
                }

                break;

            case MeshTopology.LineStrip:

                if (indices.Length < 2)
                {
                    throw new InvalidOperationException($"Line Strip mesh doesn't have the right amount of indices. Has: {indices.Length}. Should have at least 2");
                }

                break;
        }

        var layout = meshDataVertexLayout ?? GetVertexLayout(this);

        if(layout == null)
        {
            Log.Error($"[Mesh] Failed to get vertex layout for this mesh!");

            return;
        }

        if(meshDataBlob != null && meshDataBlob.Length % layout.Stride != 0)
        {
            Log.Error($"[Mesh] Mesh Data Blob has misaligned data (has {meshDataBlob.Length} bytes, should be multiples of {layout.Stride})");

            return;
        }

        var vertexBlob = meshDataBlob ?? MakeVertexDataBlob(layout);

        if(vertexBlob == null)
        {
            return;
        }

        vertexBuffer = VertexBuffer.Create(vertexBlob, layout);

        if(vertexBuffer == null)
        {
            return;
        }

        switch (indexFormat)
        {
            case MeshIndexFormat.UInt16:

                {
                    var data = new ushort[indices.Length];

                    for (var i = 0; i < indices.Length; i++)
                    {
                        if (indices[i] >= ushort.MaxValue)
                        {
                            throw new InvalidOperationException($"[Mesh] Invalid value {indices[i]} for 16-bit indices");
                        }

                        data[i] = (ushort)indices[i];
                    }

                    indexBuffer = IndexBuffer.Create(data);
                }

                break;

            case MeshIndexFormat.UInt32:

                {
                    uint[] data = new uint[indices.Length];

                    for (var i = 0; i < indices.Length; i++)
                    {
                        data[i] = (uint)indices[i];
                    }

                    indexBuffer = IndexBuffer.Create(data);
                }

                break;
        }

        if(indexBuffer == null)
        {
            vertexBuffer?.Destroy();
            vertexBuffer = null;
        }
    }

    /// <summary>
    /// Updates the estimated bounds of the mesh by calculating an AABB
    /// </summary>
    public void UpdateBounds()
    {
        bounds = AABB.CreateFromPoints(vertices);
    }

    /// <summary>
    /// Marks a mesh as dynamic (can be modified)
    /// </summary>
    public void MarkDynamic()
    {
        isDynamic = true;

        changed = true;

        vertexBuffer?.Destroy();
        indexBuffer?.Destroy();

        vertexBuffer = null;
        indexBuffer = null;
    }

    /// <summary>
    /// Adds a submesh to the mesh. By default a mesh has no submeshes and will be rendered as a whole
    /// </summary>
    /// <param name="startVertex">The start index of the vertices</param>
    /// <param name="vertexCount">The amount of vertices to render</param>
    /// <param name="startIndex">The start index of the indices</param>
    /// <param name="indexCount">The amount of indices to render</param>
    /// <param name="topology">The topology of the mesh</param>
    public void AddSubmesh(int startVertex, int vertexCount, int startIndex, int indexCount, MeshTopology topology)
    {
        if(startVertex < 0 ||
            startVertex + vertexCount > vertices.Length ||
            startIndex < 0 || startIndex + indexCount > indices.Length)
        {
            return;
        }

        submeshes.Add(new()
        {
            startVertex = startVertex,
            vertexCount = vertexCount,
            startIndex = startIndex,
            indexCount = indexCount,
            topology = topology,
        });
    }

    /// <summary>
    /// Generates normals for a list of vertices
    /// </summary>
    /// <param name="positions">The vertex positions</param>
    /// <param name="indices">The vertex indices</param>
    /// <param name="smooth">Whether to generate smooth normals. Note that this can be a slow process.</param>
    /// <remarks>Positions are expected to be triangles</remarks>
    /// <returns>An array of normals. Might be empty if the indices aren't a multiple of 3.</returns>
    public static Vector3[] GenerateNormals(ReadOnlySpan<Vector3> positions, ReadOnlySpan<ushort> indices, bool smooth = false)
    {
        if(indices.Length % 3 != 0)
        {
            return [];
        }

        for (var i = 0; i < indices.Length; i++)
        {
            if (indices[i] >= positions.Length)
            {
                return [];
            }
        }

        var normals = new Vector3[positions.Length];

        for (var i = 0; i < indices.Length; i += 3)
        {
            var p0 = positions[indices[i]];
            var p1 = positions[indices[i + 1]];
            var p2 = positions[indices[i + 2]];

            var normal = (p1 - p0).Cross(p2 - p0);

            normals[indices[i]] += normal;
            normals[indices[i + 1]] += normal;
            normals[indices[i + 2]] += normal;
        }

        if (smooth)
        {
            var uniquePositions = new Dictionary<Vector3, List<int>>();
            var handled = new Dictionary<Vector3, Vector3>();

            for (var i = 0; i < positions.Length; i++)
            {
                var p = positions[i];

                if (uniquePositions.TryGetValue(p, out var list) == false)
                {
                    list = [];

                    uniquePositions.Add(p, list);
                }

                list.Add(i);
            }

            foreach (var p in uniquePositions)
            {
                var n = Vector3.Zero;

                foreach (var index in p.Value)
                {
                    n += normals[index];
                }

                handled.Add(p.Key, n);
            }

            for (var i = 0; i < positions.Length; i++)
            {
                if (handled.TryGetValue(positions[i], out var n))
                {
                    normals[i] = n;
                }
            }
        }

        for (var i = 0; i < normals.Length; i++)
        {
            normals[i] = normals[i].Normalized;
        }

        return normals;
    }

    /// <summary>
    /// Generates normals for a list of vertices
    /// </summary>
    /// <param name="positions">The vertex positions</param>
    /// <param name="indices">The vertex indices</param>
    /// <param name="smooth">Whether to generate smooth normals. Note that this can be a slow process.</param>
    /// <remarks>Positions are expected to be triangles</remarks>
    /// <returns>An array of normals. Might be empty if the indices aren't a multiple of 3.</returns>
    public static Vector3[] GenerateNormals(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> indices, bool smooth = false)
    {
        if (indices.Length % 3 != 0)
        {
            return [];
        }

        for (var i = 0; i < indices.Length; i++)
        {
            if (indices[i] < 0 ||
                indices[i] >= positions.Length)
            {
                return [];
            }
        }

        var normals = new Vector3[positions.Length];

        for (var i = 0; i < indices.Length; i += 3)
        {
            var p0 = positions[indices[i]];
            var p1 = positions[indices[i + 1]];
            var p2 = positions[indices[i + 2]];

            var normal = (p1 - p0).Cross(p2 - p0);

            normals[indices[i]] += normal;
            normals[indices[i + 1]] += normal;
            normals[indices[i + 2]] += normal;
        }

        if (smooth)
        {
            var uniquePositions = new Dictionary<Vector3, List<int>>();
            var handled = new Dictionary<Vector3, Vector3>();

            for (var i = 0; i < positions.Length; i++)
            {
                var p = positions[i];

                if (uniquePositions.TryGetValue(p, out var list) == false)
                {
                    list = [];

                    uniquePositions.Add(p, list);
                }

                list.Add(i);
            }

            foreach (var p in uniquePositions)
            {
                var n = Vector3.Zero;

                foreach (var index in p.Value)
                {
                    n += normals[index];
                }

                handled.Add(p.Key, n);
            }

            for (var i = 0; i < positions.Length; i++)
            {
                if (handled.TryGetValue(positions[i], out var n))
                {
                    normals[i] = n;
                }
            }
        }

        for (var i = 0; i < positions.Length; i++)
        {
            normals[i] = normals[i].Normalized;
        }

        return normals;
    }

    /// <summary>
    /// Generates tangents and bitangents from a few components
    /// </summary>
    /// <param name="positions">The vertex positions</param>
    /// <param name="UVs">The vertex UVs</param>
    /// <param name="normals">The vertex normals</param>
    /// <param name="indices">The vertex indices</param>
    /// <param name="outTangents">The generated tangents</param>
    /// <param name="outBitangents">The generated bitangents</param>
    /// <remarks>Requires the data to be for triangle primitives</remarks>
    public static void GenerateTangents(ReadOnlySpan<Vector3> positions, ReadOnlySpan<Vector2> UVs,
        ReadOnlySpan<Vector3> normals, ReadOnlySpan<ushort> indices, out Vector3[] outTangents, out Vector3[] outBitangents)
    {
        if(positions.Length != UVs.Length ||
            positions.Length != normals.Length ||
            indices.Length % 3 != 0)
        {
            outTangents = default;
            outBitangents = default;

            return;
        }

        var tangents = new Vector3[positions.Length];
        var bitangents = new Vector3[positions.Length];

        outTangents = new Vector3[positions.Length];
        outBitangents = new Vector3[positions.Length];

        for (var i = 0; i < indices.Length; i += 3)
        {
            var vertexIndices = (indices[i], indices[i + 1], indices[i + 2]);

            var vectors = (positions[vertexIndices.Item1],
                positions[vertexIndices.Item2],
                positions[vertexIndices.Item3]);

            var uvs = (UVs[vertexIndices.Item1],
                UVs[vertexIndices.Item2],
                UVs[vertexIndices.Item3]);

            var edge1 = vectors.Item2 - vectors.Item1;
            var edge2 = vectors.Item3 - vectors.Item1;

            var uvDelta1 = uvs.Item2 - uvs.Item1;
            var uvDelta2 = uvs.Item3 - uvs.Item1;

            var f = 1.0f / (uvDelta1.X * uvDelta2.Y - uvDelta2.X * uvDelta1.Y);

            var tangent = Vector3.Normalize(f * (uvDelta2.Y * edge1 - uvDelta1.Y * edge2));
            var bitangent = Vector3.Normalize(f * (-uvDelta2.X * edge1 + uvDelta1.X * edge2));

            tangents[vertexIndices.Item1] += tangent;
            tangents[vertexIndices.Item2] += tangent;
            tangents[vertexIndices.Item3] += tangent;

            bitangents[vertexIndices.Item1] += bitangent;
            bitangents[vertexIndices.Item2] += bitangent;
            bitangents[vertexIndices.Item3] += bitangent;
        }

        for (var i = 0; i < positions.Length; i++)
        {
            var normal = normals[i];
            var t = Vector3.Normalize(tangents[i]);
            var b = Vector3.Normalize(bitangents[i]);

            var tangent = Vector3.Normalize(t - normal * Vector3.Dot(normal, t));

            var bitangent = Vector3.Cross(normal, Vector3.Normalize(tangent));

            if (Vector3.Dot(bitangent, b) < 0)
            {
                bitangent = -bitangent;
            }

            outTangents[i] = tangent;
            outBitangents[i] = bitangent;
        }
    }

    /// <summary>
    /// Generates tangents and bitangents from a few components
    /// </summary>
    /// <param name="positions">The vertex positions</param>
    /// <param name="UVs">The vertex UVs</param>
    /// <param name="normals">The vertex normals</param>
    /// <param name="indices">The vertex indices</param>
    /// <param name="outTangents">The generated tangents</param>
    /// <param name="outBitangents">The generated bitangents</param>
    /// <remarks>Requires the data to be for triangle primitives</remarks>
    public static void GenerateTangents(ReadOnlySpan<Vector3> positions, ReadOnlySpan<Vector2> UVs,
        ReadOnlySpan<Vector3> normals, ReadOnlySpan<int> indices, out Vector3[] outTangents, out Vector3[] outBitangents)
    {
        if (positions.Length != UVs.Length ||
            positions.Length != normals.Length ||
            indices.Length % 3 != 0)
        {
            outTangents = default;
            outBitangents = default;

            return;
        }

        var tangents = new Vector3[positions.Length];
        var bitangents = new Vector3[positions.Length];

        outTangents = new Vector3[positions.Length];
        outBitangents = new Vector3[positions.Length];

        for (var i = 0; i < indices.Length; i += 3)
        {
            var vertexIndices = (indices[i], indices[i + 1], indices[i + 2]);

            var vectors = (positions[vertexIndices.Item1],
                positions[vertexIndices.Item2],
                positions[vertexIndices.Item3]);

            var uvs = (UVs[vertexIndices.Item1],
                UVs[vertexIndices.Item2],
                UVs[vertexIndices.Item3]);

            var edge1 = vectors.Item2 - vectors.Item1;
            var edge2 = vectors.Item3 - vectors.Item1;

            var uvDelta1 = uvs.Item2 - uvs.Item1;
            var uvDelta2 = uvs.Item3 - uvs.Item1;

            var f = 1.0f / (uvDelta1.X * uvDelta2.Y - uvDelta2.X * uvDelta1.Y);

            var tangent = Vector3.Normalize(f * (uvDelta2.Y * edge1 - uvDelta1.Y * edge2));
            var bitangent = Vector3.Normalize(f * (-uvDelta2.X * edge1 + uvDelta1.X * edge2));

            tangents[vertexIndices.Item1] += tangent;
            tangents[vertexIndices.Item2] += tangent;
            tangents[vertexIndices.Item3] += tangent;

            bitangents[vertexIndices.Item1] += bitangent;
            bitangents[vertexIndices.Item2] += bitangent;
            bitangents[vertexIndices.Item3] += bitangent;
        }

        for (var i = 0; i < positions.Length; i++)
        {
            var normal = normals[i];
            var t = Vector3.Normalize(tangents[i]);
            var b = Vector3.Normalize(bitangents[i]);

            var tangent = Vector3.Normalize(t - normal * Vector3.Dot(normal, t));

            var bitangent = Vector3.Cross(normal, Vector3.Normalize(tangent));

            if (Vector3.Dot(bitangent, b) < 0)
            {
                bitangent = -bitangent;
            }

            outTangents[i] = tangent;
            outBitangents[i] = bitangent;
        }
    }

    /// <summary>
    /// Creates an instance of a specific mesh. This will only create an object with the mesh itself.
    /// </summary>
    /// <param name="name">The new entity name</param>
    /// <param name="mesh">The mesh to instantiate</param>
    /// <param name="parentEntity">The parent entity</param>
    /// <param name="options">Optional options</param>
    /// <returns>The new entity</returns>
    public static Entity InstanceMesh(string name, Mesh mesh, Entity parentEntity = default, MeshInstanceOptions options = MeshInstanceOptions.None)
    {
        if(mesh is null)
        {
            return default;
        }

        Transform parent = null;

        if (parentEntity.IsValid)
        {
            parent = parentEntity.GetComponent<Transform>();
        }

        var meshEntity = Entity.Create(name, typeof(Transform), typeof(CullingVolume));
        var meshTransform = meshEntity.GetComponent<Transform>();

        meshTransform.SetParent(parent);

        var outMaterials = mesh.meshAsset != null ? mesh.meshAsset.meshes[mesh.meshAssetIndex].submeshMaterialGuids
            .Select(x => ResourceManager.instance.LoadMaterial(x, Platform.IsEditor)).ToList() :
            [ResourceManager.instance.LoadMaterial(AssetDatabase.GetAssetGuid(AssetSerialization.StandardMaterialPath))];

        if (mesh.HasBoneIndices && mesh.meshAsset != null && options.HasFlag(MeshInstanceOptions.MakeUnskinned) == false)
        {
            meshEntity.AddComponent<SkinnedMeshInstance>();

            var skinnedRenderer = meshEntity.AddComponent<SkinnedMeshRenderer>();

            skinnedRenderer.mesh = mesh;
            skinnedRenderer.materials = outMaterials;
            skinnedRenderer.lighting = mesh.meshAsset?.lighting ?? MaterialLighting.Lit;
        }
        else
        {
            var meshRenderer = meshEntity.AddComponent<MeshRenderer>();

            meshRenderer.mesh = mesh;
            meshRenderer.materials = outMaterials;
            meshRenderer.lighting = mesh.meshAsset?.lighting ?? MaterialLighting.Lit;
        }

        return meshEntity;
    }

    /// <summary>
    /// Create an instance of one or more meshes from an asset
    /// </summary>
    /// <param name="name">The new entity name</param>
    /// <param name="asset">The mesh asset</param>
    /// <param name="parentEntity">The parent entity</param>
    /// <param name="options">Optional options</param>
    /// <returns>The new entity</returns>
    public static Entity InstanceMesh(string name, MeshAsset asset, Entity parentEntity = default, MeshInstanceOptions options = MeshInstanceOptions.None)
    {
        if(asset is null)
        {
            return default;
        }

        Transform parent = null;

        if (parentEntity.IsValid)
        {
            parent = parentEntity.GetComponent<Transform>();
        }

        var baseEntity = Entity.Create(name, typeof(Transform), typeof(CullingVolume));
        var baseTransform = baseEntity.GetComponent<Transform>();

        baseTransform.SetParent(parent);

        if(asset.BoneCount > 0)
        {
            baseEntity.AddComponent<SkinnedMeshInstance>();
        }

        Transform stapleRootNodeTransform = null;

        if ((asset.nodes?.Length ?? 0) > 0)
        {
            var parents = new Transform[asset.nodes.Length];

            for (var i = 0; i < asset.nodes.Length; i++)
            {
                var node = asset.nodes[i];

                var nodeParent = Entity.Create(node.name, typeof(Transform));

                var nodeTransform = nodeParent.GetComponent<Transform>();

                if(i == 0 && node.name == "StapleRoot")
                {
                    stapleRootNodeTransform = nodeTransform;
                }

                parents[i] = nodeTransform;

                var parentIndex = node.parent?.index ?? -1;

                var nodeTarget = parentIndex >= 0 ? parents[parentIndex] : (i > 0 && stapleRootNodeTransform != null ? stapleRootNodeTransform : baseTransform);

                foreach(var meshIndex in node.meshIndices)
                {
                    if(meshIndex < 0 || meshIndex >= asset.meshes.Count)
                    {
                        continue;
                    }
                }

                nodeTransform.SetParent(nodeTarget);

                nodeTransform.LocalPosition = node.Position;
                nodeTransform.LocalRotation = node.Rotation;
                nodeTransform.LocalScale = node.Scale;

                foreach (var index in node.meshIndices)
                {
                    if (index < 0 || index >= asset.meshes.Count)
                    {
                        continue;
                    }

                    var mesh = asset.meshes[index];

                    var meshEntity = Entity.Create(mesh.name, typeof(Transform));

                    var meshTransform = meshEntity.GetComponent<Transform>();

                    var isSkinned = mesh.bones.Any(x => x.Length > 0);

                    meshTransform.SetParent(isSkinned && options.HasFlag(MeshInstanceOptions.MakeUnskinned) == false ? baseTransform : nodeTransform);

                    var outMesh = ResourceManager.instance.LoadMesh($"{asset.Guid}:{index}", Platform.IsEditor);
                    var outMaterials = mesh.submeshMaterialGuids.Select(x => ResourceManager.instance.LoadMaterial(x, Platform.IsEditor)).ToList();

                    if (outMesh != null)
                    {
                        if (isSkinned && options.HasFlag(MeshInstanceOptions.MakeUnskinned) == false)
                        {
                            var skinnedRenderer = meshEntity.AddComponent<SkinnedMeshRenderer>();

                            skinnedRenderer.mesh = outMesh;
                            skinnedRenderer.materials = outMaterials;
                            skinnedRenderer.lighting = mesh.lighting;
                        }
                        else
                        {
                            var meshRenderer = meshEntity.AddComponent<MeshRenderer>();

                            meshRenderer.mesh = outMesh;
                            meshRenderer.materials = outMaterials;
                            meshRenderer.lighting = mesh.lighting;
                        }
                    }
                }
            }
        }
        else if ((asset.meshes?.Count ?? 0) > 0)
        {
            for (var i = 0; i < asset.meshes.Count; i++)
            {
                var mesh = asset.meshes[i];

                var meshEntity = Entity.Create(mesh.name, typeof(Transform));

                var meshTransform = meshEntity.GetComponent<Transform>();

                var isSkinned = mesh.bones.Any(x => x.Length > 0);

                meshTransform.SetParent(baseTransform);

                var outMesh = ResourceManager.instance.LoadMesh($"{asset.Guid}:{i}", true);
                var outMaterials = mesh.submeshMaterialGuids.Select(x => ResourceManager.instance.LoadMaterial(x, true)).ToList();

                if (outMesh != null)
                {
                    if (isSkinned && options.HasFlag(MeshInstanceOptions.MakeUnskinned) == false)
                    {
                        var skinnedRenderer = meshEntity.AddComponent<SkinnedMeshRenderer>();

                        skinnedRenderer.mesh = outMesh;
                        skinnedRenderer.materials = outMaterials;
                        skinnedRenderer.lighting = mesh.lighting;
                    }
                    else
                    {
                        var meshRenderer = meshEntity.AddComponent<MeshRenderer>();

                        meshRenderer.mesh = outMesh;
                        meshRenderer.materials = outMaterials;
                        meshRenderer.lighting = mesh.lighting;
                    }
                }
            }
        }

        return baseEntity;
    }
}
