using Staple;
using System.Numerics;

namespace TestGame;

public class PlayerControlSystem : IEntitySystemUpdate
{
    private readonly SceneQuery<PlayerControlComponent> keyboards = new();

    public void Update(float deltaTime)
    {
        foreach(var controller in keyboards.Contents)
        {
            var targetRotation = Quaternion.Identity;

            var direction = Vector3.Zero;

            if (controller.movement.X < 0)
            {
                direction = controller.Transform.Left;
            }

            if (controller.movement.X > 0)
            {
                direction = controller.Transform.Right;
            }

            if (controller.movement.Y > 0)
            {
                if(controller.is3D)
                {
                    direction += controller.Transform.Forward;
                }
                else
                {
                    direction.Y = 1;
                }
            }

            if (controller.movement.Y < 0)
            {
                if (controller.is3D)
                {
                    direction += controller.Transform.Back;
                }
                else
                {
                    direction.Y = -1;
                }
            }

            if(controller.is3D)
            {
                var rotation = controller.Transform.LocalRotation.ToEulerAngles();

                rotation.X -= controller.rotation.Y;
                rotation.Y -= controller.rotation.X;

                targetRotation = Quaternion.Euler(rotation);
            }

            var targetDirection = direction * controller.speed;

            var body = Physics.GetBody3D(controller.Entity);

            if(body != null)
            {
                body.Velocity = targetDirection;
                body.Rotation = targetRotation;
            }
            else
            {
                controller.Transform.LocalPosition += targetDirection * deltaTime;
                controller.Transform.LocalRotation = targetRotation;
            }

            controller.rotation = Vector2.Zero;
            controller.movement = Vector2.Zero;
        }
    }
}
