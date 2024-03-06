namespace Staple.Editor;

/// <summary>
/// Types of editor windows
/// </summary>
public enum EditorWindowType
{
    /// <summary>
    /// Regular window
    /// </summary>
    Normal,

    /// <summary>
    /// Modal (takes over the UI)
    /// </summary>
    Modal,

    /// <summary>
    /// Popup (Dismissable)
    /// </summary>
    Popup
}
