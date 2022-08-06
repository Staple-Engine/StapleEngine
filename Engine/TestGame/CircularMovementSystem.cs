using Staple;
using System;
using System.Numerics;
using Math = Staple.Math;

namespace TestGame
{
    public class CircularMovementSystem : IEntitySystem
    {
        public Type[] targetComponents { get; private set; } = new Type[] { typeof(CircularMovementComponent) };

        public void Process(Entity entity)
        {
            var component = entity.GetComponent<CircularMovementComponent>();

            component.t += Time.deltaTime * component.speed;

            if(component.followMouse)
            {
                entity.Transform.LocalPosition = Input.MousePosition.ToVector3();
            }
            else
            {
                entity.Transform.LocalPosition = new Vector3(Math.Cos(Math.Deg2Rad(component.t)) * component.distance,
                    Math.Sin(Math.Deg2Rad(component.t)) * component.distance,
                    0);
            }
        }
    }
}