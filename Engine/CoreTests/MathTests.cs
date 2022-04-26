using GlmSharp;
using NUnit.Framework;
using Staple;

namespace CoreTests
{
    internal class MathTests
    {
        [Test]
        public void TestVector3Init()
        {
            var v = new Vector3();

            Assert.AreEqual(Vector3.zero, v);

            v = new Vector3(1, 2, 3);

            Assert.AreEqual(1, v.x);
            Assert.AreEqual(2, v.y);
            Assert.AreEqual(3, v.z);
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

            v *= Vector3.one * 2.0f;

            Assert.AreEqual(new Vector3(2, 4, 6), v);

            v /= Vector3.one * 2.0f;

            Assert.AreEqual(new Vector3(1, 2, 3), v);
        }

        [Test]
        public void TestMathRound()
        {
            Assert.AreEqual(1.0f, Math.Round(0.5f));
            Assert.AreEqual(0.0f, Math.Round(0.49f));
            Assert.AreEqual(-1.0f, Math.Round(-0.5f));
            Assert.AreEqual(0.0f, Math.Round(-0.49f));

            Assert.AreEqual(1, Math.RoundToInt(0.5f));
            Assert.AreEqual(0, Math.RoundToInt(0.49f));
            Assert.AreEqual(-1, Math.RoundToInt(-0.5f));
            Assert.AreEqual(0, Math.RoundToInt(-0.49f));
        }
    }
}
