using Staple;
using System.Numerics;

namespace CoreTests
{
    internal class JoltPhysicsBasicTests
    {
        [Test]
        public void TestRayHit()
        {
            LayerMask.AllLayers.Add("Default");

            Physics3D.Instance = new Physics3D(new JoltPhysics3D());

            Assert.That(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Static, 0, false,
                1, 0, 0, false, false, false, false, out var body), Is.True);

            Assert.That(body.MotionType, Is.EqualTo(BodyMotionType.Static));

            Assert.That(body.Position, Is.EqualTo(Vector3.Zero));

            var ray = new Ray(new Vector3(0, 0, 10), new Vector3(0, 0, -1));

            Assert.That(Physics3D.Instance.RayCast(ray, out _, out _, PhysicsTriggerQuery.Ignore, 10), Is.True);

            ray.position = new Vector3(0, 0, -10);
            ray.direction = new Vector3(0, 0, 1);

            Assert.That(Physics3D.Instance.RayCast(ray, out _, out _, PhysicsTriggerQuery.Ignore, 10), Is.True);

            ray.position = new Vector3(-10, 0, 0);
            ray.direction = new Vector3(1, 0, 0);

            Assert.That(Physics3D.Instance.RayCast(ray, out _, out _, PhysicsTriggerQuery.Ignore, 10), Is.True);

            ray.position = new Vector3(10, 0, 0);
            ray.direction = new Vector3(-1, 0, 0);

            Assert.That(Physics3D.Instance.RayCast(ray, out _, out _, PhysicsTriggerQuery.Ignore, 10), Is.True);
        }

        [Test]
        public void TestMoveStatic()
        {
            LayerMask.AllLayers.Add("Default");

            Physics3D.Instance = new Physics3D(new JoltPhysics3D());

            Assert.That(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Static, 0, false,
                1, 0, 0, false, false, false, false, out var body), Is.True);

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

            Assert.That(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Kinematic, 0, false,
                1, 0, 0, false, false, false, false, out var body), Is.True);

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

            Assert.That(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Dynamic, 0, false,
                1, 0, 0, false, false, false, false, out var body), Is.True);

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

            Assert.That(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Dynamic, 0, false,
                1, 0, 0, false, false, false, false, out var body), Is.True);

            Assert.IsFalse(body.IsTrigger);

            body.IsTrigger = true;

            Assert.IsTrue(body.IsTrigger);

            body.IsTrigger = false;

            Assert.IsFalse(body.IsTrigger);
        }
    }
}
