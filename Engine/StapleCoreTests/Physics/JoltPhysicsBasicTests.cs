using Staple;
using System.Numerics;

namespace CoreTests
{
    internal class JoltPhysicsBasicTests
    {
        [Test]
        public void TestMoveStatic()
        {
            LayerMask.AllLayers.Add("Default");

            Physics3D.Instance = new Physics3D(new JoltPhysics3D());

            Assert.IsTrue(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Static, 0, false, 1, out var body));

            Assert.That(body.MotionType, Is.EqualTo(BodyMotionType.Static));

            Assert.That(body.Position, Is.EqualTo(Vector3.Zero));

            body.Position = Vector3.One;

            Assert.That(body.Position, Is.EqualTo(Vector3.One));

            body.Position = Vector3.Zero;

            Assert.That(body.Position, Is.EqualTo(Vector3.Zero));
        }

        [Test]
        public void TestMoveKinematic()
        {
            LayerMask.AllLayers.Add("Default");

            Physics3D.Instance = new Physics3D(new JoltPhysics3D());

            Assert.IsTrue(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Kinematic, 0, false, 1, out var body));

            Assert.That(body.MotionType, Is.EqualTo(BodyMotionType.Kinematic));

            Assert.That(body.Position, Is.EqualTo(Vector3.Zero));

            body.Position = Vector3.One;

            Assert.That(body.Position, Is.EqualTo(Vector3.One));

            body.Position = Vector3.Zero;

            Assert.That(body.Position, Is.EqualTo(Vector3.Zero));
        }

        [Test]
        public void TestMoveDynamic()
        {
            LayerMask.AllLayers.Add("Default");

            Physics3D.Instance = new Physics3D(new JoltPhysics3D());

            Assert.IsTrue(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Dynamic, 0, false, 1, out var body));

            Assert.That(body.MotionType, Is.EqualTo(BodyMotionType.Dynamic));

            Assert.That(body.Position, Is.EqualTo(Vector3.Zero));

            body.Position = Vector3.One;

            Assert.That(body.Position, Is.EqualTo(Vector3.One));

            body.Position = Vector3.Zero;

            Assert.That(body.Position, Is.EqualTo(Vector3.Zero));
        }


        [Test]
        public void TestSensor()
        {
            LayerMask.AllLayers.Add("Default");

            Physics3D.Instance = new Physics3D(new JoltPhysics3D());

            Assert.IsTrue(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Dynamic, 0, false, 1, out var body));

            Assert.IsFalse(body.IsTrigger);

            body.IsTrigger = true;

            Assert.IsTrue(body.IsTrigger);

            body.IsTrigger = false;

            Assert.IsFalse(body.IsTrigger);
        }
    }
}
