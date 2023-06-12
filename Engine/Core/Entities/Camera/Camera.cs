using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Camera component
    /// </summary>
    public class Camera : IComponent
    {
        /// <summary>
        /// How to clear the camera
        /// </summary>
        public CameraClearMode clearMode = CameraClearMode.SolidColor;

        /// <summary>
        /// The type of camera
        /// </summary>
        public CameraType cameraType = CameraType.Perspective;

        /// <summary>
        /// The field of view
        /// </summary>
        public float fov = 90;

        /// <summary>
        /// The near plane. You probably want a near plane of 0 for Orthographic cameras.
        /// </summary>
        public float nearPlane = 0.01f;

        /// <summary>
        /// The far plane
        /// </summary>
        public float farPlane = 1000;

        /// <summary>
        /// The camera viewport (X: x, Y: y, Z: width, W: height)
        /// Width and Height are normalized
        /// </summary>
        public Vector4 viewport = new(0, 0, 1, 1);

        /// <summary>
        /// The camera depth. Cameras are sorted by depth, from lower to higher.
        /// </summary>
        public ushort depth = 0;

        /// <summary>
        /// The clear color for the camera
        /// </summary>
        public Color32 clearColor;

        /// <summary>
        /// The layers this camera handles
        /// </summary>
        public LayerMask cullingLayers = LayerMask.Everything;

        internal float Width => viewport.Z * AppPlayer.ScreenWidth;

        internal float Height => viewport.W * AppPlayer.ScreenHeight;

        internal static Matrix4x4 Projection(World world, Entity entity, Camera camera)
        {
            switch (camera.cameraType)
            {
                case CameraType.Perspective:

                    if (camera.nearPlane <= 0 || camera.farPlane <= 0 || camera.nearPlane >= camera.farPlane)
                    {
                        Log.Error($"{world?.GetEntityName(entity)} camera component has invalid near/far plane parameters: {camera.nearPlane} / {camera.farPlane}");

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

        /// <summary>
        /// Converts a screen point to world coordinates
        /// </summary>
        /// <param name="point">The point</param>
        /// <param name="world">The world the camera belongs to</param>
        /// <param name="entity">The entity the camera belongs to</param>
        /// <param name="camera">The camera</param>
        /// <param name="transform">The camera's transform</param>
        /// <returns>A world-space point</returns>
        public static Vector3 ScreenPointToWorld(Vector2 point, World world, Entity entity, Camera camera, Transform transform)
        {
            var clipSpace = new Vector4(((point.X * 2.0f) / AppPlayer.ScreenWidth) - 1,
                (1.0f - (point.Y * 2.0f) / AppPlayer.ScreenHeight),
                0.0f, 1.0f);

            var p = Projection(world, entity, camera);

            if (Matrix4x4.Invert(p, out var invP) == false)
            {
                return default;
            }

            var viewSpace = Vector4.Transform(clipSpace, invP);

            //We don't need to invert the world space matrix since it is already being inverted in the render system
            return Vector4.Transform(viewSpace, transform.Matrix).ToVector3();
        }

        /// <summary>
        /// Converts a screen point to a ray
        /// </summary>
        /// <param name="point">The point</param>
        /// <param name="world">The world the camera belongs to</param>
        /// <param name="entity">The entity the camera belongs to</param>
        /// <param name="camera">The camera</param>
        /// <param name="transform">The camera's transform</param>
        /// <returns>The ray</returns>
        public static Ray ScreenPointToRay(Vector2 point, World world, Entity entity, Camera camera, Transform transform)
        {
            var clipSpace = new Vector4(((point.X * 2.0f) / AppPlayer.ScreenWidth) - 1,
                (1.0f - (point.Y * 2.0f) / AppPlayer.ScreenHeight),
                0.0f, 1.0f);

            var p = Projection(world, entity, camera);

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
