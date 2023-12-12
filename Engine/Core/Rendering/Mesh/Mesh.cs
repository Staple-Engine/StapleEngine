using Staple.Internal;
using System;
using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Mesh Resource
    /// </summary>
    public partial class Mesh : IPathAsset
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

                indices = new int[0];
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
                if(isReadable == false)
                {
                    return new Vector3[0];
                }

                return vertices ?? new Vector3[0];
            }

            set
            {
                if(isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector3[0];
                }

                return normals ?? new Vector3[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector3[0];
                }

                return tangents ?? new Vector3[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector3[0];
                }

                return bitangents ?? new Vector3[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Color[0];
                }

                return colors ?? new Color[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Color32[0];
                }

                return colors32 ?? new Color32[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector2[0];
                }

                return uv ?? new Vector2[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector2[0];
                }

                return uv2 ?? new Vector2[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector2[0];
                }

                return uv3 ?? new Vector2[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector2[0];
                }

                return uv4 ?? new Vector2[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector2[0];
                }

                return uv5 ?? new Vector2[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector2[0];
                }

                return uv6 ?? new Vector2[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector2[0];
                }

                return uv7 ?? new Vector2[0];
            }

            set
            {
                if (isWritable == false)
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
                if (isReadable == false)
                {
                    return new Vector2[0];
                }

                return uv8 ?? new Vector2[0];
            }

            set
            {
                if (isWritable == false)
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
        /// Sets or gets the current UVs for channel 0.
        /// Getting depends on isReadable.
        /// </summary>
        public int[] Indices
        {
            get
            {
                if (isReadable == false)
                {
                    return new int[0];
                }

                return indices ?? new int[0];
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
        public int VertexCount => vertices?.Length ?? 0;

        /// <summary>
        /// Total amount of indices
        /// </summary>
        public int IndexCount => indices?.Length ?? 0;

        internal string path;

        public string Path
        {
            get => path;

            set => path = value;
        }

        public static object Create(string path)
        {
            return ResourceManager.instance.LoadMesh(path);
        }

        public Mesh() { }

        public void Clear()
        {
            vertices = null;
            normals = null;
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

            changed = true;

            vertexBuffer?.Destroy();
            indexBuffer?.Destroy();

            vertexBuffer = null;
            indexBuffer = null;
        }

        public void UploadMeshData()
        {
            changed = false;

            vertexBuffer?.Destroy();
            indexBuffer?.Destroy();

            vertexBuffer = null;
            indexBuffer = null;

            if (vertices == null || vertices.Length == 0)
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

            var layout = GetVertexLayout(this);

            if(layout == null)
            {
                Log.Error($"[Mesh] Failed to get vertex layout for this mesh!");

                return;
            }

            var vertexBlob = MakeVertexDataBlob(layout);

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

            bounds = AABB.FromPoints(vertices);
        }

        public void MarkDynamic()
        {
            isDynamic = true;

            changed = true;

            vertexBuffer?.Destroy();
            indexBuffer?.Destroy();

            vertexBuffer = null;
            indexBuffer = null;
        }
    }
}
