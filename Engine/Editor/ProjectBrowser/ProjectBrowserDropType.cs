namespace Staple.Editor;

/// <summary>
/// Drag and Drop types for the project browser
/// </summary>
internal enum ProjectBrowserDropType
{
    /// <summary>
    /// No drag
    /// </summary>
    None,

    /// <summary>
    /// Dragging an asset
    /// </summary>
    Asset,

    /// <summary>
    /// Dropping an asset to an object picker
    /// </summary>
    AssetObjectPicker,

    /// <summary>
    /// Dragging a scene for the scene list
    /// </summary>
    SceneList
}
