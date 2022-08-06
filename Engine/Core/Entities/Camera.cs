using System.Numerics;

namespace Staple
{
    [DisallowMultipleComponent]
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

        public LayerMask cullingLayers = LayerMask.Everything;

        internal float Width => viewport.Z * AppPlayer.ScreenWidth;

        internal float Height => viewport.W * AppPlayer.ScreenHeight;

        public Ray ScreenPointToRay(Vector2 point)
        {
            return new Ray(new Vector3(point.X, point.Y, 0), Transform.Forward);
        }
    }
}
