using Staple;
using System.Numerics;

namespace CoreTests
{
    internal class TransformTests
    {
        [Test]
        public void TestChanged()
        {
            var transform = new Transform();

            Assert.IsFalse(transform.Changed);

            transform.LocalPosition = transform.LocalPosition;

            Assert.IsFalse(transform.Changed);

            transform.LocalRotation = transform.LocalRotation;

            Assert.IsFalse(transform.Changed);

            transform.LocalScale = transform.LocalScale;

            Assert.IsFalse(transform.Changed);

            transform.Position = transform.Position;

            Assert.IsFalse(transform.Changed);

            transform.Rotation = transform.Rotation;

            Assert.IsFalse(transform.Changed);

            transform.Scale = transform.Scale;

            Assert.IsFalse(transform.Changed);

            transform.LocalPosition = Vector3.One;

            Assert.IsTrue(transform.Changed);

            transform.Changed = false;

            transform.LocalRotation = Quaternion.CreateFromYawPitchRoll(1, 2, 3);

            Assert.IsTrue(transform.Changed);

            transform.Changed = false;

            transform.LocalScale = Vector3.Zero;

            Assert.IsTrue(transform.Changed);

            transform.Changed = false;

            transform.Position = Vector3.Zero;

            Assert.IsTrue(transform.Changed);

            transform.Changed = false;

            transform.Rotation = Quaternion.Identity;

            Assert.IsTrue(transform.Changed);

            transform.Changed = false;

            transform.Scale = Vector3.One;

            Assert.IsTrue(transform.Changed);

            transform.Changed = false;
        }

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

        [Test]
        public void TestPosition()
        {
            var transform = new Transform();
            var parent = new Transform();

            transform.SetParent(parent);

            transform.LocalPosition = new Vector3(0, 0, 1);

            Assert.That(transform.Changed, Is.True);

            Assert.That(transform.Position, Is.EqualTo(new Vector3(0, 0, 1)));

            parent.LocalPosition = new(0, 0, 1);

            Assert.That(transform.Position, Is.EqualTo(new Vector3(0, 0, 2)));

            transform.LocalPosition = Vector3.Zero;

            Assert.That(transform.Position, Is.EqualTo(new Vector3(0, 0, 1)));
        }

        [Test]
        public void TestRotation()
        {
            var transform = new Transform();
            var parent = new Transform();

            transform.SetParent(parent);

            transform.LocalRotation = Staple.Math.FromEulerAngles(new(0, 45, 0));

            Assert.That(transform.Changed, Is.True);

            var angles = transform.Rotation.ToEulerAngles();

            angles.X = Staple.Math.Round(angles.X);
            angles.Y = Staple.Math.Round(angles.Y);
            angles.Z = Staple.Math.Round(angles.Z);

            Assert.That(angles, Is.EqualTo(new Vector3(0, 45, 0)));

            parent.LocalRotation = Staple.Math.FromEulerAngles(new(0, 45, 0));

            angles = transform.Rotation.ToEulerAngles();

            angles.X = Staple.Math.Round(angles.X);
            angles.Y = Staple.Math.Round(angles.Y);
            angles.Z = Staple.Math.Round(angles.Z);

            Assert.That(angles, Is.EqualTo(new Vector3(0, 90, 0)));

            transform.Rotation = Staple.Math.FromEulerAngles(new(0, 45, 0));

            angles = transform.Rotation.ToEulerAngles();

            angles.X = Staple.Math.Round(angles.X);
            angles.Y = Staple.Math.Round(angles.Y);
            angles.Z = Staple.Math.Round(angles.Z);

            Assert.That(angles, Is.EqualTo(new Vector3(0, 45, 0)));
        }

        [Test]
        public void TestScale()
        {
            var transform = new Transform();
            var parent = new Transform();

            transform.SetParent(parent);

            transform.LocalScale = new Vector3(2, 2, 2);

            Assert.That(transform.Changed, Is.True);

            Assert.That(transform.Scale, Is.EqualTo(new Vector3(2, 2, 2)));

            parent.LocalScale = new(0.5f, 0.5f, 0.5f);

            Assert.That(transform.Scale, Is.EqualTo(new Vector3(1, 1, 1)));

            transform.Scale = new(1, 1, 1);

            Assert.That(transform.Scale, Is.EqualTo(new Vector3(1, 1, 1)));
        }
    }
}