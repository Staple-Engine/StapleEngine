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

        internal static Matrix4x4 Projection(World world, Entity entity, Camera camera, Transform transform)
        {
            switch (camera.cameraType)
            {
                case CameraType.Perspective:

                    if (camera.nearPlane <= 0 || camera.farPlane <= 0 || camera.nearPlane >= camera.farPlane)
                    {
                        Log.Error($"{world.GetEntityName(entity)} camera component has invalid near/far plane parameters: {camera.nearPlane} / {camera.farPlane}");

                        return Matrix4x4.Identity;
                    }
                    return Matrix4x4.CreatePerspectiveFieldOfView(Math.Deg2Rad(camera.fov), camera.Width / camera.Height,
                        camera.nearPlane, camera.farPlane);

                case CameraType.Orthographic:

                    return Matrix4x4.CreateOrthographicOffCenter(0, camera.Width, camera.Height, 0, camera.nearPlane, camera.farPlane);

                default:

                    throw new System.ArgumentException("Camera Type is invalid", "cameraType");
            }
        }

        public static Vector3 ScreenPointToWorld(Vector2 point, World world, Entity entity, Camera camera, Transform transform)
        {
            var clipSpace = new Vector4(((point.X * 2.0f) / AppPlayer.ScreenWidth) - 1,
                (1.0f - (point.Y * 2.0f) / AppPlayer.ScreenHeight),
                0.0f, 1.0f);

            var p = Projection(world, entity, camera, transform);

            if (Matrix4x4.Invert(p, out var invP) == false)
            {
                return default;
            }

            var viewSpace = Vector4.Transform(clipSpace, invP);

            //We don't need to invert the world space matrix since it is already being inverted in the render system
            return Vector4.Transform(viewSpace, transform.Matrix).ToVector3();
        }

        public static Ray ScreenPointToRay(Vector2 point, World world, Entity entity, Camera camera, Transform transform)
        {
            var clipSpace = new Vector4(((point.X * 2.0f) / AppPlayer.ScreenWidth) - 1,
                (1.0f - (point.Y * 2.0f) / AppPlayer.ScreenHeight),
                0.0f, 1.0f);

            var p = Projection(world, entity, camera, transform);

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
