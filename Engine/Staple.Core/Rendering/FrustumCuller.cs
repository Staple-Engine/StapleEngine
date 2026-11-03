using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple.Internal;

enum FrustumResult
{
    Visible,
    Intersecting,
    Invisible
}

/// <summary>
/// Frustum Culling calculator
/// </summary>
internal class FrustumCuller
{
    /// <summary>
    /// The frustum planes
    /// </summary>
    internal readonly Vector3[] planes = new Vector3[6];
    /// <summary>
    /// The distances for the planes
    /// </summary>
    internal readonly float[] distances = new float[6];
    /// <summary>
    /// The planes combined into Vector4's
    /// </summary>
    internal readonly Vector4[] combinedPlanes = new Vector4[6];

    /// <summary>
    /// Updates the frustum culler's planes
    /// </summary>
    /// <param name="view">The view matrix</param>
    /// <param name="projection">The projection matrix</param>
    public void Update(in Matrix4x4 view, in Matrix4x4 projection)
    {
        var clip = view * projection;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void HandlePlane(float A, float B, float C, float D, float NA, float NB, float NC, float ND, ref Vector3 plane,
            ref float distance, ref Vector4 combined)
        {
            var vector = new Vector3(A + NA, B + NB, C + NC);

            var magnitude = vector.Length();

            var normalized = vector.Normalized;

            plane.X = combined.X = normalized.X;
            plane.Y = combined.Y = normalized.Y;
            plane.Z = combined.Z = normalized.Z;
            distance = combined.W = (D + ND) / magnitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void HandlePlaneSimple(float A, float B, float C, float D, ref Vector3 plane, ref float distance, ref Vector4 combined)
        {
            var vector = new Vector3(A, B, C);

            var magnitude = vector.Length();

            var normalized = vector.Normalized;

            plane.X = combined.X = normalized.X;
            plane.Y = combined.Y = normalized.Y;
            plane.Z = combined.Z = normalized.Z;
            distance = combined.W = D / magnitude;
        }

        //Left
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            clip.M11, clip.M21, clip.M31, clip.M41, ref planes[0], ref distances[0], ref combinedPlanes[0]);

        //Right
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            -clip.M11, -clip.M21, -clip.M31, -clip.M41, ref planes[1], ref distances[1], ref combinedPlanes[1]);

        //Top
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            -clip.M12, -clip.M22, -clip.M32, -clip.M42, ref planes[2], ref distances[2], ref combinedPlanes[2]);

        //Bottom
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            clip.M12, clip.M22, clip.M32, clip.M42, ref planes[3], ref distances[3], ref combinedPlanes[3]);

        //Near
        HandlePlaneSimple(clip.M13, clip.M23, clip.M33, clip.M43, ref planes[4], ref distances[4], ref combinedPlanes[4]);

        //Far
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            -clip.M13, -clip.M23, -clip.M33, -clip.M43, ref planes[5], ref distances[5], ref combinedPlanes[5]);
    }

    /// <summary>
    /// Checks if a point is visible
    /// </summary>
    /// <param name="v">The point</param>
    /// <returns>Whether the point is visible</returns>
    public bool PointTest(in Vector3 v)
    {
        for(var i = 0; i < 6; i++)
        {
            ref var normal = ref planes[i];
            ref var distance = ref distances[i];

            var dot = Vector3.Dot(normal, v) + distance;

            if (dot < -Math.Epsilon)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a sphere is visible
    /// </summary>
    /// <param name="center">The center of the sphere</param>
    /// <param name="radius">The radius of the sphere</param>
    /// <returns>The result of the test. If it's not Invisible, then it's at least partially visible</returns>
    public FrustumResult SphereTest(in Vector3 center, float radius)
    {
        for(var i = 0; i < 6; i++)
        {
            ref var plane = ref combinedPlanes[i];

            if(plane.X * center.X + plane.Y * center.Y + plane.Z * center.Z + plane.W <= -radius)
            {
                return FrustumResult.Invisible;
            }
        }

        return FrustumResult.Visible;
    }

    /// <summary>
    /// Checks if a <see cref="BoundingSphere"/> is visible
    /// </summary>
    /// <param name="sphere">The <see cref="BoundingSphere"/> to test</param>
    /// <returns>The result of the test. If it's not Invisible, then it's at least partially visible</returns>
    public FrustumResult SphereTest(in BoundingSphere sphere) => SphereTest(in sphere.center, sphere.radius);

    /// <summary>
    /// Checks if an Axis Aligned Bounding Box is visible
    /// </summary>
    /// <param name="aabb">The AABB</param>
    /// <returns>The result of the test. If it's not Invisible, then it's at least partially visible.</returns>
    public FrustumResult AABBTest(in AABB aabb)
    {
        var result = FrustumResult.Visible;

        var min = aabb.min;
        var max = aabb.max;

        for (var i = 0; i < 6; i++)
        {
            ref var normal = ref planes[i];
            ref var distance = ref distances[i];

            var positive = new Vector3(normal.X >= 0 ? max.X : min.X,
                normal.Y >= 0 ? max.Y : min.Y,
                normal.Z >= 0 ? max.Z : min.Z);

            var t = positive.Dot(normal) + distance;

            if(t < 0)
            {
                return FrustumResult.Invisible;
            }
        }

        return result;
    }
}
