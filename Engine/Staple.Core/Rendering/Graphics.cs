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
        /// <param name="disableLighting">Whether to disable lighting</param>
        /// <param name="materialSetupCallback">A callback to setup the material. If it's not set, the default behaviour will be used</param>
        public static void RenderGeometry(VertexBuffer vertex, IndexBuffer index, int startVertex, int startIndex, int indexCount,
            Material material, Matrix4x4 transform, MeshTopology topology, bool disableLighting = false, Action materialSetupCallback = null)
        {
            if(vertex == null ||
                vertex.Disposed ||
                index == null ||
                index.Disposed ||
                startVertex < 0 || 
                startIndex < 0 ||
                indexCount <= 0 ||
                material == null ||
                !material.IsValid)
            {
                return;
            }

            var renderState = RenderState.Default;

            renderState.cull = material.CullingMode;
            renderState.primitiveType = topology;
            renderState.indexBuffer = index;
            renderState.vertexBuffer = vertex;
            renderState.startVertex = startVertex;
            renderState.startIndex = startIndex;
            renderState.indexCount = indexCount;
            renderState.world = transform;

            if(materialSetupCallback != null)
            {
                materialSetupCallback();
            }
            else
            {
                material.DisableShaderKeyword(Shader.SkinningKeyword);

                material.ApplyProperties(ref renderState);
            }

            if (material.ShaderProgram == null)
            {
                return;
            }

            LightSystem.Instance.ApplyLightProperties(material, RenderSystem.CurrentCamera.transform?.Position ?? default, disableLighting);

            RenderSystem.Submit(renderState, Mesh.TriangleCount(topology, indexCount), 1);
        }

        /// <summary>
        /// Renders geometry through transient geometry
        /// </summary>
        /// <typeparam name="T">The vertex type</typeparam>
        /// <param name="vertices">The vertices</param>
        /// <param name="layout">The vertex layout</param>
        /// <param name="indices">The indices</param>
        /// <param name="material">The material to use</param>
        /// <param name="transform">The transform matrix</param>
        /// <param name="topology">The mesh topology</param>
        /// <param name="disableLighting">Whether to disable lighting</param>
        /// <param name="materialSetupCallback">An optional callback to adjust the material setup</param>
        public static void RenderTransient<T>(Span<T> vertices, VertexLayout layout, Span<ushort> indices, Material material,
            Matrix4x4 transform, MeshTopology topology, bool disableLighting = false, Action materialSetupCallback = null)
            where T: unmanaged
        {
            if (vertices.Length == 0||
                indices.Length == 0 ||
                material == null ||
                !material.IsValid)
            {
                return;
            }

            var renderState = RenderState.Default;

            renderState.primitiveType = topology;
            renderState.world = transform;

            if (materialSetupCallback != null)
            {
                materialSetupCallback();
            }
            else
            {
                material.DisableShaderKeyword(Shader.SkinningKeyword);

                material.ApplyProperties(ref renderState);
            }

            if (material.ShaderProgram == null)
            {
                return;
            }

            LightSystem.Instance.ApplyLightProperties(material, RenderSystem.CurrentCamera.transform?.Position ?? default, disableLighting);

            RenderSystem.Backend.RenderTransient(vertices, layout, indices, renderState);
        }

        /// <summary>
        /// Renders geometry through transient geometry
        /// </summary>
        /// <typeparam name="T">The vertex type</typeparam>
        /// <param name="vertices">The vertices</param>
        /// <param name="layout">The vertex layout</param>
        /// <param name="indices">The indices</param>
        /// <param name="material">The material to use</param>
        /// <param name="transform">The transform matrix</param>
        /// <param name="topology">The mesh topology</param>
        /// <param name="disableLighting">Whether to disable lighting</param>
        /// <param name="materialSetupCallback">An optional callback to adjust the material setup</param>
        public static void RenderTransient<T>(Span<T> vertices, VertexLayout layout, Span<uint> indices, Material material,
            Matrix4x4 transform, MeshTopology topology, bool disableLighting = false, Action materialSetupCallback = null)
            where T : unmanaged
        {
            if (vertices.Length == 0 ||
                indices.Length == 0 ||
                material == null ||
                !material.IsValid)
            {
                return;
            }

            var renderState = RenderState.Default;

            renderState.primitiveType = topology;
            renderState.world = transform;

            if (materialSetupCallback != null)
            {
                materialSetupCallback();
            }
            else
            {
                material.DisableShaderKeyword(Shader.SkinningKeyword);

                material.ApplyProperties(ref renderState);
            }

            if (material.ShaderProgram == null)
            {
                return;
            }

            LightSystem.Instance.ApplyLightProperties(material, RenderSystem.CurrentCamera.transform?.Position ?? default, disableLighting);

            RenderSystem.Backend.RenderTransient(vertices, layout, indices, renderState);
        }
    }
}
