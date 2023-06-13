using Staple;
using System.Numerics;

namespace CoreTests
{
    internal class RaycastTests
    {
        [Test]
        public void TestBoxRaycast()
        {
            Physics3D.Instance = new Physics3D(new JoltPhysics3D());

            Physics3D.Instance.Startup();

            var ray = new Ray(new Vector3(0, 0, 1), new Vector3(0, 0, -1));

            Assert.IsTrue(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One, Vector3.Zero, Quaternion.Identity, BodyMotionType.Dynamic, 0, out var body));

            Assert.IsTrue(Physics.RayCast3D(ray, out var target, out _));

            Assert.That(target, Is.EqualTo(body));
        }
    }
}
