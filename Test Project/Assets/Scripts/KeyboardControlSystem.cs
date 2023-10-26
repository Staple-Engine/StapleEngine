using Staple;
using System.Numerics;

namespace TestGame
{
    public class KeyboardControlSystem : IEntitySystem
    {
        public SubsystemType UpdateType => SubsystemType.Update;

        public void Startup()
        {
        }

        public void Process(World world, float deltaTime)
        {
            world.ForEach((Entity entity, bool enabled, ref KeyboardControlComponent component, ref Transform transform) =>
            {
                if(enabled == false)
                {
                    return;
                }

                var targetRotation = Quaternion.Identity;
                var targetDirection = Vector3.Zero;

                var direction = Vector3.Zero;

                if (Input.GetKey(KeyCode.A))
                {
                    direction = transform.Left;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    direction = transform.Right;
                }

                if (Input.GetKey(KeyCode.W))
                {
                    if(component.is3D)
                    {
                        direction += transform.Forward;
                    }
                    else
                    {
                        direction.Y = 1;
                    }
                }

                if (Input.GetKey(KeyCode.S))
                {
                    if (component.is3D)
                    {
                        direction += transform.Back;
                    }
                    else
                    {
                        direction.Y = -1;
                    }
                }

                if(component.is3D)
                {
                    var rotation = Math.ToEulerAngles(transform.LocalRotation);

                    rotation.X -= Input.MouseRelativePosition.Y;
                    rotation.Y -= Input.MouseRelativePosition.X;

                    targetRotation = Math.FromEulerAngles(rotation);
                }

                targetDirection = direction * component.speed;

                var body = Physics.GetBody3D(entity);

                if(body != null)
                {
                    body.Velocity = targetDirection;
                    body.Rotation = targetRotation;
                }
                else
                {
                    transform.LocalPosition += targetDirection * deltaTime;
                    transform.LocalRotation = targetRotation;
                }
            });
        }

        public void Shutdown()
        {
        }
    }
}
