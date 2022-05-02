using Bgfx;
using GlmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class Camera
    {
        public CameraClearMode clearMode = CameraClearMode.SolidColor;

        public CameraType cameraType = CameraType.Perspective;

        public float fov = 90;

        public float orthoSize = 5;

        public Vector4 viewport = new Vector4(0, 0, 1, 1);

        public ushort depth = 0;

        public Color32 clearColor;

        public void PrepareRender()
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

            bgfx.set_view_rect(depth, (ushort)viewport.x, (ushort)viewport.y, (ushort)(viewport.z * AppPlayer.ScreenWidth), (ushort)(viewport.w * AppPlayer.ScreenHeight));
        }
    }
}
