namespace Staple.Editor;

/// <summary>
/// Type of project browser item
/// </summary>
internal enum ProjectBrowserNodeType
{
    /// <summary>
    /// File (Inspect on click)
    /// </summary>
    File,

    /// <summary>
    /// Folder (should open the contents in the browser on double click)
    /// </summary>
    Folder
}
