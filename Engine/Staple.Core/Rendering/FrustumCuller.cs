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
    internal readonly Plane[] planes = new Plane[6];

    /// <summary>
    /// Updates the frustum culler's planes
    /// </summary>
    /// <param name="view">The view matrix</param>
    /// <param name="projection">The projection matrix</param>
    public void Update(in Matrix4x4 view, in Matrix4x4 projection)
    {
        var clip = view * projection;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void HandlePlane(float A, float B, float C, float D, float NA, float NB, float NC, float ND, ref Plane plane)
        {
            var vector = new Vector3(A + NA, B + NB, C + NC);

            var magnitude = vector.Length();

            plane.Normal = vector.Normalized;
            plane.D = (D + ND) / magnitude;
        }

        static void HandlePlaneSimple(float A, float B, float C, float D, ref Plane plane)
        {
            var vector = new Vector3(A, B, C);

            var magnitude = vector.Length();

            plane.Normal = vector.Normalized;
            plane.D = D / magnitude;
        }

        //Left
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            clip.M11, clip.M21, clip.M31, clip.M41, ref planes[0]);

        //Right
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            -clip.M11, -clip.M21, -clip.M31, -clip.M41, ref planes[1]);

        //Top
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            -clip.M12, -clip.M22, -clip.M32, -clip.M42, ref planes[2]);

        //Bottom
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            clip.M12, clip.M22, clip.M32, clip.M42, ref planes[3]);

        //Near
        HandlePlaneSimple(clip.M13, clip.M23, clip.M33, clip.M43, ref planes[4]);

        //Far
        HandlePlane(clip.M14, clip.M24, clip.M34, clip.M44,
            -clip.M13, -clip.M23, -clip.M33, -clip.M43, ref planes[5]);
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
            var dot = Vector3.Dot(planes[i].Normal, v) + planes[i].D;

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
            var plane = planes[i];
            var normal = plane.Normal;
            var distance = plane.D;

            if(normal.X * center.X + normal.Y * center.Y + normal.Z * center.Z + distance <= -radius)
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
            var plane = planes[i];
            var normal = plane.Normal;
            var planeVector = new Vector4(normal.X, normal.Y, normal.Z, plane.D);

            var positive = new Vector4(normal.X > 0 ? max.X : min.X,
                normal.Y > 0 ? max.Y : min.Y,
                normal.Z > 0 ? max.Z : min.Z,
                1.0f);

            var t = Vector4.Dot(positive, planeVector);

            if(t < 0)
            {
                return FrustumResult.Invisible;
            }
        }

        return result;
    }
}
