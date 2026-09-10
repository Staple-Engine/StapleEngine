using Staple;
using System.Numerics;

namespace TestGame;

public class CircularMovementSystem : IEntitySystemFixedUpdate
{
    private readonly SceneQuery<CircularMovementComponent> movements = new();

    public void FixedUpdate(float deltaTime)
    {
        foreach (var movement in movements.Contents)
        {
            movement.t += deltaTime * movement.speed;

            if (movement.followMouse)
            {
                movement.Transform.LocalPosition = Input.MousePosition.ToVector3();
            }
            else
            {
                movement.Transform.LocalPosition = new Vector3(Math.Cos(movement.t * Math.Deg2Rad) * movement.distance,
                    Math.Sin(movement.t * Math.Deg2Rad) * movement.distance,
                    0);
            }
        }
    }
}
