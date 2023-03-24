using Staple;
using System.Numerics;

namespace CoreTests
{
    internal class TransformTests
    {
        [Test]
        public void TestLocalPosition()
        {
            var transform = new Transform();

            Assert.IsFalse(transform.Changed);

            transform.LocalPosition = new Vector3(0, 0, 1);

            Assert.IsTrue(transform.Changed);

            var matrix = transform.Matrix;

            Assert.AreNotEqual(matrix, Matrix4x4.Identity);
        }

        [Test]
        public void TestLocalRotation()
        {
            var transform = new Transform();

            Assert.IsFalse(transform.Changed);

            transform.LocalRotation = Staple.Math.FromEulerAngles(new Vector3(0, 90, 0));

            Assert.IsTrue(transform.Changed);

            var matrix = transform.Matrix;

            Assert.AreNotEqual(matrix, Matrix4x4.Identity);

            var forward = transform.Forward;

            forward.X = Staple.Math.Round(forward.X);
            forward.Y = Staple.Math.Round(forward.Y);
            forward.Z = Staple.Math.Round(forward.Z);

            Assert.AreEqual(new Vector3(-1, 0, 0), forward);
        }

        [Test]
        public void TestLocalScale()
        {
            var transform = new Transform();

            Assert.IsFalse(transform.Changed);

            transform.LocalScale = Vector3.One * 0.5f;

            Assert.IsTrue(transform.Changed);

            var matrix = transform.Matrix;

            Assert.AreNotEqual(matrix, Matrix4x4.Identity);

            var scaled = Vector3.Transform(Vector3.One, matrix);

            Assert.AreEqual(new Vector3(0.5f, 0.5f, 0.5f), scaled);
        }
    }
}