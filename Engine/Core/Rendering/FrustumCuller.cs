using System.Numerics;

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
    private readonly Plane[] planes = new Plane[6];

    /// <summary>
    /// Updates the frustum culler's planes
    /// </summary>
    /// <param name="view">The view matrix</param>
    /// <param name="projection">The projection matrix</param>
    public void Update(Matrix4x4 view, Matrix4x4 projection)
    {
        var clip = view * projection;

        var vector = new Vector4(clip.M14 - clip.M11,
            clip.M24 - clip.M21,
            clip.M34 - clip.M31,
            clip.M44 - clip.M41);

        var vector3 = vector.ToVector3();

        var magnitude = vector3.Length();

        planes[0].Normal = Vector3.Normalize(vector3);
        planes[0].D = vector.W / magnitude;

        vector = new Vector4(clip.M14 + clip.M11,
            clip.M24 + clip.M21,
            clip.M34 + clip.M31,
            clip.M44 + clip.M41);

        vector3 = vector.ToVector3();

        magnitude = vector3.Length();

        planes[1].Normal = Vector3.Normalize(vector3);
        planes[1].D = vector.W / magnitude;

        vector = new Vector4(clip.M14 + clip.M12,
            clip.M24 + clip.M22,
            clip.M34 + clip.M32,
            clip.M44 + clip.M42);

        vector3 = vector.ToVector3();

        magnitude = vector3.Length();

        planes[2].Normal = Vector3.Normalize(vector3);
        planes[2].D = vector.W / magnitude;

        vector = new Vector4(clip.M14 - clip.M12,
            clip.M24 - clip.M22,
            clip.M34 - clip.M32,
            clip.M44 - clip.M42);

        vector3 = vector.ToVector3();

        magnitude = vector3.Length();

        planes[3].Normal = Vector3.Normalize(vector3);
        planes[3].D = vector.W / magnitude;

        vector = new Vector4(clip.M14 - clip.M13,
            clip.M24 - clip.M23,
            clip.M34 - clip.M33,
            clip.M44 - clip.M43);

        vector3 = vector.ToVector3();

        magnitude = vector3.Length();

        planes[4].Normal = Vector3.Normalize(vector3);
        planes[4].D = vector.W / magnitude;

        vector = new Vector4(clip.M14 + clip.M13,
            clip.M24 + clip.M23,
            clip.M34 + clip.M33,
            clip.M44 + clip.M43);

        vector3 = vector.ToVector3();

        magnitude = vector3.Length();

        planes[5].Normal = Vector3.Normalize(vector3);
        planes[5].D = vector.W / magnitude;
    }

    /// <summary>
    /// Checks if a point is visible
    /// </summary>
    /// <param name="v">The point</param>
    /// <returns>Whether the point is visible</returns>
    public bool PointTest(Vector3 v)
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
    public FrustumResult SphereTest(Vector3 center, float radius)
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
    /// Checks if an Axis Aligned Bounding Box is visible
    /// </summary>
    /// <param name="aabb">The AABB</param>
    /// <returns>The result of the test. If it's not Invisible, then it's at least partially visible.</returns>
    public FrustumResult AABBTest(AABB aabb)
    {
        var result = FrustumResult.Visible;

        var min = aabb.min;
        var max = aabb.max;

        for (var i = 0; i < 6; i++)
        {
            var plane = planes[i];
            var normal = plane.Normal;
            var distance = plane.D;
            var planeVector = new Vector4(normal.X, normal.Y, normal.Z, distance);

            var positive = new Vector4(normal.X > 0 ? max.X : min.X,
                normal.Y > 0 ? max.Y : min.Y,
                normal.Z > 0 ? max.Z : min.Z,
                1.0f);
                
            var negative = new Vector4(normal.X > 0 ? min.X : max.X,
                normal.Y > 0 ? min.Y : max.Y,
                normal.Z > 0 ? min.Z : max.Z,
                1.0f);

            var t = Vector4.Dot(positive, planeVector);

            if(t < 0)
            {
                return FrustumResult.Invisible;
            }

            t = Vector4.Dot(negative, planeVector);

            if(t < 0)
            {
                result = FrustumResult.Intersecting;
            }
        }

        return result;
    }
}
