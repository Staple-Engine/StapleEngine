﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Ray
    {
        public Vector3 position;
        public Vector3 direction;

        public Ray(Vector3 position, Vector3 direction)
        {
            this.position = position;
            this.direction = direction;
        }

        public static bool IntersectsAABB(Ray ray, AABB aabb)
        {
            float t0 = 0, t1 = 0;

            return IntersectsAABB(ray, aabb, out t0, out t1);
        }

        public static bool IntersectsAABB(Ray ray, AABB aabb, out float t0, out float t1)
        {
            t0 = 0;
            t1 = 0;

            float tfirst = -999999;
            float tlast = 999999;

            bool RaySlabIntersect(float start, float dir, float min, float max)
            {
                if(Math.Abs(dir) < 1.0E-8)
                {
                    return (start < max && start > min);
                }

                float tmin = (min - start) / dir;
                float tmax = (max - start) / dir;

                if(tmin > tmax)
                {
                    (tmin, tmax) = (tmax, tmin);
                }

                if (tmax < tfirst || tmin > tlast)
                {
                    return false;
                }

                if(tmin > tfirst)
                {
                    tfirst = tmin;
                }

                if(tmax < tlast)
                {
                    tlast = tmax;
                }

                return true;
            }

            if(RaySlabIntersect(ray.position.X, ray.direction.X, aabb.min.X, aabb.max.X) == false)
            {
                return false;
            }

            if (RaySlabIntersect(ray.position.Y, ray.direction.Y, aabb.min.Y, aabb.max.Y) == false)
            {
                return false;
            }

            if (RaySlabIntersect(ray.position.Z, ray.direction.Z, aabb.min.Z, aabb.max.Z) == false)
            {
                return false;
            }

            t0 = tfirst;
            t1 = tlast;

            return true;
        }

        public static bool IntersectsAABB(Ray ray, AABB aabb, Transform transform)
        {
            float t0 = 0, t1 = 0;

            return IntersectsAABB(ray, aabb, transform, out t0, out t1);
        }

        public static bool IntersectsAABB(Ray ray, AABB aabb, Transform transform, out float t0, out float t1)
        {
            t0 = 0;
            t1 = 0;

            var rayDelta = transform.Position - ray.position;
            var matrix = transform.Matrix;
            int p = 0;
            float tmin = -999999, tmax = 999999;

            var floatMatrix = new float[16]
            {
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44,
            };

            for(var i = 0; i < 3; i++, p += 4)
            {
                var axis = new Vector3(floatMatrix[p], floatMatrix[p + 1], floatMatrix[p + 2]);

                var nominatorLength = Vector3.Dot(axis, rayDelta);
                var denominatorLength = Vector3.Dot(ray.direction, axis);

                float boxMin = 0;
                float boxMax = 0;

                switch (i)
                {
                    case 0:
                        boxMin = aabb.min.X;
                        boxMax = aabb.max.X;

                        break;

                    case 1:
                        boxMin = aabb.min.Y;
                        boxMax = aabb.max.Y;

                        break;

                    case 2:
                        boxMin = aabb.min.Z;
                        boxMax = aabb.max.Z;

                        break;
                }

                if (Math.Abs(denominatorLength) > 0.00001)
                {

                    var min = (nominatorLength + boxMin) / denominatorLength;
                    var max = (nominatorLength + boxMax) / denominatorLength;

                    if(min > max)
                    {
                        (min, max) = (max, min);
                    }

                    if(min > tmin)
                    {
                        tmin = min;
                    }

                    if(max < tmax)
                    {
                        tmax = max;
                    }

                    if(tmax < tmin)
                    {
                        return false;
                    }
                }
                else if(-nominatorLength + boxMin > 0 || -nominatorLength + boxMax < 0)
                {
                    return false;
                }
            }

            t0 = tmin;
            t1 = tmax;

            return true;
        }
    }
}