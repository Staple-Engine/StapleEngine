using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// 3D data container (Mesh)
/// </summary>
public sealed partial class Mesh : IGuidAsset
{
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
                colors32 = null;
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
                return meshDataBlob.Length / meshDataVertexLayout.layout.stride;
            }

            return vertices?.Length ?? 0;
        }
    }

    /// <summary>
    /// Total amount of indices
    /// </summary>
    public int IndexCount => indices?.Length ?? 0;

    internal string guid;

    /// <summary>
    /// Asset GUID
    /// </summary>
    public string Guid
    {
        get => guid;

        set => guid = value;
    }

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
    }

    /// <summary>
    /// Clears all data in this mesh
    /// </summary>
    public void Clear()
    {
        vertices = null;
        normals = null;
        colors = null;
        colors32 = null;
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
    public void SetMeshData(byte[] meshData, VertexLayout vertexLayout)
    {
        if(meshData == null || vertexLayout == null)
        {
            throw new Exception("Mesh Data or Vertex Layout are null");
        }

        if(meshData.Length % vertexLayout.layout.stride != 0)
        {
            throw new Exception($"Mesh Data Blob has misaligned data (has {meshData.Length} bytes, should be multiples of {vertexLayout.layout.stride})");
        }

        meshDataBlob = meshData;
        meshDataVertexLayout = vertexLayout;

        if(vertexBuffer != null && vertexBuffer.Disposed == false && isDynamic)
        {
            vertexBuffer.Update(meshData, 0);
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
    public void SetMeshData<T>(T[] meshData, VertexLayout vertexLayout) where T : unmanaged
    {
        if (meshData == null || vertexLayout == null)
        {
            throw new Exception("Mesh Data or Vertex Layout are null");
        }

        var size = Marshal.SizeOf<T>();

        if(size != vertexLayout.layout.stride)
        {
            throw new Exception($"Mesh Data element size does not equal vertex layout size (has: {size}, needs: {vertexLayout.layout.stride})");
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
            vertexBuffer.Update<T>(meshData, 0);
        }
        else
        {
            changed = true;
        }
    }

    /// <summary>
    /// Uploads the mesh data to the GPU
    /// </summary>
    public void UploadMeshData()
    {
        if(changed == false)
        {
            return;
        }

        changed = false;

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

            case MeshTopology.Points:

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

        if(meshDataBlob != null && meshDataBlob.Length % layout.layout.stride != 0)
        {
            Log.Error($"[Mesh] Mesh Data Blob has misaligned data (has {meshDataBlob.Length} bytes, should be multiples of {layout.layout.stride})");

            return;
        }

        var vertexBlob = meshDataBlob ?? MakeVertexDataBlob(layout);

        if(vertexBlob == null)
        {
            return;
        }

        vertexBuffer = isDynamic ? VertexBuffer.CreateDynamic(layout) : VertexBuffer.Create(vertexBlob, layout);

        if(vertexBuffer == null)
        {
            return;
        }

        if(vertexBuffer.type == VertexBufferType.Dynamic)
        {
            vertexBuffer.Update(vertexBlob, 0);
        }

        switch (indexFormat)
        {
            case MeshIndexFormat.UInt16:

                {
                    ushort[] data = new ushort[indices.Length];

                    for (var i = 0; i < indices.Length; i++)
                    {
                        if (indices[i] >= ushort.MaxValue)
                        {
                            throw new InvalidOperationException($"[Mesh] Invalid value {indices[i]} for 16-bit indices");
                        }

                        data[i] = (ushort)indices[i];
                    }

                    indexBuffer = IndexBuffer.Create(data, RenderBufferFlags.None);
                }

                break;

            case MeshIndexFormat.UInt32:

                {
                    uint[] data = new uint[indices.Length];

                    for (var i = 0; i < indices.Length; i++)
                    {
                        data[i] = (uint)indices[i];
                    }

                    indexBuffer = IndexBuffer.Create(data, RenderBufferFlags.Index32);
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

            var normal = Vector3.Cross(p2 - p0, p1 - p0);

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
            normals[i] = Vector3.Normalize(normals[i]);
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

            var normal = Vector3.Cross(p2 - p0, p1 - p0);

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
            normals[i] = Vector3.Normalize(normals[i]);
        }

        return normals;
    }
}
