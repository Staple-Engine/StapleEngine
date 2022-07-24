using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Staple
{
    public struct AABB
    {
        public Vector3 center;
        public Vector3 extents;

        public Vector3 min => new Vector3(center.X - extents.X / 2, center.Y - extents.Y / 2, center.Z - extents.Z / 2);

        public Vector3 max => new Vector3(center.X + extents.X / 2, center.Y + extents.Y / 2, center.Z + extents.Z / 2);

        public Vector3 size => extents * 2;

        public AABB(Vector3 center, Vector3 size)
        {
            this.center = center;

            extents = size / 2;
        }

        public bool Contains(Vector3 point)
        {
            var min = this.min;
            var max = this.max;

            return point.X >= min.X && point.Y >= min.Y && point.Z >= min.Z &&
                point.X <= max.X && point.Y <= max.Y && point.Z <= max.Z;
        }

        public void Expand(float amount)
        {
            extents *= (1 + amount / 2);
        }

        public void Expand(Vector3 amount)
        {
            extents *= new Vector3(1 + amount.X / 2, 1 + amount.Y / 2, 1 + amount.Z / 2);
        }
    }
}
