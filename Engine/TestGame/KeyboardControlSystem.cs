using Staple;
using System;
using System.Numerics;

namespace TestGame
{
    public class KeyboardControlSystem : IEntitySystem
    {
        public void Process(World world, float deltaTime)
        {
            world.ForEach((Entity entity, ref KeyboardControlComponent component, ref Transform transform) =>
            {
                var direction = Vector3.Zero;

                if (Input.GetKey(KeyCode.A))
                {
                    direction.X = -1;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    direction.X = 1;
                }

                if (Input.GetKey(KeyCode.W))
                {
                    direction.Y = -1;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    direction.Y = 1;
                }

                transform.LocalPosition += direction * component.speed * deltaTime;
            });
        }
    }
}
