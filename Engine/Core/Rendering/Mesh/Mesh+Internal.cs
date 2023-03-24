using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Staple
{
    public partial class Mesh
    {
        internal bool changed;
        internal Vector3[] vertices;
        internal Vector3[] normals;
        internal Vector4[] tangents;
        internal Vector4[] bitangents;
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
        internal VertexBuffer vertexBuffer;
        internal IndexBuffer indexBuffer;

        internal static Dictionary<string, VertexLayout> vertexLayouts = new Dictionary<string, VertexLayout>();

        internal bool hasNormals => (normals?.Length ?? 0) > 0;
        internal bool hasTangents => (tangents?.Length ?? 0) > 0;
        internal bool hasBitangents => (tangents?.Length ?? 0) > 0;
        internal bool hasColors => (colors?.Length ?? 0) > 0;
        internal bool hasColors32 => (colors32?.Length ?? 0) > 0;
        internal bool hasUV => (uv?.Length ?? 0) > 0;
        internal bool hasUV2 => (uv2?.Length ?? 0) > 0;
        internal bool hasUV3 => (uv3?.Length ?? 0) > 0;
        internal bool hasUV4 => (uv4?.Length ?? 0) > 0;
        internal bool hasUV5 => (uv5?.Length ?? 0) > 0;
        internal bool hasUV6 => (uv6?.Length ?? 0) > 0;
        internal bool hasUV7 => (uv7?.Length ?? 0) > 0;
        internal bool hasUV8 => (uv8?.Length ?? 0) > 0;

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

            if (mesh.hasNormals)
            {
                keyBuilder.Append("n");
            }

            if(mesh.hasTangents)
            {
                keyBuilder.Append("t");
            }

            if(mesh.hasBitangents)
            {
                keyBuilder.Append("bt");
            }

            if (mesh.hasColors)
            {
                keyBuilder.Append("c");
            }

            if (mesh.hasColors32)
            {
                keyBuilder.Append("c32");
            }

            if (mesh.hasUV)
            {
                keyBuilder.Append("u");
            }

            if (mesh.hasUV2)
            {
                keyBuilder.Append("u2");
            }

            if (mesh.hasUV3)
            {
                keyBuilder.Append("u3");
            }

            if (mesh.hasUV4)
            {
                keyBuilder.Append("u4");
            }

            if (mesh.hasUV5)
            {
                keyBuilder.Append("u5");
            }

            if (mesh.hasUV6)
            {
                keyBuilder.Append("u6");
            }

            if (mesh.hasUV7)
            {
                keyBuilder.Append("u7");
            }

            if (mesh.hasUV8)
            {
                keyBuilder.Append("u8");
            }

            var key = keyBuilder.ToString();

            if(vertexLayouts.TryGetValue(key, out var layout) && layout != null)
            {
                return layout;
            }

            var builder = new VertexLayoutBuilder();

            builder.Add(Bgfx.bgfx.Attrib.Position, 3, Bgfx.bgfx.AttribType.Float);

            if(mesh.hasNormals)
            {
                builder.Add(Bgfx.bgfx.Attrib.Normal, 3, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasTangents)
            {
                builder.Add(Bgfx.bgfx.Attrib.Tangent, 4, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasBitangents)
            {
                builder.Add(Bgfx.bgfx.Attrib.Bitangent, 4, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasColors || mesh.hasColors32)
            {
                builder.Add(Bgfx.bgfx.Attrib.Color0, 4, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasUV)
            {
                builder.Add(Bgfx.bgfx.Attrib.TexCoord0, 2, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasUV2)
            {
                builder.Add(Bgfx.bgfx.Attrib.TexCoord1, 2, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasUV3)
            {
                builder.Add(Bgfx.bgfx.Attrib.TexCoord2, 2, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasUV4)
            {
                builder.Add(Bgfx.bgfx.Attrib.TexCoord3, 2, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasUV5)
            {
                builder.Add(Bgfx.bgfx.Attrib.TexCoord4, 2, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasUV6)
            {
                builder.Add(Bgfx.bgfx.Attrib.TexCoord5, 2, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasUV7)
            {
                builder.Add(Bgfx.bgfx.Attrib.TexCoord6, 2, Bgfx.bgfx.AttribType.Float);
            }

            if (mesh.hasUV8)
            {
                builder.Add(Bgfx.bgfx.Attrib.TexCoord7, 2, Bgfx.bgfx.AttribType.Float);
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
                if(hasNormals)
                {
                    Copy(BitConverter.GetBytes(normals[i].X), ref index);
                    Copy(BitConverter.GetBytes(normals[i].Y), ref index);
                    Copy(BitConverter.GetBytes(normals[i].Z), ref index);
                }

                if(hasTangents)
                {
                    Copy(BitConverter.GetBytes(tangents[i].X), ref index);
                    Copy(BitConverter.GetBytes(tangents[i].Y), ref index);
                    Copy(BitConverter.GetBytes(tangents[i].Z), ref index);
                    Copy(BitConverter.GetBytes(tangents[i].W), ref index);
                }

                if (hasBitangents)
                {
                    Copy(BitConverter.GetBytes(bitangents[i].X), ref index);
                    Copy(BitConverter.GetBytes(bitangents[i].Y), ref index);
                    Copy(BitConverter.GetBytes(bitangents[i].Z), ref index);
                    Copy(BitConverter.GetBytes(bitangents[i].W), ref index);
                }

                if(hasColors)
                {
                    Copy(BitConverter.GetBytes(colors[i].r), ref index);
                    Copy(BitConverter.GetBytes(colors[i].g), ref index);
                    Copy(BitConverter.GetBytes(colors[i].b), ref index);
                    Copy(BitConverter.GetBytes(colors[i].a), ref index);
                }
                else if(hasColors32)
                {
                    var c = (Color)colors32[i];

                    Copy(BitConverter.GetBytes(c.r), ref index);
                    Copy(BitConverter.GetBytes(c.g), ref index);
                    Copy(BitConverter.GetBytes(c.b), ref index);
                    Copy(BitConverter.GetBytes(c.a), ref index);
                }

                if(hasUV)
                {
                    Copy(BitConverter.GetBytes(uv[i].X), ref index);
                    Copy(BitConverter.GetBytes(uv[i].Y), ref index);
                }

                if (hasUV2)
                {
                    Copy(BitConverter.GetBytes(uv2[i].X), ref index);
                    Copy(BitConverter.GetBytes(uv2[i].Y), ref index);
                }

                if (hasUV3)
                {
                    Copy(BitConverter.GetBytes(uv3[i].X), ref index);
                    Copy(BitConverter.GetBytes(uv3[i].Y), ref index);
                }

                if (hasUV4)
                {
                    Copy(BitConverter.GetBytes(uv4[i].X), ref index);
                    Copy(BitConverter.GetBytes(uv4[i].Y), ref index);
                }

                if (hasUV5)
                {
                    Copy(BitConverter.GetBytes(uv5[i].X), ref index);
                    Copy(BitConverter.GetBytes(uv5[i].Y), ref index);
                }

                if (hasUV6)
                {
                    Copy(BitConverter.GetBytes(uv6[i].X), ref index);
                    Copy(BitConverter.GetBytes(uv6[i].Y), ref index);
                }

                if (hasUV7)
                {
                    Copy(BitConverter.GetBytes(uv7[i].X), ref index);
                    Copy(BitConverter.GetBytes(uv7[i].Y), ref index);
                }

                if (hasUV8)
                {
                    Copy(BitConverter.GetBytes(uv8[i].X), ref index);
                    Copy(BitConverter.GetBytes(uv8[i].Y), ref index);
                }
            }

            return buffer;
        }

        internal bool SetActive()
        {
            if(vertexBuffer == null || indexBuffer == null)
            {
                return false;
            }

            vertexBuffer.SetActive(0, 0, (uint)vertices.Length);
            indexBuffer.SetActive(0, (uint)indices.Length);

            return true;
        }
    }
}