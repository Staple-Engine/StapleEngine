using Staple.Internal;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// Camera component
/// </summary>
[ComponentIcon("Camera.png")]
public partial class Camera : VersionableComponent, IComponentDisposable
{
    /// <summary>
    /// Contains information on a spatial partition entry
    /// </summary>
    internal class SpatialNodeInfo
    {
        /// <summary>
        /// Frames until we need to recalculate the culling result
        /// </summary>
        public int framesTillRecalculation;

        /// <summary>
        /// The last culling result since the last recalculation
        /// </summary>
        public CullingState lastCullResult;

        /// <summary>
        /// Whether the node needs to be updated
        /// </summary>
        public bool needsUpdate = true;
    }

    /// <summary>
    /// How to render elements of the camera
    /// </summary>
    public partial CameraViewMode viewMode { get; set; }

    /// <summary>
    /// How to clear the camera
    /// </summary>
    public partial CameraClearMode clearMode { get; set; }

    /// <summary>
    /// The type of camera
    /// </summary>
    public partial CameraType cameraType { get; set; }

    /// <summary>
    /// The camera's orthographic size, equal to half the screen size in world units
    /// </summary>
    [Min(1)]
    public partial float orthographicSize { get; set; }

    /// <summary>
    /// The field of view
    /// </summary>
    public partial float fov { get; set; }

    /// <summary>
    /// The near plane. You probably want a near plane of 0 for Orthographic cameras.
    /// </summary>
    public partial float nearPlane { get; set; }

    /// <summary>
    /// The far plane
    /// </summary>
    public partial float farPlane { get; set; }

    /// <summary>
    /// The camera viewport (X: x, Y: y, Z: width, W: height)
    /// Width and Height are normalized
    /// </summary>
    public partial Vector4 viewport { get; set; }

    /// <summary>
    /// The camera depth. Cameras are sorted by depth, from lower to higher.
    /// </summary>
    public partial float depth { get; set; }

    /// <summary>
    /// The clear color for the camera
    /// </summary>
    public partial Color32 clearColor { get; set; }

    /// <summary>
    /// The layers this camera handles
    /// </summary>
    public partial LayerMask cullingLayers { get; set; }

    /// <summary>
    /// Actual width of this camera viewport
    /// </summary>
    internal float Width => viewport.Z * Screen.RenderTargetWidth;

    /// <summary>
    /// Actual height of this camera viewport
    /// </summary>
    internal float Height => viewport.W * Screen.RenderTargetHeight;

    /// <summary>
    /// The frustum culler for this camera
    /// </summary>
    internal readonly FrustumCuller frustumCuller = new();

    /// <summary>
    /// Contains information on spatial nodes for this particular camera
    /// </summary>
    internal readonly Dictionary<Vector3Int, SpatialNodeInfo> spatialNodes = [];

    internal ulong transformVersion = 0;

    internal ulong cameraVersion = 0;

    internal VertexBuffer lightBuffer;

    public Camera()
    {
        viewMode = CameraViewMode.Default;
        clearMode = CameraClearMode.SolidColor;
        cameraType = CameraType.Perspective;
        orthographicSize = 5;
        fov = 90;
        nearPlane = 0.01f;
        farPlane = 1000;
        viewport = new(0, 0, 1, 1);
        clearColor = Color32.LightBlue;
        cullingLayers = LayerMask.Everything;
    }

    public void DisposeComponent()
    {
        lightBuffer?.Destroy();

        lightBuffer = null;
    }

    /// <summary>
    /// Compares this camera's current transform version with another transform to see if changed
    /// </summary>
    /// <param name="cameraTransform">The camera transform</param>
    /// <returns>Whether it changed</returns>
    /// <remarks>This doesn't use <see cref="Component.Transform"/> because of the editor not using a normal entity</remarks>
    internal bool Changed(Transform cameraTransform)
    {
        var version = cameraTransform.Version;

        if(version != transformVersion)
        {
            transformVersion = version;

            return true;
        }

        version = Version;

        if(version != cameraVersion)
        {
            cameraVersion = version;

            return true;
        }

        return false;
    }

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

                    var scale = Screen.RenderTargetHeight / (orthographicSize * 2);

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
    public bool IsVisible(in AABB bounds)
    {
        return frustumCuller.AABBTest(bounds) != FrustumResult.Invisible;
    }

    /// <summary>
    /// Updates the frustum for this camera
    /// </summary>
    /// <param name="view">The view matrix</param>
    /// <param name="projection">The projection matrix</param>
    internal void UpdateFrustum(in Matrix4x4 view, in Matrix4x4 projection)
    {
        frustumCuller.Update(in view, in projection);
    }

    /// <summary>
    /// Calculates projection matrix for a camera
    /// </summary>
    /// <param name="camera">The camera</param>
    /// <returns>The matrix, or identity if failed</returns>
    internal static Matrix4x4 Projection(Camera camera)
    {
        switch (camera.cameraType)
        {
            case CameraType.Perspective:

                if (camera.nearPlane <= 0 || camera.farPlane <= 0 || camera.nearPlane >= camera.farPlane)
                {
                    Log.Error($"{camera.Entity} camera component has invalid near/far plane parameters: {camera.nearPlane} / {camera.farPlane}");

                    return Matrix4x4.Identity;
                }

                return Matrix4x4.CreatePerspectiveFieldOfViewLeftHandedReverseZ(camera.fov, camera.Width / camera.Height,
                    camera.nearPlane, camera.farPlane);

            case CameraType.Orthographic:

                if(camera.orthographicSize < 1)
                {
                    Log.Error($"{camera.Entity} camera component has invalid orthographic size: {camera.orthographicSize}");

                    return Matrix4x4.Identity;
                }

                var scale = Screen.RenderTargetHeight / (camera.orthographicSize * 2);

                var width = camera.Width / scale;
                var height = camera.Height / scale;

                return Matrix4x4.CreateOrthographicLeftHanded(width, height, camera.nearPlane, camera.farPlane);

            default:

                throw new System.ArgumentException("Camera Type is invalid", nameof(cameraType));
        }
    }

    /// <summary>
    /// Calculates projection matrix for a camera
    /// </summary>
    /// <param name="entity">The entity that the camera belongs to</param>
    /// <param name="camera">The camera</param>
    /// <param name="size">The specific screen size we're targeting</param>
    /// <returns>The matrix, or identity if failed</returns>
    internal static Matrix4x4 ProjectionCustomSize(Camera camera, Vector2Int size)
    {
        switch (camera.cameraType)
        {
            case CameraType.Perspective:

                if (camera.nearPlane <= 0 || camera.farPlane <= 0 || camera.nearPlane >= camera.farPlane)
                {
                    Log.Error($"{camera.Entity} camera component has invalid near/far plane parameters: {camera.nearPlane} / {camera.farPlane}");

                    return Matrix4x4.Identity;
                }

                return Matrix4x4.CreatePerspectiveFieldOfViewLeftHandedReverseZ(camera.fov, size.X / (float)size.Y, camera.nearPlane, camera.farPlane);

            case CameraType.Orthographic:

                if (camera.orthographicSize < 1)
                {
                    Log.Error($"{camera.Entity} camera component has invalid orthographic size: {camera.orthographicSize}");

                    return Matrix4x4.Identity;
                }

                var scale = size.Y / (camera.orthographicSize * 2);

                var width = size.X / scale;
                var height = size.Y / scale;

                return Matrix4x4.CreateOrthographicLeftHanded(width, height, camera.nearPlane, camera.farPlane);

            default:

                throw new System.ArgumentException("Camera Type is invalid", nameof(cameraType));
        }
    }

    /// <summary>
    /// Converts a screen point to world coordinates
    /// </summary>
    /// <param name="point">The point</param>
    /// <param name="camera">The camera</param>
    /// <param name="transform">The camera's transform</param>
    /// <returns>A world-space point</returns>
    public static Vector3 ScreenPointToWorld(Vector2 point, Camera camera, Transform transform)
    {
        var clipSpace = new Vector4(((point.X * 2.0f) / Screen.RenderTargetWidth) - 1,
            (1.0f - (point.Y * 2.0f) / Screen.RenderTargetHeight),
            0.0f, 1.0f);

        var p = Projection(camera);

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
    /// <param name="camera">The camera</param>
    /// <param name="transform">The camera's transform</param>
    /// <returns>The ray</returns>
    public static Ray ScreenPointToRay(Vector2 point, Camera camera, Transform transform)
    {
        var clipSpace = new Vector4(((point.X * 2.0f) / Screen.RenderTargetWidth) - 1,
            (1.0f - (point.Y * 2.0f) / Screen.RenderTargetHeight),
            0.0f, 1.0f);

        var p = Projection(camera);

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

    /// <summary>
    /// Checks if a spatial node is visible (for partitioning frustum culling checks on larger scenes)
    /// </summary>
    /// <param name="coordinate">The coordinate to check</param>
    /// <param name="force">Whether to force the check</param>
    /// <returns>The culling state of the node</returns>
    internal CullingState IsSpatialNodeVisible(Vector3Int coordinate, bool force)
    {
        var checkAnyway = force;

        if(!spatialNodes.TryGetValue(coordinate, out var node))
        {
            node = new()
            {
                framesTillRecalculation = spatialNodes.Count,
            };

            spatialNodes.Add(coordinate, node);
        }

        var shouldCheck = checkAnyway || node.needsUpdate;

        if(!shouldCheck)
        {
            return node.lastCullResult;
        }

        var aabb = RenderSystem.MakeSpatialAABB(coordinate);

        node.needsUpdate = false;
        node.lastCullResult = IsVisible(aabb) ? CullingState.Visible : CullingState.Invisible;

        return node.lastCullResult;
    }

    /// <summary>
    /// Called when the frame starts
    /// </summary>
    internal void OnStartFrame()
    {
        foreach(var pair in spatialNodes)
        {
            ref var frames = ref pair.Value.framesTillRecalculation;

            if (frames > 0)
            {
                frames--;
            }

            if (frames == 0)
            {
                frames = RenderSystem.MaxFramesBetwenSpatialRecalculation;

                pair.Value.needsUpdate = true;
            }
        }
    }
}
