using Staple.Internal;
using System.Collections.Generic;
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
    public Color32 clearColor = Color32.LightBlue;

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
    /// Gets the camera's frustum corners
    /// </summary>
    /// <param name="cameraTransform">The transform of the camera</param>
    /// <returns>The 8 frustum corners of the camera</returns>
    public Vector3[] Corners(Transform cameraTransform)
    {
        switch(cameraType)
        {
            case CameraType.Orthographic:

                {
                    var orthographicSize = this.orthographicSize < 1 ? 1 : this.orthographicSize;

                    var scale = Screen.Height / (orthographicSize * 2);

                    var width = Width / scale;
                    var height = Height / scale;

                    var halfWidth = width * 0.5f;
                    var halfHeight = height * 0.5f;
                    var halfDepth = (farPlane - nearPlane) * 0.5f;

                    var z = cameraTransform.Forward;
                    var x = Vector3.Up.Cross(z).Normalized;
                    var y = z.Cross(x).Normalized;

                    var center = cameraTransform.Position + z * (nearPlane + halfDepth);

                    return [
                        center + (y * halfHeight) - (x * halfWidth) - (z * halfDepth),
                        center + (y * halfHeight) + (x * halfWidth) - (z * halfDepth),
                        center - (y * halfHeight) - (x * halfWidth) - (z * halfDepth),
                        center - (y * halfHeight) + (x * halfWidth) - (z * halfDepth),

                        center + (y * halfHeight) - (x * halfWidth) + (z * halfDepth),
                        center + (y * halfHeight) + (x * halfWidth) + (z * halfDepth),
                        center - (y * halfHeight) - (x * halfWidth) + (z * halfDepth),
                        center - (y * halfHeight) + (x * halfWidth) + (z * halfDepth),
                    ];
                }

            case CameraType.Perspective:

                {
                    var nearHeight = 2 * Math.Tan(Math.Deg2Rad * fov * 0.5f) * nearPlane;
                    var nearWidth = nearHeight * Width / Height;

                    var farHeight = 2 * Math.Tan(Math.Deg2Rad * fov * 0.5f) * farPlane;
                    var farWidth = farHeight * Width / Height;

                    var z = cameraTransform.Forward;
                    var x = Vector3.Up.Cross(z).Normalized;
                    var y = z.Cross(x).Normalized;

                    var nearCenter = cameraTransform.Position + z * nearPlane;
                    var farCenter = cameraTransform.Position + z * farPlane;

                    return [
                        nearCenter + (y * (nearHeight / 2)) - (x * (nearWidth / 2)),
                        nearCenter + (y * (nearHeight / 2)) + (x * (nearWidth / 2)),
                        nearCenter - (y * (nearHeight / 2)) - (x * (nearWidth / 2)),
                        nearCenter - (y * (nearHeight / 2)) + (x * (nearWidth / 2)),
                        farCenter + (y * (farHeight / 2)) - (x * (farWidth / 2)),
                        farCenter + (y * (farHeight / 2)) + (x * (farWidth / 2)),
                        farCenter - (y * (farHeight / 2)) - (x * (farWidth / 2)),
                        farCenter - (y * (farHeight / 2)) + (x * (farWidth / 2)),
                    ];
                }

            default:

                return [];
        }
    }

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

                return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Math.Deg2Rad * camera.fov, camera.Width / camera.Height,
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

                return Matrix4x4.CreateOrthographicLeftHanded(width, height, camera.nearPlane, camera.farPlane);

            default:

                throw new System.ArgumentException("Camera Type is invalid", nameof(cameraType));
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

        if (!Matrix4x4.Invert(p, out var invP))
        {
            return default;
        }

        var viewSpace = clipSpace.Transformed(invP);

        viewSpace.W = 1;

        //We don't need to invert the world space matrix since it is already being inverted in the render system
        return viewSpace.Transformed(transform.Matrix).ToVector3();
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

        if(!Matrix4x4.Invert(p, out var invP))
        {
            return new Ray();
        }

        var viewSpace = clipSpace.Transformed(invP);

        viewSpace.W = 1;

        //We don't need to invert the world space matrix since it is already being inverted in the render system
        var worldSpace = viewSpace.Transformed(transform.Matrix);

        return new Ray(transform.Position, (worldSpace.ToVector3() - transform.Position).Normalized);
    }
}
