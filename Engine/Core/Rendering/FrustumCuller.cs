using System.Numerics;

namespace Staple.Internal;

enum FrustumAABBResult
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

        var magnitude = vector.ToVector3().Length();

        planes[0].Normal = new Vector3(vector.X / magnitude, vector.Y / magnitude, vector.Z / magnitude);
        planes[0].D = vector.W / magnitude;

        vector = Vector4.Normalize(new Vector4(clip.M14 + clip.M11,
            clip.M24 + clip.M21,
            clip.M34 + clip.M31,
            clip.M44 + clip.M41));

        magnitude = vector.ToVector3().Length();

        planes[1].Normal = new Vector3(vector.X / magnitude, vector.Y / magnitude, vector.Z / magnitude);
        planes[1].D = vector.W / magnitude;

        vector = Vector4.Normalize(new Vector4(clip.M14 + clip.M12,
            clip.M24 + clip.M22,
            clip.M34 + clip.M32,
            clip.M44 + clip.M42));

        magnitude = vector.ToVector3().Length();

        planes[2].Normal = new Vector3(vector.X / magnitude, vector.Y / magnitude, vector.Z / magnitude);
        planes[2].D = vector.W / magnitude;

        vector = Vector4.Normalize(new Vector4(clip.M14 - clip.M12,
            clip.M24 - clip.M22,
            clip.M34 - clip.M32,
            clip.M44 - clip.M42));

        magnitude = vector.ToVector3().Length();

        planes[3].Normal = new Vector3(vector.X / magnitude, vector.Y / magnitude, vector.Z / magnitude);
        planes[3].D = vector.W / magnitude;

        vector = Vector4.Normalize(new Vector4(clip.M14 - clip.M13,
            clip.M24 - clip.M23,
            clip.M34 - clip.M33,
            clip.M44 - clip.M43));

        magnitude = vector.ToVector3().Length();

        planes[4].Normal = new Vector3(vector.X / magnitude, vector.Y / magnitude, vector.Z / magnitude);
        planes[4].D = vector.W / magnitude;

        vector = Vector4.Normalize(new Vector4(clip.M14 + clip.M13,
            clip.M24 + clip.M23,
            clip.M34 + clip.M33,
            clip.M44 + clip.M43));

        magnitude = vector.ToVector3().Length();

        planes[5].Normal = new Vector3(vector.X / magnitude, vector.Y / magnitude, vector.Z / magnitude);
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
    /// CHecks if an Axis Aligned Bounding Box is visible
    /// </summary>
    /// <param name="aabb">The AABB</param>
    /// <returns>The result of the test. If it's not Invisible, then it's at least partially visible.</returns>
    public FrustumAABBResult AABBTest(AABB aabb)
    {
        var result = FrustumAABBResult.Visible;

        for(var i = 0; i < 6; i++)
        {
            var plane = planes[i];
            var normal = plane.Normal;
            var distance = plane.D;
            var planeVector = new Vector4(normal.X, normal.Y, normal.Z, distance);

            var positive = new Vector4(normal.X > 0 ? aabb.Max.X : aabb.Min.X,
                normal.Y > 0 ? aabb.Max.Y : aabb.Min.Y,
                normal.Z > 0 ? aabb.Max.Z : aabb.Min.Z,
                1.0f);

            var negative = new Vector4(normal.X < 0 ? aabb.Max.X : aabb.Min.X,
                normal.Y < 0 ? aabb.Max.Y : aabb.Min.Y,
                normal.Z < 0 ? aabb.Max.Z : aabb.Min.Z,
                1.0f);

            var t = Vector4.Dot(positive, planeVector);

            if(t < 0)
            {
                return FrustumAABBResult.Invisible;
            }

            t = Vector4.Dot(negative, planeVector);

            if(t < 0)
            {
                result = FrustumAABBResult.Intersecting;
            }
        }

        return result;
    }
}
