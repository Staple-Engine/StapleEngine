using Staple;
using System.Numerics;

namespace CoreTests
{
    internal class MathTests
    {
        [Test]
        public void TestNextPowerOf2()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Staple.Math.NextPowerOfTwo(1), Is.EqualTo(2));
                Assert.That(Staple.Math.NextPowerOfTwo(21), Is.EqualTo(32));
                Assert.That(Staple.Math.NextPowerOfTwo(10), Is.EqualTo(16));
            });
        }

        [Test]
        public void TestVector3Init()
        {
            var v = new Vector3();

            Assert.That(v, Is.EqualTo(Vector3.Zero));

            v = new Vector3(1, 2, 3);

            Assert.Multiple(() =>
            {
                Assert.That(v.X, Is.EqualTo(1));
                Assert.That(v.Y, Is.EqualTo(2));
                Assert.That(v.Z, Is.EqualTo(3));
            });
        }

        [Test]
        public void TestVector3Operators()
        {
            var v = new Vector3(3, 2, 1);

            var add = new Vector3(1, 2, 3);

            v += add;

            Assert.That(v, Is.EqualTo(new Vector3(4, 4, 4)));

            var sub = new Vector3(3, 2, 1);

            v -= sub;

            Assert.That(v, Is.EqualTo(new Vector3(1, 2, 3)));

            v *= 2.0f;

            Assert.That(v, Is.EqualTo(new Vector3(2, 4, 6)));

            v /= 2.0f;

            Assert.That(v, Is.EqualTo(new Vector3(1, 2, 3)));

            Assert.IsTrue(v == new Vector3(1, 2, 3));

            Assert.IsFalse(v == new Vector3(3, 2, 1));

            v *= Vector3.One * 2.0f;

            Assert.That(v, Is.EqualTo(new Vector3(2, 4, 6)));

            v /= Vector3.One * 2.0f;

            Assert.That(v, Is.EqualTo(new Vector3(1, 2, 3)));
        }

        [Test]
        public void TestMathRound()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Staple.Math.Round(0.5f), Is.EqualTo(1.0f));
                Assert.That(Staple.Math.Round(0.49f), Is.EqualTo(0.0f));
                Assert.That(Staple.Math.Round(-0.5f), Is.EqualTo(-1.0f));
                Assert.That(Staple.Math.Round(-0.49f), Is.EqualTo(0.0f));

                Assert.That(Staple.Math.RoundToInt(0.5f), Is.EqualTo(1));
                Assert.That(Staple.Math.RoundToInt(0.49f), Is.EqualTo(0));
                Assert.That(Staple.Math.RoundToInt(-0.5f), Is.EqualTo(-1));
                Assert.That(Staple.Math.RoundToInt(-0.49f), Is.EqualTo(0));
            });
        }

        [Test]
        public void TestQuaternionEuler()
        {
            for(var i = 0; i < 10; i++)
            {
                var rotation = new Vector3(0, i * 18, 0);

                var quaternion = Staple.Math.FromEulerAngles(rotation);
                var newRotation = Staple.Math.ToEulerAngles(quaternion);

                Assert.Multiple(() =>
                {
                    Assert.That(newRotation.X, Is.EqualTo(0));
                    Assert.That(newRotation.Z, Is.EqualTo(0));

                    Assert.That(Staple.Math.RoundToInt(newRotation.Y), Is.EqualTo(i * 18));
                });
            }
        }
    }
}
