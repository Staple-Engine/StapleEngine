using System.Collections.Generic;

namespace Staple.Editor;

/// <summary>
/// Contains data on a project item
/// </summary>
internal class ProjectBrowserNode
{
    /// <summary>
    /// The item name
    /// </summary>
    public string name;

    /// <summary>
    /// The item path
    /// </summary>
    public string path;

    /// <summary>
    /// The type of node
    /// </summary>
    public ProjectBrowserNodeType type;

    /// <summary>
    /// The file extension
    /// </summary>
    public string extension;

    /// <summary>
    /// The related type name
    /// </summary>
    public string typeName;

    /// <summary>
    /// The parent node of this node
    /// </summary>
    public ProjectBrowserNode parent;

    /// <summary>
    /// The subnodes in this project item
    /// </summary>
    public List<ProjectBrowserNode> subnodes = [];

    /// <summary>
    /// The action to execute on double click
    /// </summary>
    public ProjectBrowserNodeAction action = ProjectBrowserNodeAction.None;
}
