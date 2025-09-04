using System.Numerics;

namespace Staple;

public interface IInputReceiver : IComponent
{
    void OnPressed(InputActionContext context);

    void OnAxis(InputActionContext context, float value);

    void OnDualAxis(InputActionContext context, Vector2 value);
}
