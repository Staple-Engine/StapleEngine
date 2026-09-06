using Staple;
using System.Numerics;

namespace TestGame;

public class PlayerControlComponent : InputReceiver
{
    public float speed = 50;
    public bool is3D = false;

    internal Vector2 movement;
    internal Vector2 rotation;

    public override void OnAxis(InputActionContext context, float value)
    {
    }

    public override void OnDualAxis(InputActionContext context, Vector2 value)
    {
        switch(context.name.ToLowerInvariant())
        {
            case "movement":

                movement = value;

                break;

            case "rotation":

                rotation = value;

                break;
        }
    }

    public override void OnPressed(InputActionContext context)
    {
    }
}
