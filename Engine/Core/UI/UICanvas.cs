namespace Staple.UI;

/// <summary>
/// Canvas component, used as a container for UI.
/// </summary>
public class UICanvas : IComponent
{
    /// <summary>
    /// The UI Manager for this canvas
    /// </summary>
    public UIManager Manager { get; internal set; }

    /// <summary>
    /// The layout to load (if any)
    /// </summary>
    public TextAsset layout;
}
