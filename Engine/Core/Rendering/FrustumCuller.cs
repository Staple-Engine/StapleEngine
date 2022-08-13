using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    enum FrustumAABBResult
    {
        Visible,
        Intersecting,
        Invisible
    }

    internal class FrustumCuller
    {
        private Plane[] planes = new Plane[6];

        public void Update(Matrix4x4 view, Matrix4x4 Projection)
        {
            var clip = view * Projection;

            var vector = Vector4.Normalize(new Vector4(clip.M14 - clip.M11,
                clip.M24 - clip.M21,
                clip.M34 - clip.M31,
                clip.M44 - clip.M41));

            planes[0].Normal = new Vector3(vector.X, vector.Y, vector.Z);
            planes[0].D = vector.W;

            vector = Vector4.Normalize(new Vector4(clip.M14 + clip.M11,
                clip.M24 + clip.M21,
                clip.M34 + clip.M31,
                clip.M44 + clip.M41));

            planes[1].Normal = new Vector3(vector.X, vector.Y, vector.Z);
            planes[1].D = vector.W;

            vector = Vector4.Normalize(new Vector4(clip.M14 + clip.M12,
                clip.M24 + clip.M22,
                clip.M34 + clip.M32,
                clip.M44 + clip.M42));

            planes[2].Normal = new Vector3(vector.X, vector.Y, vector.Z);
            planes[2].D = vector.W;

            vector = Vector4.Normalize(new Vector4(clip.M14 - clip.M12,
                clip.M24 - clip.M22,
                clip.M34 - clip.M32,
                clip.M44 - clip.M42));

            planes[3].Normal = new Vector3(vector.X, vector.Y, vector.Z);
            planes[3].D = vector.W;

            vector = Vector4.Normalize(new Vector4(clip.M14 - clip.M13,
                clip.M24 - clip.M23,
                clip.M34 - clip.M33,
                clip.M44 - clip.M43));

            planes[4].Normal = new Vector3(vector.X, vector.Y, vector.Z);
            planes[4].D = vector.W;

            vector = Vector4.Normalize(new Vector4(clip.M14 + clip.M13,
                clip.M24 + clip.M23,
                clip.M34 + clip.M33,
                clip.M44 + clip.M43));

            planes[5].Normal = new Vector3(vector.X, vector.Y, vector.Z);
            planes[5].D = vector.W;
        }

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

        public FrustumAABBResult AABBTest(AABB aabb)
        {
            var result = FrustumAABBResult.Visible;

            for(var i = 0; i < 6; i++)
            {
                var positive = new Vector4(planes[i].Normal.X > 0 ? aabb.max.X : aabb.min.X,
                    planes[i].Normal.Y > 0 ? aabb.max.Y : aabb.min.Y,
                    planes[i].Normal.Z > 0 ? aabb.max.Z : aabb.min.Z,
                    1.0f);

                var negative = new Vector4(planes[i].Normal.X < 0 ? aabb.max.X : aabb.min.X,
                    planes[i].Normal.Y < 0 ? aabb.max.Y : aabb.min.Y,
                    planes[i].Normal.Z < 0 ? aabb.max.Z : aabb.min.Z,
                    1.0f);

                var planeVector = new Vector4(planes[i].Normal.X, planes[i].Normal.Y, planes[i].Normal.Z, planes[i].D);

                if(Vector4.Dot(positive, planeVector) < 0)
                {
                    return FrustumAABBResult.Invisible;
                }

                if(Vector4.Dot(negative, planeVector) < 0)
                {
                    result = FrustumAABBResult.Intersecting;
                }
            }

            return result;
        }
    }
}
