using Newtonsoft.Json;
using Staple.Internal;
using System.IO;
using System.Collections.Generic;
using Staple.ProjectManagement;
using System.Linq;

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
    /// The human-friendly version of the type name
    /// </summary>
    public string friendlyTypeName;

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

    /// <summary>
    /// Whether the tree node for this item is open
    /// </summary>
    public bool open;

    /// <summary>
    /// Updates node data from its extension and path
    /// </summary>
    public void UpdateData()
    {
        typeName = AssetDatabase.GetAssetType(path);

        if (string.IsNullOrEmpty(typeName))
        {

            switch (ProjectBrowser.ResourceTypeForExtension(extension))
            {
                case ProjectBrowserResourceType.Asset:

                    try
                    {
                        var text = File.ReadAllText($"{path}.meta");
                        var holder = JsonConvert.DeserializeObject<AssetHolder>(text);

                        if (holder != null)
                        {
                            typeName = holder.typeName;
                        }
                    }
                    catch (System.Exception)
                    {
                        typeName = "";
                    }

                    break;

                case ProjectBrowserResourceType.Material:

                    typeName = typeof(Material).FullName;

                    break;

                case ProjectBrowserResourceType.Shader:

                    typeName = typeof(Shader).FullName;

                    break;

                case ProjectBrowserResourceType.ComputeShader:

                    typeName = typeof(ComputeShader).FullName;

                    break;

                case ProjectBrowserResourceType.Scene:

                    action = ProjectBrowserNodeAction.InspectScene;
                    typeName = typeof(Scene).FullName;

                    break;

                case ProjectBrowserResourceType.Texture:

                    action = ProjectBrowserNodeAction.Open;
                    typeName = typeof(Texture).FullName;

                    break;

                case ProjectBrowserResourceType.Audio:

                    action = ProjectBrowserNodeAction.Open;
                    typeName = typeof(AudioClip).FullName;

                    break;

                case ProjectBrowserResourceType.Font:

                    typeName = typeof(FontAsset).FullName;

                    break;

                case ProjectBrowserResourceType.Mesh:

                    action = ProjectBrowserNodeAction.Open;
                    typeName = typeof(Mesh).FullName;

                    break;

                case ProjectBrowserResourceType.Prefab:

                    action = ProjectBrowserNodeAction.InspectScene;
                    typeName = typeof(Prefab).FullName;

                    break;

                case ProjectBrowserResourceType.AssemblyDefinition:

                    typeName = typeof(AssemblyDefinition).FullName;

                    break;

                case ProjectBrowserResourceType.Plugin:

                    typeName = typeof(PluginAsset).FullName;

                    break;

                case ProjectBrowserResourceType.Text:

                    action = ProjectBrowserNodeAction.Open;
                    typeName = typeof(TextAsset).FullName;

                    break;

                default:

                    action = ProjectBrowserNodeAction.Open;
                    typeName = "Unknown";

                    break;
            }
        }

        friendlyTypeName = typeName.Split('.').LastOrDefault()?.ExpandCamelCaseName() ?? typeName;
    }
}
