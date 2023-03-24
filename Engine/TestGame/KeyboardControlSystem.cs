using Staple;
using System.Numerics;

namespace TestGame
{
    public class KeyboardControlSystem : IEntitySystem
    {
        public SubsystemType UpdateType => SubsystemType.Update;

        public void Process(World world, float deltaTime)
        {
            world.ForEach((Entity entity, ref KeyboardControlComponent component, ref Transform transform) =>
            {
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
                        direction.Y = -1;
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
                        direction.Y = 1;
                    }
                }

                if(component.is3D)
                {
                    var rotation = Math.ToEulerAngles(transform.LocalRotation);

                    rotation.X += Input.MouseRelativePosition.Y;
                    rotation.Y += Input.MouseRelativePosition.X;

                    transform.LocalRotation = Math.FromEulerAngles(rotation);
                }

                transform.LocalPosition += direction * component.speed * deltaTime;
            });
        }
    }
}
