using Staple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TestGame
{
    public class KeyboardControlSystem : IEntitySystem
    {
        public Type[] targetComponents { get; private set; } = new Type[] { typeof(KeyboardControlComponent) };

        public void Process(Entity entity, float deltaTime)
        {
            var component = entity.GetComponent<KeyboardControlComponent>();

            var direction = Vector3.Zero;

            if(Input.GetKey(KeyCode.A))
            {
                direction.X = -1;
            }

            if(Input.GetKey(KeyCode.D))
            {
                direction.X = 1;
            }

            if(Input.GetKey(KeyCode.W))
            {
                direction.Y = -1;
            }

            if(Input.GetKey(KeyCode.S))
            {
                direction.Y = 1;
            }

            entity.Transform.LocalPosition += direction * component.speed * deltaTime;
        }
    }
}
