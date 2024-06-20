using Staple;
using System.Numerics;

namespace TestGame
{
    public class CircularMovementSystem : IEntitySystem
    {
        public EntitySubsystemType UpdateType => EntitySubsystemType.FixedUpdate;

        public void Startup()
        {
        }

        public void FixedUpdate(float deltaTime)
        {
            Scene.ForEach((Entity entity, ref CircularMovementComponent movement, ref Transform transform) =>
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
            });
        }

        public void Update(float deltaTime)
        {
        }

        public void Shutdown()
        {
        }
    }
}
