using Bgfx;
using Staple.Internal;
using System;
using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Low level graphics class
    /// </summary>
    public static class Graphics
    {
        /// <summary>
        /// Renders Geometry using a vertex buffer, index buffer, and material
        /// </summary>
        /// <param name="vertex">The vertex buffer</param>
        /// <param name="index">The index buffer</param>
        /// <param name="startVertex">The starting vertex</param>
        /// <param name="vertexCount">The amount of vertexes to draw</param>
        /// <param name="startIndex">The start index</param>
        /// <param name="indexCount">The amount of indices to draw</param>
        /// <param name="material">The material to use</param>
        /// <param name="transform">The transform for the model</param>
        /// <param name="topology">The geometry topology</param>
        /// <param name="viewID">The bgfx view ID to render to</param>
        /// <param name="materialSetupCallback">A callback to setup the material. If it's not set, the default behaviour will be used</param>
        public static void RenderGeometry(VertexBuffer vertex, IndexBuffer index,
            int startVertex, int vertexCount, int startIndex, int indexCount, Material material,
            Matrix4x4 transform, MeshTopology topology, ushort viewID, Action materialSetupCallback = null)
        {
            if(vertex == null ||
                vertex.Disposed ||
                index == null ||
                index.Disposed ||
                startVertex < 0 || 
                startIndex < 0 ||
                vertexCount <= 0 ||
                indexCount <= 0 ||
                material == null ||
                material.IsValid == false)
            {
                throw new Exception("Invalid arguments passed");
            }

            unsafe
            {
                _ = bgfx.set_transform(&transform, 1);
            }

            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
                bgfx.StateFlags.WriteA |
                bgfx.StateFlags.WriteZ |
                bgfx.StateFlags.DepthTestLequal |
                (bgfx.StateFlags)topology |
                material.shader.BlendingFlag();

            bgfx.set_state((ulong)state, 0);

            vertex.SetActive(0, (uint)startVertex, (uint)vertexCount);
            index.SetActive((uint)startIndex, (uint)indexCount);

            if(materialSetupCallback != null)
            {
                materialSetupCallback();
            }
            else
            {
                material.ApplyProperties();

                material.DisableShaderKeyword(Shader.SkinningKeyword);
            }

            var program = material.ShaderProgram;

            if(program.Valid)
            {
                bgfx.submit(viewID, program, 0, (byte)bgfx.DiscardFlags.All);
            }
            else
            {
                bgfx.discard((byte)bgfx.DiscardFlags.All);
            }
        }
        public static void RenderSimple<T>(Span<T> vertices, VertexLayout layout, ushort[] indices, Material material, Matrix4x4 transform,
            MeshTopology topology, ushort viewID, Action materialSetupCallback = null) where T: unmanaged
        {
            if (vertices.Length == 0||
                indices.Length == 0 ||
                material == null ||
                material.IsValid == false)
            {
                throw new Exception("Invalid arguments passed");
            }

            var vertexBuffer = VertexBuffer.Create(vertices, layout, true);
            var indexBuffer = IndexBuffer.Create(indices, RenderBufferFlags.Read, true);

            if(vertexBuffer == null || indexBuffer == null)
            {
                return;
            }

            unsafe
            {
                _ = bgfx.set_transform(&transform, 1);
            }

            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
                bgfx.StateFlags.WriteA |
                bgfx.StateFlags.WriteZ |
                bgfx.StateFlags.DepthTestLequal |
                (bgfx.StateFlags)topology |
                material.shader.BlendingFlag();

            bgfx.set_state((ulong)state, 0);

            vertexBuffer.SetActive(0, 0, (uint)vertices.Length);
            indexBuffer.SetActive(0, (uint)indices.Length);

            if (materialSetupCallback != null)
            {
                materialSetupCallback();
            }
            else
            {
                material.ApplyProperties();

                material.DisableShaderKeyword(Shader.SkinningKeyword);
            }

            var program = material.ShaderProgram;

            if (program.Valid)
            {
                bgfx.submit(viewID, program, 0, (byte)bgfx.DiscardFlags.All);
            }
            else
            {
                bgfx.discard((byte)bgfx.DiscardFlags.All);
            }
        }
    }
}
