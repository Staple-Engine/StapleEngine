namespace Staple.UI;

/// <summary>
/// Canvas component, used as a container for UI.
/// </summary>
public class UICanvas : IComponent
{
    /// <summary>
    /// The UI Manager for this canvas
    /// </summary>
    public readonly UIManager manager = new();

    /// <summary>
    /// The layout to load (if any)
    /// </summary>
    public TextAsset layout;

    /// <summary>
    /// The last used layout in this canvas
    /// </summary>
    internal TextAsset lastLayout;
}
