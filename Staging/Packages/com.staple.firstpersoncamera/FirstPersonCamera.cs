using Staple;
using System.Numerics;

namespace Staple;

public class FirstPersonCamera : CallbackComponent
{
    public bool allowLook = true;
    public bool allowMove = true;

    public float sensitivity = 20;

    public float minVerticalRotation = -90;
    public float maxVerticalRotation = 90;

    public float moveSpeed = 5;

    public Entity focus;

    private Vector2 angles;
    private IBody3D body;

    protected virtual Vector2 GetLookMovement()
    {
        return Input.MouseRelativePosition;
    }

    protected virtual Vector2 GetCharacterMovement()
    {
        return new(Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0,
            Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0);
    }

    public override void Start()
    {
        Cursor.LockState = CursorLockMode.Locked;
        Cursor.Visible = false;

        body = Physics.GetBody3D(focus);

        if(body == null && Physics.GetBody3D(Entity) != null)
        {
            Log.Error($"First Person Camera: Warning: There's no body in the Focus entity, but the camera entity has it. Please use a focus entity.");
        }
    }

    public override void Update()
    {
        if(allowLook)
        {
            var movement = GetLookMovement() * sensitivity * Time.deltaTime;

            angles.X += movement.Y;
            angles.Y += movement.X;

            angles.X = Math.Clamp(angles.X, minVerticalRotation, maxVerticalRotation);

            angles.Y = Math.Repeat(angles.Y, 360);

            if (body != null)
            {
                body.Rotation = Quaternion.Euler(0, angles.Y, 0);

                Transform.LocalRotation = Quaternion.Euler(angles.X, 0, 0);
            }
            else
            {
                Transform.Rotation = Quaternion.Euler(angles.X, angles.Y, 0);
            }
        }

        if(allowMove)
        {
            var movement = GetCharacterMovement();

            var forward = Transform.Forward;

            forward.Y = 0.0f;

            if (forward != Vector3.Zero)
            {
                forward = forward.Normalized;
            }

            var right = Transform.Right;

            right.Y = 0.0f;

            if (right != Vector3.Zero)
            {
                right = right.Normalized;
            }

            var direction = (forward * movement.Y + right * movement.X);

            var velocity = direction * moveSpeed;

            if(body != null)
            {
                body.Velocity = velocity;
            }
            else
            {
                Transform.Position += velocity * Time.deltaTime;
            }
        }
    }
}
