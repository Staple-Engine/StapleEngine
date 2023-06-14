using Staple;
using System.Numerics;

namespace CoreTests
{
    internal class RaycastTests
    {
        [Test]
        public void TestBoxRaycast()
        {
            LayerMask.AllLayers.Add("Default");

            Physics3D.Instance = new Physics3D(new JoltPhysics3D());

            var ray = new Ray(new Vector3(0, 0, 1), new Vector3(0, 0, -1));

            Assert.IsTrue(Physics3D.Instance.CreateBox(Entity.Empty, Vector3.One * 2, Vector3.Zero, Quaternion.Identity, BodyMotionType.Dynamic, 0, false, 1, out var body));

            body.GravityFactor = 0;

            Assert.IsTrue(Physics.RayCast3D(ray, out var target, out _));

            Assert.That(target, Is.EqualTo(body));
        }
    }
}
