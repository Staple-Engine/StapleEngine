namespace Staple.UI;

/// <summary>
/// Canvas component, used as a container for UI.
/// </summary>
public class UICanvas : IComponent
{
    public readonly UIManager manager = new()
    {
        CanvasSize = new(Screen.Width, Screen.Height),
    };

    public TextAsset layout;
}
