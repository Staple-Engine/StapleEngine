using Staple;
using System.Numerics;

namespace TestGame;

public class PlayerControlSystem : IEntitySystemUpdate
{
    private readonly SceneQuery<PlayerControlComponent, Transform> keyboards = new();

    public void Update(float deltaTime)
    {
        foreach((Entity entity, PlayerControlComponent component, Transform transform) in keyboards.Contents)
        {
            var targetRotation = Quaternion.Identity;

            var direction = Vector3.Zero;

            if (component.movement.X < 0)
            {
                direction = transform.Left;
            }

            if (component.movement.X > 0)
            {
                direction = transform.Right;
            }

            if (component.movement.Y > 0)
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

            if (component.movement.Y < 0)
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
                var rotation = transform.LocalRotation.ToEulerAngles();

                rotation.X -= component.rotation.Y;
                rotation.Y -= component.rotation.X;

                targetRotation = Quaternion.Euler(rotation);
            }

            var targetDirection = direction * component.speed;

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

            component.rotation = Vector2.Zero;
            component.movement = Vector2.Zero;
        }
    }
}
