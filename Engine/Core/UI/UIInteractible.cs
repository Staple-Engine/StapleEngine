namespace Staple.UI;

public abstract class UIInteractible : IComponent
{
    /// <summary>
    /// Whether we're currently focused
    /// </summary>
    public bool Focused { get; internal set; }

    /// <summary>
    /// Whether we've been clicked
    /// </summary>
    public bool Clicked { get; internal set; }

    /// <summary>
    /// Whether we're currently mouse hovered
    /// </summary>
    public bool Hovered { get; internal set; }

    public abstract void Interact();
}
