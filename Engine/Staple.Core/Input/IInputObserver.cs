using System.Numerics;

namespace Staple;

public interface IInputObserver
{
    void OnMouseButtonJustPressed(MouseButton button);

    void OnMouseButtonPressed(MouseButton button);

    void OnMouseButtonReleased(MouseButton button);

    void OnMouseMove(Vector2Int position);

    void OnMouseWheelScrolled(Vector2 delta);

    void OnKeyJustPressed(KeyCode key);

    void OnKeyPressed(KeyCode key);

    void OnKeyReleased(KeyCode key);

    void OnCharacterEntered(char character);
}
