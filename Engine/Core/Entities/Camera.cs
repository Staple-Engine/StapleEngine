using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class Camera : Component
    {
        public CameraClearMode clearMode = CameraClearMode.SolidColor;

        public CameraType cameraType = CameraType.Perspective;

        public float fov = 90;

        public float nearPlane = 0.01f;

        public float farPlane = 1000;

        public Vector4 viewport = new Vector4(0, 0, 1, 1);

        public ushort depth = 0;

        public Color32 clearColor;

        internal Camera(Entity entity) : base(entity)
        {
        }

        internal float Width => viewport.Z * AppPlayer.ScreenWidth;

        internal float Height => viewport.W * AppPlayer.ScreenHeight;

        public Matrix4x4 ProjectionMatrix
        {
            get
            {
                switch (cameraType)
                {
                    case CameraType.Perspective:

                        return Matrix4x4.CreatePerspectiveFieldOfView(Math.Deg2Rad(fov), Width / Height,
                            nearPlane, farPlane);

                    case CameraType.Orthographic:

                        return Matrix4x4.CreateOrthographic(Width, Height, nearPlane, farPlane);

                    default:
                        return Matrix4x4.Identity;
                }
            }
        }

        public void PrepareRender(ushort depth)
        {
            switch(clearMode)
            {
                case CameraClearMode.Depth:
                    bgfx.set_view_clear(depth, (ushort)(bgfx.ClearFlags.Depth), 0, 24, 0);

                    break;

                case CameraClearMode.None:
                    bgfx.set_view_clear(depth, (ushort)(bgfx.ClearFlags.None), 0, 24, 0);

                    break;

                case CameraClearMode.SolidColor:
                    bgfx.set_view_clear(depth, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.uintValue, 24, 0);

                    break;
            }

            bgfx.set_view_rect(depth, (ushort)viewport.X, (ushort)viewport.Y, (ushort)(viewport.Z * AppPlayer.ScreenWidth), (ushort)(viewport.W * AppPlayer.ScreenHeight));
        }
    }
}
