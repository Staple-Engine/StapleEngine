using Staple.Internal;
using System.Numerics;

namespace Staple;

/// <summary>
/// Camera component
/// </summary>
[ComponentIcon("Camera.png")]
public sealed class Camera : IComponent
{
    /// <summary>
    /// How to render elements of the camera
    /// </summary>
    public CameraViewMode viewMode = CameraViewMode.Default;

    /// <summary>
    /// How to clear the camera
    /// </summary>
    public CameraClearMode clearMode = CameraClearMode.SolidColor;

    /// <summary>
    /// The type of camera
    /// </summary>
    public CameraType cameraType = CameraType.Perspective;

    /// <summary>
    /// The camera's orthographic size, equal to half the screen size in world units
    /// </summary>
    [Min(1)]
    public float orthographicSize = 5;

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

    /// <summary>
    /// Actual width of this camera viewport
    /// </summary>
    internal float Width => viewport.Z * Screen.Width;

    /// <summary>
    /// Actual height of this camera viewport
    /// </summary>
    internal float Height => viewport.W * Screen.Height;

    /// <summary>
    /// The frustum culler for this camera
    /// </summary>
    private readonly FrustumCuller frustumCuller = new();

    /// <summary>
    /// Checks whether some bounds are visible for this camera
    /// </summary>
    /// <param name="bounds">The bounds to check</param>
    /// <returns>Whether it's visible</returns>
    public bool IsVisible(AABB bounds)
    {
        return frustumCuller.AABBTest(bounds) != FrustumResult.Invisible;
    }

    /// <summary>
    /// Updates the frustum for this camera
    /// </summary>
    /// <param name="view">The view matrix</param>
    /// <param name="projection">The projection matrix</param>
    internal void UpdateFrustum(Matrix4x4 view, Matrix4x4 projection)
    {
        frustumCuller.Update(view, projection);
    }

    /// <summary>
    /// Calculates projection matrix for a camera
    /// </summary>
    /// <param name="entity">The entity that the camera belongs to</param>
    /// <param name="camera">The camera</param>
    /// <returns>The matrix, or identity if failed</returns>
    internal static Matrix4x4 Projection(Entity entity, Camera camera)
    {
        switch (camera.cameraType)
        {
            case CameraType.Perspective:

                if (camera.nearPlane <= 0 || camera.farPlane <= 0 || camera.nearPlane >= camera.farPlane)
                {
                    Log.Error($"{entity} camera component has invalid near/far plane parameters: {camera.nearPlane} / {camera.farPlane}");

                    return Matrix4x4.Identity;
                }

                return Matrix4x4.CreatePerspectiveFieldOfView(Math.Deg2Rad * camera.fov, camera.Width / camera.Height,
                    camera.nearPlane, camera.farPlane);

            case CameraType.Orthographic:

                if(camera.orthographicSize < 1)
                {
                    Log.Error($"{entity} camera component has invalid orthographic size: {camera.orthographicSize}");

                    return Matrix4x4.Identity;
                }

                var scale = Screen.Height / (camera.orthographicSize * 2);

                var width = camera.Width / scale;
                var height = camera.Height / scale;

                return Matrix4x4.CreateOrthographic(width, height, camera.nearPlane, camera.farPlane);

            default:

                throw new System.ArgumentException("Camera Type is invalid", "cameraType");
        }
    }

    /// <summary>
    /// Converts a screen point to world coordinates
    /// </summary>
    /// <param name="point">The point</param>
    /// <param name="entity">The entity the camera belongs to</param>
    /// <param name="camera">The camera</param>
    /// <param name="transform">The camera's transform</param>
    /// <returns>A world-space point</returns>
    public static Vector3 ScreenPointToWorld(Vector2 point, Entity entity, Camera camera, Transform transform)
    {
        var clipSpace = new Vector4(((point.X * 2.0f) / Screen.Width) - 1,
            (1.0f - (point.Y * 2.0f) / Screen.Height),
            0.0f, 1.0f);

        var p = Projection(entity, camera);

        if (Matrix4x4.Invert(p, out var invP) == false)
        {
            return default;
        }

        var viewSpace = Vector4.Transform(clipSpace, invP);

        viewSpace.W = 1;

        //We don't need to invert the world space matrix since it is already being inverted in the render system
        return Vector4.Transform(viewSpace, transform.Matrix).ToVector3();
    }

    /// <summary>
    /// Converts a screen point to a ray
    /// </summary>
    /// <param name="point">The point</param>
    /// <param name="entity">The entity the camera belongs to</param>
    /// <param name="camera">The camera</param>
    /// <param name="transform">The camera's transform</param>
    /// <returns>The ray</returns>
    public static Ray ScreenPointToRay(Vector2 point, Entity entity, Camera camera, Transform transform)
    {
        var clipSpace = new Vector4(((point.X * 2.0f) / Screen.Width) - 1,
            (1.0f - (point.Y * 2.0f) / Screen.Height),
            0.0f, 1.0f);

        var p = Projection(entity, camera);

        if(Matrix4x4.Invert(p, out var invP) == false)
        {
            return new Ray();
        }

        var viewSpace = Vector4.Transform(clipSpace, invP);

        viewSpace.W = 1;

        //We don't need to invert the world space matrix since it is already being inverted in the render system
        var worldSpace = Vector4.Transform(viewSpace, transform.Matrix);

        return new Ray(transform.Position, Vector3.Normalize(worldSpace.ToVector3() - transform.Position));
    }
}
