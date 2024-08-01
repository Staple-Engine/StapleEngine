using Hexa.NET.ImGui;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Staple.Editor;

/// <summary>
/// Handles the navigation and listing of project items
/// </summary>
internal class ProjectBrowser
{
    /// <summary>
    /// All valid resource types
    /// </summary>
    public static Dictionary<string, ProjectBrowserResourceType> resourceTypes = new()
    {
        { ".asset", ProjectBrowserResourceType.Asset },
        { ".mat", ProjectBrowserResourceType.Material },
        { ".stsh", ProjectBrowserResourceType.Shader },
        { ".stsc", ProjectBrowserResourceType.Scene },
        { ".stpr", ProjectBrowserResourceType.Prefab },
    };

    static ProjectBrowser()
    {
        void AddAll(string[] extensions, ProjectBrowserResourceType type)
        {
            foreach (var ext in extensions)
            {
                resourceTypes.Add($".{ext}", type);
            }
        }

        AddAll(AssetSerialization.TextureExtensions, ProjectBrowserResourceType.Texture);
        AddAll(AssetSerialization.AudioExtensions, ProjectBrowserResourceType.Audio);
        AddAll(AssetSerialization.MeshExtensions, ProjectBrowserResourceType.Mesh);
        AddAll(AssetSerialization.FontExtensions, ProjectBrowserResourceType.Font);
    }

    public const float contentPanelThumbnailSize = 64;

    public const float contentPanelPadding = 16;

    /// <summary>
    /// The location of the project
    /// </summary>
    public string basePath;

    /// <summary>
    /// The current platform we're using
    /// </summary>
    public AppPlatform currentPlatform;

    /// <summary>
    /// Which drag and drop operation we're doing right now
    /// </summary>
    public static ProjectBrowserDropType dropType = ProjectBrowserDropType.None;

    /// <summary>
    /// The current list of project browser nodes/items
    /// </summary>
    internal List<ProjectBrowserNode> projectBrowserNodes = new();

    /// <summary>
    /// The currently selected project browser node
    /// </summary>
    public ProjectBrowserNode currentContentNode;

    /// <summary>
    /// The currently browsable project browser nodes
    /// </summary>
    private readonly List<ImGuiUtils.ContentGridItem> currentContentBrowserNodes = new();

    /// <summary>
    /// All nodes in the project
    /// </summary>
    private List<ProjectBrowserNode> allNodes = new();

    /// <summary>
    /// Local editor resources for rendering
    /// </summary>
    internal Dictionary<string, Texture> editorResources = new();

    /// <summary>
    /// Gets an editor resource if able
    /// </summary>
    /// <param name="name">The resource name</param>
    /// <returns>The texture or null</returns>
    public Texture GetEditorResource(string name)
    {
        return editorResources.TryGetValue(name, out var texture) ? texture : null;
    }

    /// <summary>
    /// Attempts to load an editor texture
    /// </summary>
    /// <param name="name">The resource name</param>
    /// <param name="path">The texture path</param>
    public void LoadEditorTexture(string name, string path)
    {
        path = Path.Combine(Environment.CurrentDirectory, "Editor Resources", path);

        try
        {
            var texture = Texture.CreateStandard(path, File.ReadAllBytes(path), StandardTextureColorComponents.RGBA);

            if (texture != null)
            {
                editorResources.Add(name, texture);
            }
        }
        catch (Exception)
        {
        }
    }

    internal static ProjectBrowserResourceType ResourceTypeForExtension(string extension)
    {
        if(resourceTypes.TryGetValue(extension, out var type))
        {
            return type;
        }

        return ProjectBrowserResourceType.Other;
    }

    /// <summary>
    /// Updates all project browser nodes
    /// </summary>
    public void UpdateProjectBrowserNodes()
    {
        allNodes.Clear();

        if (basePath == null)
        {
            projectBrowserNodes.Clear();
        }
        else
        {
            projectBrowserNodes.Clear();

            void Recursive(string p, List<ProjectBrowserNode> nodes)
            {
                string[] directories = Array.Empty<string>();
                string[] files = Array.Empty<string>();

                try
                {
                    directories = Directory.GetDirectories(p);
                }
                catch (Exception)
                {
                }

                try
                {
                    files = Directory.GetFiles(p);
                }
                catch (Exception)
                {
                }

                foreach (var directory in directories)
                {
                    var subnodes = new List<ProjectBrowserNode>();

                    Recursive(directory, subnodes);

                    var node = new ProjectBrowserNode()
                    {
                        name = Path.GetFileName(directory),
                        extension = "",
                        path = directory.Replace("\\", "/"),
                        type = ProjectBrowserNodeType.Folder,
                        subnodes = subnodes,
                        typeName = typeof(FolderAsset).FullName,
                    };

                    nodes.Add(node);

                    allNodes.Add(node);
                }

                foreach (var file in files)
                {
                    if (file.EndsWith(".meta"))
                    {
                        continue;
                    }

                    var node = new ProjectBrowserNode()
                    {
                        name = Path.GetFileNameWithoutExtension(file),
                        extension = Path.GetExtension(file).ToLowerInvariant(),
                        path = file.Replace("\\", "/"),
                        subnodes = new(),
                        type = ProjectBrowserNodeType.File
                    };

                    nodes.Add(node);

                    allNodes.Add(node);

                    switch (ResourceTypeForExtension(node.extension))
                    {
                        case ProjectBrowserResourceType.Asset:

                            try
                            {
                                var text = File.ReadAllText($"{file}.meta");
                                var holder = JsonConvert.DeserializeObject<AssetHolder>(text);

                                if(holder != null)
                                {
                                    node.typeName = holder.typeName;
                                }
                            }
                            catch(Exception)
                            {
                                node.typeName = "";
                            }

                            break;

                        case ProjectBrowserResourceType.Material:

                            node.typeName = typeof(Material).FullName;

                            break;

                        case ProjectBrowserResourceType.Shader:

                            node.typeName = typeof(Shader).FullName;

                            break;

                        case ProjectBrowserResourceType.Scene:

                            node.action = ProjectBrowserNodeAction.InspectScene;
                            node.typeName = typeof(Scene).FullName;

                            break;

                        case ProjectBrowserResourceType.Texture:

                            node.typeName = typeof(Texture).FullName;

                            break;

                        case ProjectBrowserResourceType.Audio:

                            node.typeName = typeof(AudioClip).FullName;

                            break;

                        case ProjectBrowserResourceType.Font:

                            node.typeName = typeof(FontAsset).FullName;

                            break;

                        case ProjectBrowserResourceType.Mesh:

                            node.typeName = typeof(Mesh).FullName;

                            break;

                        case ProjectBrowserResourceType.Prefab:

                            node.typeName = typeof(Prefab).FullName;

                            break;

                        default:

                            node.typeName = "Unknown";

                            break;
                    }
                }
            }

            Recursive(Path.Combine(basePath, "Assets"), projectBrowserNodes);

            var currentPath = currentContentNode?.path;

            currentContentNode = allNodes.FirstOrDefault(x => x.path == currentPath);

            UpdateCurrentContentNodes(currentContentNode?.subnodes ?? projectBrowserNodes);
        }
    }

    /// <summary>
    /// Updates the current visible content nodes
    /// </summary>
    /// <param name="nodes">The base nodes we have access to right now</param>
    public void UpdateCurrentContentNodes(List<ProjectBrowserNode> nodes)
    {
        currentContentBrowserNodes.Clear();

        if(nodes != projectBrowserNodes)
        {
            ThumbnailCache.Clear();
        }

        foreach (var node in nodes)
        {
            if (node.path.EndsWith(".meta"))
            {
                continue;
            }

            var item = new ImGuiUtils.ContentGridItem()
            {
                name = node.name,
            };

            switch (node.type)
            {
                case ProjectBrowserNodeType.File:

                    item.ensureValidTexture = (texture) =>
                    {
                        if ((texture?.Disposed ?? true) || ThumbnailCache.HasCachedThumbnail(node.path))
                        {
                            return ThumbnailCache.GetThumbnail(node.path) ?? GetEditorResource("FileIcon");
                        }

                        return texture;
                    };

                    break;

                case ProjectBrowserNodeType.Folder:

                    item.ensureValidTexture = (texture) =>
                    {
                        if (texture?.Disposed ?? true)
                        {
                            return GetEditorResource("FolderIcon");
                        }

                        return texture;
                    };

                    break;
            }

            currentContentBrowserNodes.Add(item);
        }
    }

    /// <summary>
    /// Creates any missing meta files
    /// </summary>
    public void CreateMissingMetaFiles()
    {
        string[] metaFiles = [];

        try
        {
            metaFiles = Directory.GetFiles(basePath, "*.meta", SearchOption.AllDirectories);
        }
        catch(Exception)
        {
        }

        foreach (var file in metaFiles)
        {
            try
            {
                var local = file[..^".meta".Length];

                var valid = File.Exists(local) || Directory.Exists(local);

                if(valid == false)
                {
                    File.Delete(file);
                }
            }
            catch(Exception)
            {
            }
        }

        static void Recursive(List<ProjectBrowserNode> nodes)
        {
            foreach (var node in nodes)
            {
                static string Hash()
                {
                    //Guid collision fix
                    Thread.Sleep(25);

                    return Guid.NewGuid().ToString();
                }

                if (node.type == ProjectBrowserNodeType.Folder)
                {
                    try
                    {
                        if (File.Exists($"{node.path}.meta") == false)
                        {
                            var holder = new FolderAsset()
                            {
                                guid = Hash(),
                                typeName = typeof(FolderAsset).FullName,
                            };

                            var json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                            File.WriteAllText($"{node.path}.meta", json);
                        }
                    }
                    catch (Exception)
                    {
                    }

                    Recursive(node.subnodes);
                }
                else
                {
                    if(resourceTypes.TryGetValue(node.extension, out var type))
                    {
                        switch(type)
                        {
                            case ProjectBrowserResourceType.Texture:

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new TextureMetadata()
                                        {
                                            guid = Hash(),
                                        },
                                        Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText($"{node.path}.meta", jsonData);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                break;

                            case ProjectBrowserResourceType.Material:

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(Material).FullName,
                                        },
                                        Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText($"{node.path}.meta", jsonData);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                break;

                            case ProjectBrowserResourceType.Shader:

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(Shader).FullName,
                                        },
                                        Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText($"{node.path}.meta", jsonData);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                break;

                            case ProjectBrowserResourceType.Scene:

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(Scene).FullName,
                                        },
                                        Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText($"{node.path}.meta", jsonData);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                break;

                            case ProjectBrowserResourceType.Mesh:

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new MeshAssetMetadata()
                                        {
                                            guid = Hash(),
                                        },
                                        Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText($"{node.path}.meta", jsonData);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                break;

                            case ProjectBrowserResourceType.Audio:

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(AudioClip).FullName,
                                        },
                                        Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText($"{node.path}.meta", jsonData);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                break;

                            case ProjectBrowserResourceType.Font:

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(FontAsset).FullName,
                                        },
                                        Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText($"{node.path}.meta", jsonData);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                break;

                            case ProjectBrowserResourceType.Asset:

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var text = File.ReadAllText(node.path);
                                        var holder = JsonConvert.DeserializeObject<AssetHolder>(text);

                                        if (holder != null)
                                        {
                                            var json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                            File.WriteAllText($"{node.path}.meta", json);
                                        }
                                        else
                                        {
                                            var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                            {
                                                guid = Hash(),
                                                typeName = "Unknown",
                                            },
                                            Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                            File.WriteAllText($"{node.path}.meta", jsonData);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                break;

                            case ProjectBrowserResourceType.Prefab:

                                try
                                {
                                    if (File.Exists($"{node.path}.meta") == false)
                                    {
                                        var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                        {
                                            guid = Hash(),
                                            typeName = typeof(Prefab).FullName,
                                        },
                                        Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                        File.WriteAllText($"{node.path}.meta", jsonData);
                                    }
                                }
                                catch (Exception)
                                {
                                }

                                break;
                        }
                    }
                    else
                    {
                        if (node.path.EndsWith(".meta") == false)
                        {
                            try
                            {
                                if (File.Exists($"{node.path}.meta") == false)
                                {
                                    var holder = new AssetHolder()
                                    {
                                        guid = Hash(),
                                        typeName = "Unknown",
                                    };

                                    var json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                    File.WriteAllText($"{node.path}.meta", json);
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
        }

        Recursive(projectBrowserNodes);
    }

    /// <summary>
    /// Draws the project browser in the editor GUI
    /// </summary>
    /// <param name="io">The ImGUI IO Pointer</param>
    /// <param name="onClick">Callback when an item is clicked</param>
    /// <param name="onDoubleClick">Callback when an item is double clicked</param>
    /// <returns>Whether the window is hovered</returns>
    public bool Draw(ImGuiIOPtr io, Action<ProjectBrowserNode> onClick, Action<ProjectBrowserNode> onDoubleClick)
    {
        ImGui.Columns(2, "ProjectBrowserContent");

        var nextNode = currentContentNode;

        void Recursive(ProjectBrowserNode node)
        {
            if (node.type != ProjectBrowserNodeType.Folder)
            {
                return;
            }

            var flags = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow;
            var hasChildren = node.subnodes.Any(x => x.type == ProjectBrowserNodeType.Folder);

            if (hasChildren == false)
            {
                flags |= ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Leaf;
            }

            if (ImGui.TreeNodeEx($"{node.name}##0", flags))
            {
                if (ImGui.IsItemClicked())
                {
                    nextNode = node;
                }

                if (hasChildren)
                {
                    foreach (var subnode in node.subnodes)
                    {
                        Recursive(subnode);
                    }

                    ImGui.TreePop();
                }
            }
        }

        if (ImGui.TreeNodeEx("Assets", ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnArrow))
        {
            if (ImGui.IsItemClicked())
            {
                currentContentNode = nextNode = null;

                UpdateCurrentContentNodes(projectBrowserNodes);
            }

            foreach (var node in projectBrowserNodes)
            {
                Recursive(node);
            }

            ImGui.TreePop();
        }

        ImGui.NextColumn();

        if(nextNode != null && currentContentNode != nextNode)
        {
            currentContentNode = nextNode;

            UpdateCurrentContentNodes(nextNode.subnodes);
        }

        ImGui.BeginChild("ProjectBrowserContentAssets");

        ImGuiUtils.ContentGrid(currentContentBrowserNodes, contentPanelPadding, contentPanelThumbnailSize,
            "ASSET", true,
            (index, _) =>
            {
                ProjectBrowserNode item = null;

                if (currentContentNode == null)
                {
                    item = projectBrowserNodes[index];
                }
                else
                {
                    item = index >= 0 && index < currentContentNode.subnodes.Count ? currentContentNode.subnodes[index] : null;
                }

                if (item == null)
                {
                    return;
                }

                onClick?.Invoke(item);
            },
            (index, _) =>
            {
                ProjectBrowserNode item = null;

                if (currentContentNode == null)
                {
                    item = projectBrowserNodes[index];
                }
                else
                {
                    item = index >= 0 && index < currentContentNode.subnodes.Count ? currentContentNode.subnodes[index] : null;
                }

                if (item == null)
                {
                    return;
                }

                if (item.subnodes.Count == 0)
                {
                    if (item.type == ProjectBrowserNodeType.File)
                    {
                        onDoubleClick?.Invoke(item);
                    }
                }
                else
                {
                    currentContentNode = item;

                    UpdateCurrentContentNodes(item.subnodes);
                }
            },
            (index, _) =>
            {
                if(Scene.current == null)
                {
                    return;
                }

                ProjectBrowserNode item = null;

                if (currentContentNode == null)
                {
                    currentContentNode = projectBrowserNodes[index];

                    item = currentContentNode;
                }
                else
                {
                    item = currentContentNode.type == ProjectBrowserNodeType.Folder ? (index >= 0 && index < currentContentNode.subnodes.Count ? currentContentNode.subnodes[index] : null) :
                        currentContentNode;
                }

                if (item == null)
                {
                    return;
                }

                var cachePath = EditorUtils.GetLocalPath(item.path);

                var guid = AssetDatabase.GetAssetGuid(cachePath);

                if(guid == null)
                {
                    return;
                }

                switch(dropType)
                {
                    case ProjectBrowserDropType.Asset:

                        switch(item.typeName)
                        {
                            case string t when t == typeof(Prefab).FullName:

                                {
                                    var prefab = ResourceManager.instance.LoadPrefab(guid);

                                    if(prefab == null)
                                    {
                                        return;
                                    }

                                    var targetEntity = StapleEditor.instance.dropTargetEntity;

                                    Entity.Instantiate(prefab, targetEntity.GetComponent<Transform>());
                                }

                                break;

                            case string t when t == typeof(Mesh).FullName:

                                {
                                    var asset = ResourceManager.instance.LoadMeshAsset(guid);

                                    if(asset == null)
                                    {
                                        return;
                                    }

                                    var targetEntity = StapleEditor.instance.dropTargetEntity;

                                    EditorUtils.InstanceMesh(item.name, asset, targetEntity);
                                }

                                break;
                        }

                        break;

                    case ProjectBrowserDropType.SceneList:

                        if(BuildWindow.instance.TryGetTarget(out var buildWindow))
                        {
                            buildWindow.AddScene(guid);
                        }

                        break;
                }

                dropType = ProjectBrowserDropType.None;
            },
            (index, localItem, name) =>
            {
                var invalidChars = Path.GetInvalidPathChars()
                    .Concat(Path.GetInvalidFileNameChars())
                    .ToList();

                if(invalidChars.Any(x => name.Contains(x)))
                {
                    return;
                }

                ProjectBrowserNode item = null;

                if (currentContentNode == null)
                {
                    currentContentNode = projectBrowserNodes[index];

                    item = currentContentNode;
                }
                else
                {
                    item = currentContentNode.type == ProjectBrowserNodeType.Folder ? (index >= 0 && index < currentContentNode.subnodes.Count ? currentContentNode.subnodes[index] : null) :
                        currentContentNode;
                }

                if (item == null)
                {
                    return;
                }

                var normalizedPath = Path.Combine(Path.GetDirectoryName(item.path), $"{name}{Path.GetExtension(item.path) ?? ""}").Replace("\\", "/");

                switch(item.type)
                {
                    case ProjectBrowserNodeType.File:

                        try
                        {
                            File.Move(item.path, normalizedPath);

                            if(File.Exists($"{item.path}.meta"))
                            {
                                File.Move($"{item.path}.meta", $"{normalizedPath}.meta");
                            }
                        }
                        catch(Exception)
                        {
                        }

                        break;

                    case ProjectBrowserNodeType.Folder:

                        try
                        {
                            Directory.Move(item.path, normalizedPath);
                        }
                        catch(Exception)
                        {
                        }

                        break;
                }

                EditorUtils.RefreshAssets(null);
            },
            (index, localItem) =>
            {
                ProjectBrowserNode item = null;

                if (currentContentNode == null)
                {
                    currentContentNode = projectBrowserNodes[index];

                    item = currentContentNode;
                }
                else
                {
                    item = currentContentNode.type == ProjectBrowserNodeType.Folder ? (index >= 0 && index < currentContentNode.subnodes.Count ? currentContentNode.subnodes[index] : null) :
                        currentContentNode;
                }

                if (item == null)
                {
                    return;
                }

                try
                {
                    if(File.Exists(item.path))
                    {
                        File.Delete(item.path);
                    }
                    else if(Directory.Exists(item.path))
                    {
                        Directory.Delete(item.path, true);
                    }

                    File.Delete($"{item.path}.meta");
                }
                catch(Exception)
                {
                }

                EditorUtils.RefreshAssets(null);
            });

        var result = ImGui.IsWindowHovered();

        ImGui.EndChild();

        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload("ENTITY");

            unsafe
            {
                if (payload.Handle != null)
                {
                    StapleEditor.instance.CreatePrefabFromDragged();
                }
            }
        }

        ImGui.Columns();

        return result;
    }
}
