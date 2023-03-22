using System.Numerics;

namespace Staple
{
    [DisallowMultipleComponent]
    public class Camera : IComponent
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

        public Matrix4x4 Projection
        {
            get
            {
                switch (cameraType)
                {
                    case CameraType.Perspective:

                        return Matrix4x4.CreatePerspectiveFieldOfView(Math.Deg2Rad(fov), Width / Height,
                            nearPlane, farPlane);

                    case CameraType.Orthographic:

                        return Matrix4x4.CreateOrthographicOffCenter(0, Width, Height, 0, nearPlane, farPlane);
                }

                return Matrix4x4.Identity;
            }
        }

        public Vector3 ScreenPointToWorld(Vector2 point, Transform transform)
        {
            var clipSpace = new Vector4(((point.X * 2.0f) / AppPlayer.ScreenWidth) - 1,
                (1.0f - (point.Y * 2.0f) / AppPlayer.ScreenHeight),
                0.0f, 1.0f);

            var p = Projection;

            if (Matrix4x4.Invert(p, out var invP) == false)
            {
                return default;
            }

            var viewSpace = Vector4.Transform(clipSpace, invP);

            //We don't need to invert the world space matrix since it is already being inverted in the render system
            return Vector4.Transform(viewSpace, transform.Matrix).ToVector3();
        }

        public Ray ScreenPointToRay(Vector2 point, Transform transform)
        {
            var clipSpace = new Vector4(((point.X * 2.0f) / AppPlayer.ScreenWidth) - 1,
                (1.0f - (point.Y * 2.0f) / AppPlayer.ScreenHeight),
                0.0f, 1.0f);

            var p = Projection;

            if(Matrix4x4.Invert(p, out var invP) == false)
            {
                return new Ray();
            }

            var viewSpace = Vector4.Transform(clipSpace, invP);

            //We don't need to invert the world space matrix since it is already being inverted in the render system
            var worldSpace = Vector4.Transform(viewSpace, transform.Matrix);

            return new Ray(transform.Position, Vector3.Normalize(worldSpace.ToVector3() - transform.Position));
        }
    }
}
