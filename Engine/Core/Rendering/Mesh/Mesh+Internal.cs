using Bgfx;
using Staple.Utilities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Staple;

public sealed partial class Mesh
{
    internal class SubmeshInfo
    {
        public int startVertex;
        public int vertexCount;
        public int startIndex;
        public int indexCount;
        public MeshTopology topology;
    }

    internal bool changed;
    internal Vector3[] vertices;
    internal Vector3[] normals;
    internal Vector3[] tangents;
    internal Vector3[] bitangents;
    internal Color[] colors;
    internal Color32[] colors32;
    internal Vector2[] uv;
    internal Vector2[] uv2;
    internal Vector2[] uv3;
    internal Vector2[] uv4;
    internal Vector2[] uv5;
    internal Vector2[] uv6;
    internal Vector2[] uv7;
    internal Vector2[] uv8;
    internal int[] indices;
    internal Vector4[] boneIndices;
    internal Vector4[] boneWeights;
    internal MeshIndexFormat indexFormat = MeshIndexFormat.UInt16;
    internal MeshTopology meshTopology = MeshTopology.TriangleStrip;
    internal VertexBuffer vertexBuffer;
    internal IndexBuffer indexBuffer;

    internal byte[] meshDataBlob = null;
    internal VertexLayout meshDataVertexLayout = null;

    internal MeshAsset meshAsset;
    internal int meshAssetIndex;

    internal List<SubmeshInfo> submeshes = new();

    internal static Dictionary<string, VertexLayout> vertexLayouts = new();

    internal bool HasNormals => (normals?.Length ?? 0) > 0;
    internal bool HasTangents => (tangents?.Length ?? 0) > 0;
    internal bool HasBitangents => (tangents?.Length ?? 0) > 0;
    internal bool HasColors => (colors?.Length ?? 0) > 0;
    internal bool HasColors32 => (colors32?.Length ?? 0) > 0;
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

    internal static readonly Dictionary<string, Mesh> defaultMeshes = new()
    {
        { "Internal/Quad", Quad },
        { "Internal/Cube", Cube },
        { "Internal/Sphere", Sphere },
    };

    private static Mesh _quad;
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

                _quad.guid = "Internal/Quad";

                _quad.UpdateBounds();
            }

            return _quad;
        }
    }

    private static Mesh _cube;
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

                _cube.guid = "Internal/Cube";

                _cube.UpdateBounds();
            }

            return _cube;
        }
    }

    private static Mesh _sphere;
    internal static Mesh Sphere
    {
        get
        {
            if (_sphere == null)
            {
                _sphere = GenerateSphere(36, 18, 0.5f);

                _sphere.guid = "Internal/Sphere";

                _sphere.UpdateBounds();
            }

            return _sphere;
        }
    }

    internal static Mesh GenerateSphere(int sectorCount, int stackCount, float radius)
    {
        //Based on https://www.songho.ca/opengl/gl_sphere.html
        var outValue = new Mesh(false, false)
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
                    indices.Add(k1 + 1);
                    indices.Add(k2);
                    indices.Add(k1);
                }

                if (i != (stackCount - 1))
                {
                    indices.Add(k2 + 1);
                    indices.Add(k2);
                    indices.Add(k1 + 1);
                }
            }
        }

        outValue.vertices = vertices.ToArray();
        outValue.normals = normals.ToArray();
        outValue.uv = texCoords.ToArray();
        outValue.indices = indices.ToArray();

        return outValue;
    }

    internal bool isDynamic = false;

    internal Mesh(bool readable, bool writable)
    {
        isReadable = readable;
        isWritable = writable;
    }

    internal bgfx.StateFlags PrimitiveFlag()
    {
        return (bgfx.StateFlags)meshTopology;
    }

    internal void Destroy()
    {
        vertexBuffer?.Destroy();
        indexBuffer?.Destroy();

        vertexBuffer = null;
        indexBuffer = null;
    }

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

        if (mesh.HasColors32)
        {
            keyBuilder.Append("c32");
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

        var builder = new VertexLayoutBuilder();

        builder.Add(bgfx.Attrib.Position, 3, bgfx.AttribType.Float);

        if(mesh.HasNormals)
        {
            builder.Add(bgfx.Attrib.Normal, 3, bgfx.AttribType.Float);
        }

        if (mesh.HasTangents)
        {
            builder.Add(bgfx.Attrib.Tangent, 3, bgfx.AttribType.Float);
        }

        if (mesh.HasBitangents)
        {
            builder.Add(bgfx.Attrib.Bitangent, 3, bgfx.AttribType.Float);
        }

        if (mesh.HasColors || mesh.HasColors32)
        {
            builder.Add(bgfx.Attrib.Color0, 4, bgfx.AttribType.Float);
        }

        if (mesh.HasUV)
        {
            builder.Add(bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float);
        }

        if (mesh.HasUV2)
        {
            builder.Add(bgfx.Attrib.TexCoord1, 2, bgfx.AttribType.Float);
        }

        if (mesh.HasUV3)
        {
            builder.Add(bgfx.Attrib.TexCoord2, 2, bgfx.AttribType.Float);
        }

        if (mesh.HasUV4)
        {
            builder.Add(bgfx.Attrib.TexCoord3, 2, bgfx.AttribType.Float);
        }

        if (mesh.HasUV5)
        {
            builder.Add(bgfx.Attrib.TexCoord4, 2, bgfx.AttribType.Float);
        }

        if (mesh.HasUV6)
        {
            builder.Add(bgfx.Attrib.TexCoord5, 2, bgfx.AttribType.Float);
        }

        if (mesh.HasUV7)
        {
            builder.Add(bgfx.Attrib.TexCoord6, 2, bgfx.AttribType.Float);
        }

        if (mesh.HasUV8)
        {
            builder.Add(bgfx.Attrib.TexCoord7, 2, bgfx.AttribType.Float);
        }

        if(mesh.HasBoneIndices)
        {
            builder.Add(bgfx.Attrib.Indices, 4, bgfx.AttribType.Float, false, false);
        }

        if(mesh.HasBoneWeights)
        {
            builder.Add(bgfx.Attrib.Weight, 4, bgfx.AttribType.Float, false, false);
        }

        layout = builder.Build();

        if(layout != null)
        {
            vertexLayouts.Add(key, layout);
        }

        return layout;
    }

    internal byte[] MakeVertexDataBlob(VertexLayout layout)
    {
        var size = layout.layout.stride * vertices.Length;

        var buffer = new byte[size];

        void Copy(byte[] source, ref int index)
        {
            if(index + source.Length > buffer.Length)
            {
                throw new InvalidOperationException($"[Mesh] Buffer Overrun while generating vertex data blob: {index} -> {index + source.Length} "
                    + $"is larger than buffer {buffer.Length}");
            }

            Buffer.BlockCopy(source, 0, buffer, index, source.Length);

            index += source.Length;
        }

        for (int i = 0, index = 0; i < vertices.Length; i++)
        {
            if(index % layout.layout.stride != 0)
            {
                throw new InvalidOperationException("[Mesh] Exceeded expected byte count while generating vertex data blob");
            }

            //Copy position
            Copy(BitConverter.GetBytes(vertices[i].X), ref index);
            Copy(BitConverter.GetBytes(vertices[i].Y), ref index);
            Copy(BitConverter.GetBytes(vertices[i].Z), ref index);

            //Copy normals
            if(HasNormals)
            {
                Copy(BitConverter.GetBytes(normals[i].X), ref index);
                Copy(BitConverter.GetBytes(normals[i].Y), ref index);
                Copy(BitConverter.GetBytes(normals[i].Z), ref index);
            }

            if(HasTangents)
            {
                Copy(BitConverter.GetBytes(tangents[i].X), ref index);
                Copy(BitConverter.GetBytes(tangents[i].Y), ref index);
                Copy(BitConverter.GetBytes(tangents[i].Z), ref index);
            }

            if (HasBitangents)
            {
                Copy(BitConverter.GetBytes(bitangents[i].X), ref index);
                Copy(BitConverter.GetBytes(bitangents[i].Y), ref index);
                Copy(BitConverter.GetBytes(bitangents[i].Z), ref index);
            }

            if(HasColors)
            {
                Copy(BitConverter.GetBytes(colors[i].r), ref index);
                Copy(BitConverter.GetBytes(colors[i].g), ref index);
                Copy(BitConverter.GetBytes(colors[i].b), ref index);
                Copy(BitConverter.GetBytes(colors[i].a), ref index);
            }
            else if(HasColors32)
            {
                var c = (Color)colors32[i];

                Copy(BitConverter.GetBytes(c.r), ref index);
                Copy(BitConverter.GetBytes(c.g), ref index);
                Copy(BitConverter.GetBytes(c.b), ref index);
                Copy(BitConverter.GetBytes(c.a), ref index);
            }

            if(HasUV)
            {
                Copy(BitConverter.GetBytes(uv[i].X), ref index);
                Copy(BitConverter.GetBytes(uv[i].Y), ref index);
            }

            if (HasUV2)
            {
                Copy(BitConverter.GetBytes(uv2[i].X), ref index);
                Copy(BitConverter.GetBytes(uv2[i].Y), ref index);
            }

            if (HasUV3)
            {
                Copy(BitConverter.GetBytes(uv3[i].X), ref index);
                Copy(BitConverter.GetBytes(uv3[i].Y), ref index);
            }

            if (HasUV4)
            {
                Copy(BitConverter.GetBytes(uv4[i].X), ref index);
                Copy(BitConverter.GetBytes(uv4[i].Y), ref index);
            }

            if (HasUV5)
            {
                Copy(BitConverter.GetBytes(uv5[i].X), ref index);
                Copy(BitConverter.GetBytes(uv5[i].Y), ref index);
            }

            if (HasUV6)
            {
                Copy(BitConverter.GetBytes(uv6[i].X), ref index);
                Copy(BitConverter.GetBytes(uv6[i].Y), ref index);
            }

            if (HasUV7)
            {
                Copy(BitConverter.GetBytes(uv7[i].X), ref index);
                Copy(BitConverter.GetBytes(uv7[i].Y), ref index);
            }

            if (HasUV8)
            {
                Copy(BitConverter.GetBytes(uv8[i].X), ref index);
                Copy(BitConverter.GetBytes(uv8[i].Y), ref index);
            }

            if(HasBoneIndices)
            {
                Copy(BitConverter.GetBytes(boneIndices[i].X), ref index);
                Copy(BitConverter.GetBytes(boneIndices[i].Y), ref index);
                Copy(BitConverter.GetBytes(boneIndices[i].Z), ref index);
                Copy(BitConverter.GetBytes(boneIndices[i].W), ref index);
            }

            if(HasBoneWeights)
            {
                Copy(BitConverter.GetBytes(boneWeights[i].X), ref index);
                Copy(BitConverter.GetBytes(boneWeights[i].Y), ref index);
                Copy(BitConverter.GetBytes(boneWeights[i].Z), ref index);
                Copy(BitConverter.GetBytes(boneWeights[i].W), ref index);
            }
        }

        return buffer;
    }

    internal bool SetActive(int submeshIndex = 0)
    {
        if(changed)
        {
            UploadMeshData();
        }

        if(vertexBuffer == null || indexBuffer == null)
        {
            return false;
        }

        if(submeshes.Count == 0)
        {
            vertexBuffer.SetActive(0, 0, (uint)VertexCount);
            indexBuffer.SetActive(0, (uint)indices.Length);
        }
        else if(submeshIndex >= 0 && submeshIndex < submeshes.Count)
        {
            var submesh = submeshes[submeshIndex];

            vertexBuffer.SetActive(0, (uint)submesh.startVertex, (uint)submesh.vertexCount);
            indexBuffer.SetActive((uint)submesh.startIndex, (uint)submesh.indexCount);
        }
        else
        {
            return false;
        }

        return true;
    }

    internal static Mesh GetDefaultMesh(string path)
    {
        if(defaultMeshes.TryGetValue(path, out var mesh))
        {
            return mesh;
        }

        return null;
    }
}
