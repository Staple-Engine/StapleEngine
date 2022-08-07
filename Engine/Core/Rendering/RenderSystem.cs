
using Bgfx;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple
{
    internal class RenderSystem : ISubsystem
    {
        private SpriteRenderSystem spriteRenderSystem = new SpriteRenderSystem();

        internal static byte Priority = 0;

        public static ulong BlendFunction(bgfx.StateFlags source, bgfx.StateFlags destination)
        {
            return BlendFunction(source, destination, source, destination);
        }

        public static ulong BlendFunction(bgfx.StateFlags sourceColor, bgfx.StateFlags destinationColor, bgfx.StateFlags sourceAlpha, bgfx.StateFlags destinationAlpha)
        {
            return ((ulong)sourceColor | ((ulong)destinationColor << 4)) | (((ulong)sourceAlpha | ((ulong)destinationAlpha << 4)) << 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckAvailableTransientBuffers(uint _numVertices, bgfx.VertexLayout layout, uint _numIndices)
        {
            unsafe
            {
                return _numVertices == bgfx.get_avail_transient_vertex_buffer(_numVertices, &layout)
                    && (0 == _numIndices || _numIndices == bgfx.get_avail_transient_index_buffer(_numIndices, false));
            }
        }

        public void Startup()
        {
        }

        public void Shutdown()
        {
            spriteRenderSystem.Destroy();
        }

        public void Update()
        {
            ushort viewID = 1;

            var cameras = Scene.current.GetComponents<Camera>().OrderBy(x => x.depth);

            foreach (var camera in cameras)
            {
                unsafe
                {
                    Matrix4x4 projection;

                    switch (camera.cameraType)
                    {
                        case CameraType.Perspective:

                            projection = Matrix4x4.CreatePerspectiveFieldOfView(Math.Deg2Rad(camera.fov), camera.Width / camera.Height,
                                camera.nearPlane, camera.farPlane);

                            break;

                        case CameraType.Orthographic:

                            projection = Matrix4x4.CreateOrthographicOffCenter(0, camera.Width, camera.Height, 0, camera.nearPlane, camera.farPlane);

                            break;

                        default:
                            continue;
                    }

                    var view = camera.Transform.Matrix;

                    Matrix4x4.Invert(view, out view);

                    bgfx.set_view_transform(viewID, &view, &projection);
                }

                switch (camera.clearMode)
                {
                    case CameraClearMode.Depth:
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Depth), 0, 24, 0);

                        break;

                    case CameraClearMode.None:
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.None), 0, 24, 0);

                        break;

                    case CameraClearMode.SolidColor:
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), camera.clearColor.uintValue, 0, 0);

                        break;
                }

                bgfx.set_view_rect(viewID, (ushort)camera.viewport.X, (ushort)camera.viewport.Y,
                    (ushort)(camera.viewport.Z * AppPlayer.ScreenWidth), (ushort)(camera.viewport.W * AppPlayer.ScreenHeight));

                bgfx.touch(viewID);

                foreach (var entity in Scene.current.entities)
                {
                    if (camera.cullingLayers.HasLayer(entity.layer) && entity.TryGetComponent(out Renderer renderer) && renderer.enabled)
                    {
                        if (renderer is SpriteRenderer)
                        {
                            spriteRenderSystem.Process(entity, (SpriteRenderer)renderer, viewID);
                        }
                    }
                }

                viewID++;
            }
        }
    }
}
