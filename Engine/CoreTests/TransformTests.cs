using GlmSharp;
using NUnit.Framework;
using Staple;

namespace CoreTests
{
    internal class TransformTests
    {
        [Test]
        public void TestLocalPosition()
        {
            var transform = new Transform();

            Assert.IsFalse(transform.Changed);

            transform.LocalPosition = Vector3.forward;

            Assert.IsTrue(transform.Changed);

            var matrix = transform.Matrix;

            Assert.AreNotEqual(matrix, mat4.Identity);
        }

        [Test]
        public void TestLocalRotation()
        {
            var transform = new Transform();

            Assert.IsFalse(transform.Changed);

            transform.LocalRotation = quat.FromAxisAngle(glm.Radians(90.0f), new vec3(0, 1, 0));

            Assert.IsTrue(transform.Changed);

            var matrix = transform.Matrix;

            Assert.AreNotEqual(matrix, mat4.Identity);

            var forward = transform.Forward;

            forward.x = Math.Round(forward.x);
            forward.y = Math.Round(forward.y);
            forward.z = Math.Round(forward.z);

            Assert.AreEqual(new Vector3(1, 0, 0), forward);
        }

        [Test]
        public void TestLocalScale()
        {
            var transform = new Transform();

            Assert.IsFalse(transform.Changed);

            transform.LocalScale = Vector3.one * 0.5f;

            Assert.IsTrue(transform.Changed);

            var matrix = transform.Matrix;

            Assert.AreNotEqual(matrix, mat4.Identity);

            var scaled = matrix * (vec4)Vector3.one;

            Assert.AreEqual(new vec4(0.5f, 0.5f, 0.5f, 0), scaled);
        }
    }
}