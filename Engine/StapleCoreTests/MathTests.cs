using Staple;
using System.Numerics;

namespace CoreTests
{
    internal class MathTests
    {
        [Test]
        public void TestNextPowerOf2()
        {
            Assert.AreEqual(2, Staple.Math.NextPowerOfTwo(1));
            Assert.AreEqual(32, Staple.Math.NextPowerOfTwo(21));
            Assert.AreEqual(16, Staple.Math.NextPowerOfTwo(10));
        }

        [Test]
        public void TestVector3Init()
        {
            var v = new Vector3();

            Assert.AreEqual(Vector3.Zero, v);

            v = new Vector3(1, 2, 3);

            Assert.AreEqual(1, v.X);
            Assert.AreEqual(2, v.Y);
            Assert.AreEqual(3, v.Z);
        }

        [Test]
        public void TestVector3Operators()
        {
            var v = new Vector3(3, 2, 1);

            var add = new Vector3(1, 2, 3);

            v += add;

            Assert.AreEqual(new Vector3(4, 4, 4), v);

            var sub = new Vector3(3, 2, 1);

            v -= sub;

            Assert.AreEqual(new Vector3(1, 2, 3), v);

            v *= 2.0f;

            Assert.AreEqual(new Vector3(2, 4, 6), v);

            v /= 2.0f;

            Assert.AreEqual(new Vector3(1, 2, 3), v);

            Assert.IsTrue(v == new Vector3(1, 2, 3));

            Assert.IsFalse(v == new Vector3(3, 2, 1));

            v *= Vector3.One * 2.0f;

            Assert.AreEqual(new Vector3(2, 4, 6), v);

            v /= Vector3.One * 2.0f;

            Assert.AreEqual(new Vector3(1, 2, 3), v);
        }

        [Test]
        public void TestMathRound()
        {
            Assert.AreEqual(1.0f, Staple.Math.Round(0.5f));
            Assert.AreEqual(0.0f, Staple.Math.Round(0.49f));
            Assert.AreEqual(-1.0f, Staple.Math.Round(-0.5f));
            Assert.AreEqual(0.0f, Staple.Math.Round(-0.49f));

            Assert.AreEqual(1, Staple.Math.RoundToInt(0.5f));
            Assert.AreEqual(0, Staple.Math.RoundToInt(0.49f));
            Assert.AreEqual(-1, Staple.Math.RoundToInt(-0.5f));
            Assert.AreEqual(0, Staple.Math.RoundToInt(-0.49f));
        }

        [Test]
        public void TestQuaternionEuler()
        {
            for(var i = 0; i < 10; i++)
            {
                var rotation = new Vector3(0, i * 18, 0);

                var quaternion = Staple.Math.FromEulerAngles(rotation);
                var newRotation = Staple.Math.ToEulerAngles(quaternion);

                Assert.AreEqual(0, newRotation.X);
                Assert.AreEqual(0, newRotation.Z);

                Assert.AreEqual(i * 18, Staple.Math.RoundToInt(newRotation.Y));
            }
        }
    }
}
