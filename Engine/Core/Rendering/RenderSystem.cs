
using Bgfx;
using System;
using System.Linq;
using System.Numerics;

namespace Staple
{
    internal class RenderSystem
    {
        private SpriteRenderSystem spriteRenderSystem = new SpriteRenderSystem();

        public bool Perform(Scene scene)
        {
            ushort viewID = 1;

            var cameras = scene.GetComponents<Camera>().OrderBy(x => x.depth);

            bool performed = false;

            foreach(var camera in cameras)
            {
                performed = true;

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

                            projection = Matrix4x4.CreateOrthographic(camera.Width, camera.Height, camera.nearPlane, camera.farPlane);

                            break;

                        default:
                            continue;
                    }

                    var view = camera.Transform.Matrix;

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
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), camera.clearColor.uintValue, 24, 0);

                        break;
                }

                bgfx.set_view_rect(viewID, (ushort)camera.viewport.X, (ushort)camera.viewport.Y,
                    (ushort)(camera.viewport.Z * AppPlayer.ScreenWidth), (ushort)(camera.viewport.W * AppPlayer.ScreenHeight));

                bgfx.touch(viewID);

                foreach(var entity in Scene.current.entities)
                {
                    if(camera.cullingLayers.HasLayer(entity.layer) && entity.TryGetComponent(out Renderer renderer))
                    {
                        if(renderer is SpriteRenderer)
                        {
                            spriteRenderSystem.Process(entity, (SpriteRenderer)renderer, viewID);
                        }
                    }
                }

                viewID++;
            }

            return performed;
        }
    }
}
