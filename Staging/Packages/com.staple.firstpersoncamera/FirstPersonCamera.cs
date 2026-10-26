using System.Numerics;

namespace Staple;

public class FirstPersonCamera : LifecycleComponent
{
    public bool allowLook = true;
    public bool allowMove = true;

    [Tooltip("Will move on the Y axis too (requires allowMove and allowLook)")]
    public bool allowVerticalMovement = false;

    public float mouseSensitivity = 20;

    public float gamepadSensitivity = 100;

    public float touchSensitivity = 20;

    [Range(0, 1)]
    public float touchLookDeadZone;

    [Range(0, 1)]
    public float touchMoveDeadZone;

    public float minVerticalRotation = -90;
    public float maxVerticalRotation = 90;

    public float moveSpeed = 5;

    public Entity focus;

    private Vector2 angles;
    private IBody3D body;

    private int movementKey;

    private int lookKey;

    private Vector2 movement;

    private Vector2 look;

    protected virtual Vector2 GetLookMovement()
    {
        if (!Enabled || !Platform.IsPlaying)
        {
            return default;
        }

        if (Platform.IsDesktopPlatform)
        {
            return Cursor.LockState == CursorLockMode.Locked ? look : default;
        }
        else if (Platform.IsMobilePlatform)
        {
            return look;
        }

        return default;
    }

    protected virtual Vector2 GetCharacterMovement()
    {
        if (!Enabled || !Platform.IsPlaying)
        {
            return default;
        }

        if (Platform.IsDesktopPlatform)
        {
            return Cursor.LockState == CursorLockMode.Locked ? movement : default;
        }
        else if (Platform.IsMobilePlatform)
        {
            return movement;
        }

        return default;
    }

    public override void Start()
    {
        body = Physics.GetBody3D(focus);

        if(body == null && Physics.GetBody3D(Entity) != null)
        {
            Log.Error($"First Person Camera: Warning: There's no body in the Focus entity, but the camera entity has it. Please use a focus entity.");
        }

        if (Platform.IsDesktopPlatform)
        {
            Cursor.LockState = CursorLockMode.Locked;
            Cursor.Visible = false;
        }

        lookKey = Input.AddDualAxisAction(new()
        {
            type = InputActionType.DualAxis,
            devices =
            [
                new()
                {
                    device = InputDevice.Gamepad,
                    gamepad = new()
                    {
                        firstAxis = GamepadAxis.RightX,
                        secondAxis = GamepadAxis.RightY,
                        sensitivity = gamepadSensitivity,
                    }
                },
                new()
                {
                    device = InputDevice.Mouse,
                    mouse = new()
                    {
                        horizontal = true,
                        vertical = true,
                        sensitivity = mouseSensitivity,
                    }
                },
                new()
                {
                    device = InputDevice.Touch,
                    touch = new()
                    {
                        affectedArea = new(0.5f, 1, 0, 1),
                        sensitivity = touchSensitivity,
                        requireMovement = true,
                        deadZonePercentage = touchLookDeadZone,
                    }
                }
            ],
        },
        (InputActionContext context, Vector2 value) =>
        {
            look.X = value.X;
            look.Y = -value.Y;
        });

        movementKey = Input.AddDualAxisAction(new()
        {
            type = InputActionType.DualAxis,
            devices =
            [
                new()
                {
                    device = InputDevice.Gamepad,
                    gamepad = new()
                    {
                        firstAxis = GamepadAxis.LeftX,
                        secondAxis = GamepadAxis.LeftY,
                    }
                },
                new()
                {
                    device = InputDevice.Keyboard,
                    keys = new()
                    {
                        secondPositive = KeyCode.W,
                        secondNegative = KeyCode.S,
                        firstNegative = KeyCode.A,
                        firstPositive = KeyCode.D,
                    }
                },
                new()
                {
                    device = InputDevice.Touch,
                    touch = new()
                    {
                        affectedArea = new(0, 0.5f, 0, 1),
                        deadZonePercentage = touchMoveDeadZone,
                    }
                }
            ],
        },
        (InputActionContext context, Vector2 value) =>
        {
            movement = value;
        });
    }

    public override void Update()
    {
        if (!Enabled ||
            !Platform.IsPlaying ||
            (Platform.IsDesktopPlatform && Cursor.LockState != CursorLockMode.Locked))
        {
            return;
        }

        if(allowLook)
        {
            var movement = GetLookMovement() * Time.deltaTime;

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

        if (allowMove)
        {
            var movement = GetCharacterMovement();

            var forward = Transform.Forward;

            if (!allowVerticalMovement)
            {
                forward.Y = 0.0f;
            }

            if (forward != Vector3.Zero)
            {
                forward = forward.Normalized;
            }

            var right = Transform.Right;

            if (!allowVerticalMovement)
            {
                right.Y = 0.0f;
            }

            if (right != Vector3.Zero)
            {
                right = right.Normalized;
            }

            var direction = (forward * movement.Y + right * movement.X);

            var velocity = direction * moveSpeed;

            if (body != null)
            {
                body.Velocity = velocity;
            }
            else
            {
                Transform.Position += velocity * Time.deltaTime;
            }
        }

        movement = default;
        look = default;
    }
}
