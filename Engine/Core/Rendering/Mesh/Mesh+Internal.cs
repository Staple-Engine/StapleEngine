using Bgfx;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Staple
{
    public partial class Mesh
    {
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
        internal MeshIndexFormat indexFormat = MeshIndexFormat.UInt16;
        internal MeshTopology meshTopology = MeshTopology.TriangleStrip;
        internal VertexBuffer vertexBuffer;
        internal IndexBuffer indexBuffer;

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

        internal static readonly Dictionary<string, Mesh> defaultMeshes = new();

        private static Mesh _quad;

        internal static Mesh Quad
        {
            get
            {
                if(_quad == null)
                {
                    _quad = new Mesh(false, false)
                    {
                        vertices = new Vector3[]
                        {
                            new Vector3(-0.5f, -0.5f, 0),
                            new Vector3(-0.5f, 0.5f, 0),
                            new Vector3(0.5f, 0.5f, 0),
                            new Vector3(0.5f, -0.5f, 0),
                        },

                        uv = new Vector2[]
                        {
                            new Vector2(0, 1),
                            Vector2.Zero,
                            new Vector2(1, 0),
                            Vector2.One,
                        },

                        indices = new int[]
                        {
                            0, 1, 2, 2, 3, 0
                        },
                    };

                    _quad.UploadMeshData();

                    _quad.guid = "Internal/Quad";

                    defaultMeshes.Add(_quad.guid, _quad);
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
                    _cube = new Mesh(false, false)
                    {
                        vertices = new Vector3[]
                        {
                            new Vector3(-0.5f, 0.5f, 0.5f),
                            Vector3.One * 0.5f,
                            new Vector3(-0.5f, -0.5f, 0.5f),
                            new Vector3(0.5f, -0.5f, 0.5f),
                            new Vector3(-0.5f, 0.5f, -0.5f),
                            new Vector3(0.5f, 0.5f, -0.5f),
                            Vector3.One * -0.5f,
                            new Vector3(0.5f, -0.5f, -0.5f),
                        },

                        indices = new int[]
                        {
                            0, 1, 2,
                            3, 7, 1,
                            5, 0, 4,
                            2, 6, 7,
                            4, 5
                        },
                    };

                    _cube.UploadMeshData();

                    _cube.guid = "Internal/Cube";

                    defaultMeshes.Add(_cube.guid, _cube);
                }

                return _cube;
            }
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
                builder.Add(bgfx.Attrib.Tangent, 4, bgfx.AttribType.Float);
            }

            if (mesh.HasBitangents)
            {
                builder.Add(bgfx.Attrib.Bitangent, 4, bgfx.AttribType.Float);
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
                    throw new InvalidOperationException("$[Mesh] Exceeded expected byte count while generating vertex data blob");
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
            }

            return buffer;
        }

        internal bool SetActive()
        {
            if(changed)
            {
                UploadMeshData();
            }

            if(vertexBuffer == null || indexBuffer == null)
            {
                return false;
            }

            vertexBuffer.SetActive(0, 0, (uint)vertices.Length);
            indexBuffer.SetActive(0, (uint)indices.Length);

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
}