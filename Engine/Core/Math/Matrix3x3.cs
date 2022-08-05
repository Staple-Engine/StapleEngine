using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public struct Matrix3x3
    {
        public float M11, M12, M13,
            M21, M22, M23,
            M31, M32, M33;

        public static Matrix3x3 Identity
        {
            get
            {
                var matrix = new Matrix3x3();

                matrix.M11 = matrix.M22 = matrix.M33 = 1;

                return matrix;
            }
        }
    }
}
