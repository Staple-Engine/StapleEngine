using Staple;
using System.Numerics;

namespace TestGame;

public class PlayerControlComponent : IComponent, IInputReceiver
{
    public float speed = 50;
    public bool is3D = false;

    internal Vector2 movement;
    internal Vector2 rotation;

    public void OnAxis(InputActionContext context, float value)
    {
    }

    public void OnDualAxis(InputActionContext context, Vector2 value)
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

    public void OnPressed(InputActionContext context)
    {
    }
}
