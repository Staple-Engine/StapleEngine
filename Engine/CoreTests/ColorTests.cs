using GlmSharp;
using NUnit.Framework;
using Staple;

namespace CoreTests
{
    internal class ColorTests
    {
        [Test]
        public void TestColorEquality()
        {
            var color = new Color(1, 2, 3, 4);

            Assert.AreEqual(color, color);
            Assert.AreNotEqual(new Color(1, 2, 3, 0), color);
            Assert.AreNotEqual(new Color(1, 2, 0, 4), color);
            Assert.AreNotEqual(new Color(1, 0, 3, 4), color);
            Assert.AreNotEqual(new Color(0, 2, 3, 4), color);

            var color2 = new Color32(1, 2, 3, 4);

            Assert.AreEqual(color2, color2);

            Assert.AreNotEqual(new Color32(1, 2, 3, 0), color2);
            Assert.AreNotEqual(new Color32(1, 2, 0, 4), color2);
            Assert.AreNotEqual(new Color32(1, 0, 3, 4), color2);
            Assert.AreNotEqual(new Color32(0, 2, 3, 4), color2);
        }

        [Test]
        public void TestColorConversion()
        {
            var a = new Color(0.5f, 0.25f, 1.0f, 0.0f);

            var b = (Color32)a;

            Assert.AreEqual(127, b.r);
            Assert.AreEqual(63, b.g);
            Assert.AreEqual(255, b.b);
            Assert.AreEqual(0, b.a);

            a = (Color)b;

            Assert.AreEqual(498, (int)(a.r * 1000));
            Assert.AreEqual(247, (int)(a.g * 1000));
            Assert.AreEqual(1, a.b);
            Assert.AreEqual(0, a.a);
        }
    }
}
