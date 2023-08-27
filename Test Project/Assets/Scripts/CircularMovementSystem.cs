using Staple;
using System.Numerics;
using Math = Staple.Math;

namespace TestGame
{
    public class CircularMovementSystem : IEntitySystem
    {
        public SubsystemType UpdateType => SubsystemType.FixedUpdate;

        public void Process(World world, float deltaTime)
        {
            world.ForEach((Entity entity, ref CircularMovementComponent movement, ref Transform transform) =>
            {
                movement.t += deltaTime * movement.speed;

                if (movement.followMouse)
                {
                    transform.LocalPosition = Input.MousePosition.ToVector3();
                }
                else
                {
                    transform.LocalPosition = new Vector3(Math.Cos(Math.Deg2Rad(movement.t)) * movement.distance,
                        Math.Sin(Math.Deg2Rad(movement.t)) * movement.distance,
                        0);
                }
            });
        }
    }
}
