using Bgfx;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple
{
    internal class MeshRenderSystem : IRenderSystem
    {
        private struct RenderInfo
        {
            public MeshRenderer renderer;
            public Matrix4x4 transform;
            public ushort viewID;
        }

        private readonly List<RenderInfo> renderers = new();

        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material, ushort viewID)
        {
            if(mesh == null || material == null || material.Disposed || material.shader == null || material.shader.Disposed)
            {
                return;
            }

            if(mesh.changed)
            {
                mesh.UploadMeshData();
            }

            var matrix = new Transform()
            {
                Position = position,
                LocalRotation = rotation,
                LocalScale = scale,
            }.Matrix;

            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
                bgfx.StateFlags.WriteA |
                bgfx.StateFlags.WriteZ |
                bgfx.StateFlags.DepthTestLequal |
                mesh.PrimitiveFlag() |
                material.shader.BlendingFlag();

            unsafe
            {
                _ = bgfx.set_transform(&matrix, 1);
            }

            bgfx.set_state((ulong)state, 0);

            material.ApplyProperties();

            mesh.SetActive();

            bgfx.submit(viewID, material.shader.program, 0, (byte)bgfx.DiscardFlags.All);
        }

        public void Destroy()
        {
        }

        public void Prepare()
        {
            renderers.Clear();
        }

        public void Preprocess(World world, Entity entity, Transform transform, IComponent renderer)
        {
            var r = renderer as MeshRenderer;

            if (r.mesh == null || r.material == null || r.material.Disposed || r.material.shader == null || r.material.shader.Disposed)
            {
                return;
            }

            if(r.mesh.changed)
            {
                r.mesh.UploadMeshData();
            }

            r.localBounds = r.mesh.bounds;
            r.bounds = new AABB(transform.Position + r.mesh.bounds.center, r.mesh.bounds.extents * 2 * transform.Scale);
        }

        public void Process(World world, Entity entity, Transform transform, IComponent renderer, ushort viewId)
        {
            var r = renderer as MeshRenderer;

            if (r.mesh == null || r.material == null || r.material.Disposed || r.material.shader == null || r.material.shader.Disposed)
            {
                return;
            }

            renderers.Add(new RenderInfo()
            {
                renderer = r,
                transform = transform.Matrix,
                viewID = viewId,
            });
        }

        public Type RelatedComponent()
        {
            return typeof(MeshRenderer);
        }

        public void Submit()
        {
            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
                bgfx.StateFlags.WriteA |
                bgfx.StateFlags.WriteZ |
                bgfx.StateFlags.DepthTestLequal;

            foreach (var pair in renderers)
            {
                unsafe
                {
                    var transform = pair.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)(state | pair.renderer.mesh.PrimitiveFlag() | pair.renderer.material.shader.BlendingFlag()), 0);

                pair.renderer.material.ApplyProperties();

                pair.renderer.mesh.SetActive();

                bgfx.submit(pair.viewID, pair.renderer.material.shader.program, 0, (byte)bgfx.DiscardFlags.All);
            }
        }
    }
}