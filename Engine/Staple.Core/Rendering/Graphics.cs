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
        /// <param name="startIndex">The start index</param>
        /// <param name="indexCount">The amount of indices to draw</param>
        /// <param name="material">The material to use</param>
        /// <param name="transform">The transform for the model</param>
        /// <param name="topology">The geometry topology</param>
        /// <param name="lighting">What kind of lighting to apply</param>
        /// <param name="viewID">The bgfx view ID to render to</param>
        /// <param name="materialSetupCallback">A callback to setup the material. If it's not set, the default behaviour will be used</param>
        public static void RenderGeometry(VertexBuffer vertex, IndexBuffer index,
            int startVertex, int startIndex, int indexCount, Material material,
            Vector3 position, Matrix4x4 transform, MeshTopology topology, MaterialLighting lighting, ushort viewID,
            Action materialSetupCallback = null)
        {
            if(vertex == null ||
                vertex.Disposed ||
                index == null ||
                index.Disposed ||
                startVertex < 0 || 
                startIndex < 0 ||
                indexCount <= 0 ||
                material == null ||
                material.IsValid == false)
            {
                throw new Exception("Invalid arguments passed");
            }

            var renderState = new RenderState()
            {
                cull = material.CullingMode,
                primitiveType = topology,
                depthWrite = true,
                enableDepth = true,
                indexBuffer = index,
                vertexBuffer = vertex,
                startVertex = startVertex,
                startIndex = startIndex,
                indexCount = indexCount,
                world = transform,
            };

            if(materialSetupCallback != null)
            {
                materialSetupCallback();
            }
            else
            {
                material.ApplyProperties(Material.ApplyMode.All, ref renderState);

                material.DisableShaderKeyword(Shader.SkinningKeyword);

                material.DisableShaderKeyword(Shader.InstancingKeyword);
            }

            var lightSystem = RenderSystem.Instance.Get<LightSystem>();

            lightSystem?.ApplyMaterialLighting(material, lighting);

            var program = material.ShaderProgram;

            if(program != null)
            {
                renderState.program = program;

                lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position, lighting);

                RenderSystem.Submit(viewID, renderState, Mesh.TriangleCount(topology, indexCount), 1);
            }
        }

        public static void RenderSimple<T>(Span<T> vertices, VertexLayout layout, Span<ushort> indices, Material material, Vector3 position,
            Matrix4x4 transform, MeshTopology topology, MaterialLighting lighting, ushort viewID, Action materialSetupCallback = null) where T: unmanaged
        {
            if (vertices.Length == 0||
                indices.Length == 0 ||
                material == null ||
                material.IsValid == false)
            {
                throw new Exception("Invalid arguments passed");
            }

            /*
            bgfx.discard((byte)bgfx.DiscardFlags.All);

            var vertexBuffer = VertexBuffer.CreateTransient(vertices, layout);
            var indexBuffer = IndexBuffer.CreateTransient(indices);

            if(vertexBuffer == null || indexBuffer == null)
            {
                return;
            }

            unsafe
            {
                _ = bgfx.set_transform(&transform, 1);
            }

            bgfx.StateFlags state = material.shader.StateFlags |
                (bgfx.StateFlags)topology |
                material.CullingFlag;

            bgfx.set_state((ulong)state, 0);

            vertexBuffer.SetActive(0, 0, (uint)vertices.Length);
            indexBuffer.SetActive(0, (uint)indices.Length);

            if (materialSetupCallback != null)
            {
                materialSetupCallback();
            }
            else
            {
                material.ApplyProperties(Material.ApplyMode.All);

                material.DisableShaderKeyword(Shader.SkinningKeyword);

                material.DisableShaderKeyword(Shader.InstancingKeyword);
            }

            var lightSystem = RenderSystem.Instance.Get<LightSystem>();

            lightSystem?.ApplyMaterialLighting(material, lighting);

            var program = material.ShaderProgram;

            if (program.Valid)
            {
                lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position, lighting);

                RenderSystem.Submit(viewID, program, bgfx.DiscardFlags.All, Mesh.TriangleCount(topology, indices.Length), 1);
            }
            else
            {
                bgfx.discard((byte)bgfx.DiscardFlags.All);
            }
            */
        }

        public static void RenderSimple<T>(Span<T> vertices, VertexLayout layout, Span<uint> indices, Material material, Vector3 position,
            Matrix4x4 transform, MeshTopology topology, MaterialLighting lighting, ushort viewID, Action materialSetupCallback = null) where T : unmanaged
        {
            if (vertices.Length == 0 ||
                indices.Length == 0 ||
                material == null ||
                material.IsValid == false)
            {
                throw new Exception("Invalid arguments passed");
            }

            /*
            bgfx.discard((byte)bgfx.DiscardFlags.All);

            var vertexBuffer = VertexBuffer.CreateTransient(vertices, layout);
            var indexBuffer = IndexBuffer.CreateTransient(indices);

            if (vertexBuffer == null || indexBuffer == null)
            {
                return;
            }

            unsafe
            {
                _ = bgfx.set_transform(&transform, 1);
            }

            bgfx.StateFlags state = material.shader.StateFlags |
                (bgfx.StateFlags)topology |
                material.CullingFlag;

            bgfx.set_state((ulong)state, 0);

            vertexBuffer.SetActive(0, 0, (uint)vertices.Length);
            indexBuffer.SetActive(0, (uint)indices.Length);

            if (materialSetupCallback != null)
            {
                materialSetupCallback();
            }
            else
            {
                material.ApplyProperties(Material.ApplyMode.All);

                material.DisableShaderKeyword(Shader.SkinningKeyword);

                material.DisableShaderKeyword(Shader.InstancingKeyword);
            }

            var lightSystem = RenderSystem.Instance.Get<LightSystem>();

            lightSystem?.ApplyMaterialLighting(material, lighting);

            var program = material.ShaderProgram;

            if (program.Valid)
            {
                lightSystem?.ApplyLightProperties(material, RenderSystem.CurrentCamera.Item2.Position, lighting);

                RenderSystem.Submit(viewID, program, bgfx.DiscardFlags.All, Mesh.TriangleCount(topology, indices.Length), 1);
            }
            else
            {
                bgfx.discard((byte)bgfx.DiscardFlags.All);
            }
            */
        }
    }
}
