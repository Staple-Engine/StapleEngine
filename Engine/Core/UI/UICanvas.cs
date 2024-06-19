namespace Staple.UI;

/// <summary>
/// Canvas component, used as a container for UI.
/// </summary>
public class UICanvas : IComponent
{
    public UIInteractible focusedElement { get; internal set; }
}
