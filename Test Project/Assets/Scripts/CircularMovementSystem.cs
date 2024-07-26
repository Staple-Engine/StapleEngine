using Staple;
using System.Numerics;

namespace TestGame
{
    public class CircularMovementSystem : IEntitySystemFixedUpdate
    {
        public void Startup()
        {
        }

        public void FixedUpdate(float deltaTime)
        {
            var movements = Scene.Query<CircularMovementComponent, Transform>();

            foreach ((_, CircularMovementComponent movement, Transform transform) in movements)
            {
                movement.t += deltaTime * movement.speed;

                if (movement.followMouse)
                {
                    transform.LocalPosition = Input.MousePosition.ToVector3();
                }
                else
                {
                    transform.LocalPosition = new Vector3(Math.Cos(movement.t * Math.Deg2Rad) * movement.distance,
                        Math.Sin(movement.t * Math.Deg2Rad) * movement.distance,
                        0);
                }
            }
        }

        public void Shutdown()
        {
        }
    }
}
