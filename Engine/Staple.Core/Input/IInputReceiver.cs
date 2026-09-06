using System.Numerics;

namespace Staple;

public abstract class InputReceiver : Component
{
    public abstract void OnPressed(InputActionContext context);

    public abstract void OnAxis(InputActionContext context, float value);

    public abstract void OnDualAxis(InputActionContext context, Vector2 value);
}
