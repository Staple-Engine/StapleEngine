namespace Staple.UI;

/// <summary>
/// Canvas component, used as a container for UI.
/// </summary>
public class UICanvas : CallbackComponent, IComponentDisposable
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

    public override void Awake()
    {
        CheckLayoutChanges();
    }

    /// <summary>
    /// Checks for layout asset changes
    /// </summary>
    internal void CheckLayoutChanges()
    {
        manager.CanvasSize = new(Screen.Width, Screen.Height);

        if (layout != lastLayout)
        {
            manager.Clear();

            lastLayout = layout;

            if (!string.IsNullOrEmpty(layout?.text))
            {
                manager.LoadLayouts(layout.text);
            }
        }
    }

    public void DisposeComponent()
    {
        manager.UnregisterInput();
    }
}
