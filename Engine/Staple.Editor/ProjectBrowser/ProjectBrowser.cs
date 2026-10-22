using Hexa.NET.ImGui;
using Newtonsoft.Json;
using Staple.Internal;
using Staple.Jobs;
using Staple.ProjectManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Staple.Editor;

/// <summary>
/// Handles the navigation and listing of project items
/// </summary>
internal class ProjectBrowser
{
    internal class ContentGridItem : ImGuiUtils.ContentGridItem
    {
        public readonly ProjectBrowser owner;
        public readonly ProjectBrowserNode node;
        public readonly bool isSearch;

        public ContentGridItem(ProjectBrowser owner, ProjectBrowserNode node, bool isSearch)
        {
            this.owner = owner;
            this.node = node;
            this.isSearch = isSearch;

            name = node.name;
            ID = $"{node.path}/{node.name}.{node.extension}";
        }

        public override Texture EnsureValidTexture(Texture existing)
        {
            switch (node.type)
            {
                case ProjectBrowserNodeType.File:

                    {
                        if (StapleEditor.instance.RefreshingAssets)
                        {
                            return owner.GetResourceIcon(ResourceTypeForExtension(node.extension));
                        }

                        if ((texture?.Disposed ?? true) || ThumbnailCache.HasCachedThumbnail(node.path))
                        {
                            return ThumbnailCache.GetThumbnail(node.path) ??
                                owner.GetResourceIcon(ResourceTypeForExtension(node.extension));
                        }
                    };

                    break;

                case ProjectBrowserNodeType.Folder:

                    if (StapleEditor.instance.RefreshingAssets)
                    {
                        return owner.GetEditorResource("FolderIcon");
                    }

                    if (texture?.Disposed ?? true || ThumbnailCache.HasCachedThumbnail(node.path))
                    {
                        var resourceType = ResourceTypeForExtension(node.extension);

                        Texture icon = resourceType == ProjectBrowserResourceType.Other ? owner.GetEditorResource("FolderIcon") :
                            owner.GetResourceIcon(resourceType);

                        return ThumbnailCache.GetThumbnail(node.path) ?? icon;
                    }

                    break;
            }

            return texture;
        }

        public override void NotVisible()
        {
            ThumbnailCache.RemoveRenderRequest(node.path);
        }

        public override void OnClick()
        {
            if (node == null)
            {
                return;
            }

            owner.onClick?.Invoke(node);
        }

        public override void OnDelete()
        {
            if(isSearch)
            {
                return;
            }

            try
            {
                if (File.Exists(node.path))
                {
                    File.Delete(node.path);
                }
                else if (Directory.Exists(node.path))
                {
                    Directory.Delete(node.path, true);
                }

                File.Delete($"{node.path}.meta");
            }
            catch (Exception)
            {
            }

            EditorUtils.RefreshAssets(null);
        }

        public override void OnDoubleClick()
        {
            if (node.subnodes.Count == 0 && node.type == ProjectBrowserNodeType.File)
            {
                owner.onDoubleClick?.Invoke(node);
            }
            else if(!isSearch)
            {
                owner.currentContentNode = node;

                node.open = true;

                owner.UpdateCurrentContentNodes(node.subnodes);
            }
        }

        public override void OnDragDropped()
        {
            if (Scene.current == null)
            {
                return;
            }

            var cachePath = EditorUtils.GetLocalPath(node.path);

            var guid = AssetDatabase.GetAssetGuid(cachePath);

            if (guid == null)
            {
                return;
            }

            static void SetObjectPicker(object t)
            {
                if (StapleEditor.instance.dropTargetObjectPickerAction != null &&
                    StapleEditor.instance.dropTargetObjectPickerType != null &&
                    t.GetType().IsAssignableTo(StapleEditor.instance.dropTargetObjectPickerType))
                {
                    StapleEditor.instance.dropTargetObjectPickerAction(t);

                    StapleEditor.instance.dropTargetEntity = default;
                    StapleEditor.instance.dropTargetObjectPickerAction = null;
                    StapleEditor.instance.dropTargetObjectPickerType = null;
                }
            }

            switch (dropType)
            {
                case ProjectBrowserDropType.Asset:

                    switch (node.typeName)
                    {
                        case string t when t == typeof(Prefab).FullName:

                            {
                                var prefab = ResourceManager.instance.LoadPrefab(guid);

                                if (prefab == null)
                                {
                                    return;
                                }

                                var targetEntity = StapleEditor.instance.dropTargetEntity;

                                Entity.Instantiate(prefab, targetEntity.GetComponent<Transform>());

                                StapleEditor.instance.dropTargetEntity = default;
                                StapleEditor.instance.dropTargetObjectPickerAction = null;
                                StapleEditor.instance.dropTargetObjectPickerType = null;
                            }

                            break;

                        case string t when t == typeof(Mesh).FullName:

                            {
                                var asset = ResourceManager.instance.LoadMeshAsset(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                var targetEntity = StapleEditor.instance.dropTargetEntity;

                                Mesh.InstanceMesh(node.name, asset, targetEntity);

                                StapleEditor.instance.dropTargetEntity = default;
                                StapleEditor.instance.dropTargetObjectPickerAction = null;
                                StapleEditor.instance.dropTargetObjectPickerType = null;

                                MemoryUtils.GarbageCollect(true);
                            }

                            break;
                    }

                    break;

                case ProjectBrowserDropType.AssetObjectPicker:

                    switch (node.typeName)
                    {
                        case string t when t == typeof(Prefab).FullName:

                            {
                                var prefab = ResourceManager.instance.LoadPrefab(guid);

                                if (prefab == null)
                                {
                                    return;
                                }

                                SetObjectPicker(prefab);
                            }

                            break;

                        case string t when t == typeof(Mesh).FullName:

                            {
                                object asset;

                                if (StapleEditor.instance.dropTargetObjectPickerAction != null &&
                                    StapleEditor.instance.dropTargetObjectPickerType != null &&
                                    StapleEditor.instance.dropTargetObjectPickerType == typeof(Mesh))
                                {
                                    asset = ResourceManager.instance.LoadMesh(guid);
                                }
                                else
                                {
                                    asset = ResourceManager.instance.LoadMeshAsset(guid);
                                }

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        case string t when t == typeof(Material).FullName:

                            {
                                var asset = ResourceManager.instance.LoadMaterial(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        case string t when t == typeof(Texture).FullName:

                            {
                                var asset = ResourceManager.instance.LoadTexture(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        case string t when t == typeof(Shader).FullName:

                            {
                                var asset = ResourceManager.instance.LoadShader(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        case string t when t == typeof(FontAsset).FullName:

                            {
                                var asset = ResourceManager.instance.LoadFont(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        case string t when t == typeof(TextAsset).FullName:

                            {
                                var asset = ResourceManager.instance.LoadTextAsset(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        case string t when t == typeof(ComputeShader).FullName:

                            {
                                var asset = ResourceManager.instance.LoadComputeShader(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        case string t when t == typeof(PluginAsset).FullName:

                            {
                                var asset = PluginAsset.Create(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        case string t when t == typeof(AssemblyDefinition).FullName:

                            {
                                var asset = AssemblyDefinition.Create(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        case string t when t == typeof(AudioClip).FullName:

                            {
                                var asset = ResourceManager.instance.LoadAudioClip(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;

                        default:

                            {
                                var asset = ResourceManager.instance.LoadAsset(guid);

                                if (asset == null)
                                {
                                    return;
                                }

                                SetObjectPicker(asset);
                            }

                            break;
                    }

                    break;

                case ProjectBrowserDropType.SceneList:

                    if (BuildWindow.instance.TryGetTarget(out var buildWindow))
                    {
                        buildWindow.AddScene(guid);
                    }

                    break;
            }

            dropType = ProjectBrowserDropType.None;
        }

        public override void OnRename(string name)
        {
            if(isSearch)
            {
                return;
            }

            var invalidChars = Path.GetInvalidPathChars()
                .Concat(Path.GetInvalidFileNameChars())
                .ToList();

            if (invalidChars.Any(x => name.Contains(x)))
            {
                return;
            }

            var normalizedPath = Path.Combine(Path.GetDirectoryName(node.path), $"{name}{Path.GetExtension(node.path) ?? ""}").Replace("\\", "/");

            switch (node.type)
            {
                case ProjectBrowserNodeType.File:

                    try
                    {
                        File.Move(node.path, normalizedPath);

                        if (File.Exists($"{node.path}.meta"))
                        {
                            File.Move($"{node.path}.meta", $"{normalizedPath}.meta");
                        }
                    }
                    catch (Exception)
                    {
                    }

                    break;

                case ProjectBrowserNodeType.Folder:

                    try
                    {
                        Directory.Move(node.path, normalizedPath);
                    }
                    catch (Exception)
                    {
                    }

                    break;
            }

            EditorUtils.RefreshAssets(null);
        }
    }

    /// <summary>
    /// All valid resource types
    /// </summary>
    public static readonly Dictionary<string, ProjectBrowserResourceType> resourceTypes = new()
    {
        { $".{AssetSerialization.AssetExtension}", ProjectBrowserResourceType.Asset },
        { $".{AssetSerialization.MaterialExtension}", ProjectBrowserResourceType.Material },
        { $".{AssetSerialization.ShaderExtension}", ProjectBrowserResourceType.Shader },
        { $".{AssetSerialization.ComputeShaderExtension}", ProjectBrowserResourceType.ComputeShader },
        { $".{AssetSerialization.SceneExtension}", ProjectBrowserResourceType.Scene },
        { $".{AssetSerialization.PrefabExtension}", ProjectBrowserResourceType.Prefab },
        { $".{AssetSerialization.AssemblyDefinitionExtension}", ProjectBrowserResourceType.AssemblyDefinition },
    };

    public static readonly string[] CodeExtensions = [
        "cs",
        "c",
        "cpp",
        "h",
        "hpp",
        "js",
        ];

    public static readonly Dictionary<string, string> DefaultResourceIcons = new()
    {
        { "FolderIcon", "Textures/open-folder.png" },
        { "FileIcon", "Textures/files.png" },
        { "EntityIcon", "Textures/entity.png" },
        { "SceneIcon", "Textures/scene.png" },
        { "PrefabIcon", "Textures/prefab.png" },
        { "FontIcon", "Textures/font.png" },
        { "TextIcon", "Textures/text.png" },
        { "AssetIcon", "Textures/asset.png" },
        { "AudioIcon", "Textures/audio.png" },
        { "ShaderIcon", "Textures/shader.png" },
        { "CodeIcon", "Textures/code.png" },
    };

    static ProjectBrowser()
    {
        static void AddAll(string[] extensions, ProjectBrowserResourceType type)
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
        AddAll(AssetSerialization.PluginExtensions, ProjectBrowserResourceType.Plugin);
        AddAll(AssetSerialization.PluginFolderSuffixes, ProjectBrowserResourceType.Plugin);
        AddAll(AssetSerialization.TextExtensions, ProjectBrowserResourceType.Text);
        AddAll(CodeExtensions, ProjectBrowserResourceType.Code);
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
    internal List<ProjectBrowserNode> projectBrowserNodes = [];

    /// <summary>
    /// The currently selected project browser node
    /// </summary>
    public ProjectBrowserNode currentContentNode;

    /// <summary>
    /// The currently browseable project browser nodes
    /// </summary>
    private readonly List<ImGuiUtils.ContentGridItem> currentContentBrowserNodes = [];

    /// <summary>
    /// The currently browseable project browser nodes that were filtered through a search query
    /// </summary>
    private readonly List<ImGuiUtils.ContentGridItem> searchContentBrowserNodes = [];

    /// <summary>
    /// All nodes in the project
    /// </summary>
    private readonly List<ProjectBrowserNode> allNodes = [];

    /// <summary>
    /// Local editor resources for rendering
    /// </summary>
    internal Dictionary<string, Texture> editorResources = [];

    /// <summary>
    /// Called when a node is clicked
    /// </summary>
    public Action<ProjectBrowserNode> onClick;

    /// <summary>
    /// Called when a node is double clicked
    /// </summary>
    public Action<ProjectBrowserNode> onDoubleClick;

    /// <summary>
    /// The search query for assets
    /// </summary>
    internal string assetSearchQuery = "";

    private string lastAssetSearch = "";

    private float lastAssetSearchTimer;

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
        path = Path.Combine(Environment.CurrentDirectory, "EditorResources", path);

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

    internal void LoadEditorTextures()
    {
        foreach(var pair in DefaultResourceIcons)
        {
            LoadEditorTexture(pair.Key, pair.Value);
        }
    }

    internal Texture GetResourceIcon(ProjectBrowserResourceType resourceType)
    {
        var result = resourceType switch
        {
            ProjectBrowserResourceType.Scene => GetEditorResource("SceneIcon"),
            ProjectBrowserResourceType.Prefab => GetEditorResource("PrefabIcon"),
            ProjectBrowserResourceType.Font => GetEditorResource("FontIcon"),
            ProjectBrowserResourceType.Shader or ProjectBrowserResourceType.ComputeShader => GetEditorResource("ShaderIcon"),
            ProjectBrowserResourceType.Asset => GetEditorResource("AssetIcon"),
            ProjectBrowserResourceType.Audio => GetEditorResource("AudioIcon"),
            ProjectBrowserResourceType.AssemblyDefinition => GetEditorResource("FileIcon"),
            ProjectBrowserResourceType.Plugin => GetEditorResource("FileIcon"),
            ProjectBrowserResourceType.Code => GetEditorResource("CodeIcon"),
            ProjectBrowserResourceType.Text => GetEditorResource("TextIcon"),
            _ => GetEditorResource("FileIcon"),
        };

        return result ?? GetEditorResource("FileIcon");
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

            void Recursive(string p, List<ProjectBrowserNode> nodes, ProjectBrowserNode parent)
            {
                string[] directories = [];
                string[] files = [];

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
                    var pluginExtension = AssetSerialization.PluginFolderSuffixes.FirstOrDefault(x => directory.EndsWith(x));

                    if(pluginExtension != null)
                    {
                        var pluginNode = new ProjectBrowserNode()
                        {
                            name = Path.GetFileName(directory),
                            extension = $".{pluginExtension}",
                            path = directory.Replace("\\", "/"),
                            type = ProjectBrowserNodeType.File,
                            parent = parent,
                            subnodes = [],
                            typeName = typeof(PluginAsset).FullName,
                            friendlyTypeName = typeof(PluginAsset).Name.ExpandCamelCaseName(),
                        };

                        nodes.Add(pluginNode);

                        allNodes.Add(pluginNode);

                        continue;
                    }

                    var subnodes = new List<ProjectBrowserNode>();

                    var node = new ProjectBrowserNode()
                    {
                        name = Path.GetFileName(directory),
                        extension = "",
                        path = directory.Replace("\\", "/"),
                        type = ProjectBrowserNodeType.Folder,
                        parent = parent,
                        subnodes = subnodes,
                        typeName = typeof(FolderAsset).FullName,
                        friendlyTypeName = typeof(FolderAsset).Name.ExpandCamelCaseName(),
                    };

                    Recursive(directory, subnodes, node);

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
                        parent = parent,
                        subnodes = [],
                        type = ProjectBrowserNodeType.File,
                    };

                    node.UpdateData();

                    nodes.Add(node);

                    allNodes.Add(node);
                }
            }

            var dummyParent = new ProjectBrowserNode()
            {
                name = "Assets",
                path = basePath,
                type = ProjectBrowserNodeType.Folder,
            };

            Recursive(Path.Combine(basePath, "Assets"), projectBrowserNodes, dummyParent);

            dummyParent.subnodes = projectBrowserNodes;

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

        foreach (var node in nodes)
        {
            if (node.path.EndsWith(".meta"))
            {
                continue;
            }

            var item = new ContentGridItem(this, node, false);

            currentContentBrowserNodes.Add(item);
        }
    }

    /// <summary>
    /// Creates any missing meta files
    /// </summary>
    /// <param name="onFinish">Called when the process completes</param>
    public void CreateMissingMetaFiles(Action onFinish)
    {
        StapleEditor.instance.ShowBackgroundProcess();

        JobScheduler.Schedule(new ActionJob(() =>
        {
            List<string> metaFiles = [];

            try
            {
                metaFiles.AddRange(Directory.GetFiles(basePath, "*.meta", SearchOption.AllDirectories));

                var packageDirectories = Directory.GetDirectories(Path.Combine(basePath, "Cache", "Packages"));

                foreach (var directory in packageDirectories)
                {
                    metaFiles.AddRange(Directory.GetFiles(directory, "*.meta", SearchOption.AllDirectories));
                }
            }
            catch (Exception)
            {
            }

            for(var i = 0; i < metaFiles.Count; i++)
            {
                var file = metaFiles[i];

                StapleEditor.instance.SetBackgroundProgress(i / (float)metaFiles.Count, "Processing project files...");

                try
                {
                    var local = file[..^".meta".Length];

                    var valid = File.Exists(local) || Directory.Exists(local);

                    if (!valid)
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception)
                {
                }
            }

            static string Hash()
            {
                return GuidGenerator.Generate().ToString();
            }

            var missingFiles = new List<string>();

            void HandleFile(string path)
            {
                if(path.EndsWith(".meta"))
                {
                    return;
                }

                try
                {
                    if (!File.Exists($"{path}.meta"))
                    {
                        missingFiles.Add(path);
                    }
                }
                catch (Exception)
                {
                }
            }

            void ProcessFile(string path)
            {
                try
                {
                    if (File.Exists($"{path}.meta"))
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                }

                var extension = Path.GetExtension(path);

                if (resourceTypes.TryGetValue(extension, out var type))
                {
                    switch (type)
                    {
                        case ProjectBrowserResourceType.Texture:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new TextureMetadata()
                                {
                                    guid = Hash(),
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Material:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                {
                                    guid = Hash(),
                                    typeName = typeof(Material).FullName,
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Shader:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                {
                                    guid = Hash(),
                                    typeName = typeof(Shader).FullName,
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.ComputeShader:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                {
                                    guid = Hash(),
                                    typeName = typeof(ComputeShader).FullName,
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Scene:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                {
                                    guid = Hash(),
                                    typeName = typeof(Scene).FullName,
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Mesh:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new MeshAssetMetadata()
                                {
                                    guid = Hash(),
                                    flipUVs = !(path.EndsWith(".glb") ||
                                        path.EndsWith(".gltf")),
                                    combineSimilarMeshes = AssetSerialization.StaticMeshExtensions.Any(x => path.EndsWith($".{x}")),
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Audio:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                {
                                    guid = Hash(),
                                    typeName = typeof(AudioClip).FullName,
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Font:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                {
                                    guid = Hash(),
                                    typeName = typeof(FontAsset).FullName,
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.AssemblyDefinition:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                {
                                    guid = Hash(),
                                    typeName = typeof(AssemblyDefinition).FullName,
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Plugin:

                            try
                            {
                                var plugin = new PluginAsset()
                                {
                                    guid = Hash(),
                                };

                                var isAssembly = PluginAsset.IsAssembly(path);

                                switch (extension)
                                {
                                    case ".dll":

                                        if (!isAssembly)
                                        {
                                            plugin.anyPlatform = false;

                                            plugin.platforms.Add(AppPlatform.Windows);
                                        }

                                        break;

                                    case ".dylib":

                                        plugin.anyPlatform = false;
                                        plugin.platforms.Add(AppPlatform.MacOSX);

                                        break;

                                    case ".so":

                                        plugin.anyPlatform = false;
                                        plugin.platforms.Add(path.Contains($"/Plugins/Android/") ?
                                            AppPlatform.Android : AppPlatform.Linux);

                                        break;

                                    case ".androidlib":

                                        plugin.anyPlatform = false;
                                        plugin.platforms.Add(AppPlatform.Android);

                                        break;

                                    case ".bundle":
                                    case ".framework":

                                        plugin.anyPlatform = false;
                                        plugin.platforms.Add(path.Contains($"/Plugins/MacOS/") ?
                                            AppPlatform.MacOSX : AppPlatform.iOS);

                                        break;
                                }

                                var jsonData = JsonConvert.SerializeObject(plugin, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Asset:

                            try
                            {
                                var text = File.ReadAllText(path);
                                var holder = JsonConvert.DeserializeObject<AssetHolder>(text);

                                if (holder != null)
                                {
                                    var json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                    File.WriteAllText($"{path}.meta", json);
                                }
                                else
                                {
                                    var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                    {
                                        guid = Hash(),
                                        typeName = "Unknown",
                                    },
                                    Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                    File.WriteAllText($"{path}.meta", jsonData);
                                }
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Prefab:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new AssetHolder()
                                {
                                    guid = Hash(),
                                    typeName = typeof(Prefab).FullName,
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Text:

                            try
                            {
                                var jsonData = JsonConvert.SerializeObject(new TextAssetMetadata()
                                {
                                    guid = Hash(),
                                    typeName = typeof(TextAsset).FullName,
                                },
                                Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", jsonData);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectBrowserResourceType.Code:

                            try
                            {
                                var holder = new AssetHolder()
                                {
                                    guid = Hash(),
                                    typeName = "Unknown",
                                };

                                var json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                File.WriteAllText($"{path}.meta", json);
                            }
                            catch (Exception)
                            {
                            }

                            break;
                    }
                }
                else
                {
                    if (!path.EndsWith(".meta"))
                    {
                        try
                        {
                            var holder = new AssetHolder()
                            {
                                guid = Hash(),
                                typeName = "Unknown",
                            };

                            var json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                            File.WriteAllText($"{path}.meta", json);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            static void HandleDirectory(string path)
            {
                try
                {
                    if (!File.Exists($"{path}.meta"))
                    {
                        var holder = new FolderAsset()
                        {
                            guid = Hash(),
                            typeName = typeof(FolderAsset).FullName,
                        };

                        var json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                        File.WriteAllText($"{path}.meta", json);
                    }
                }
                catch (Exception)
                {
                }
            }

            void RecursiveFile(string path)
            {
                try
                {
                    var directories = Directory.GetDirectories(path);
                    var files = Directory.GetFiles(path);

                    foreach (var directory in directories)
                    {
                        HandleDirectory(directory);

                        RecursiveFile(directory);
                    }

                    foreach (var file in files)
                    {
                        HandleFile(file);
                    }
                }
                catch (Exception)
                {
                }
            }

            void RecursiveProject(List<ProjectBrowserNode> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node.type == ProjectBrowserNodeType.Folder)
                    {
                        HandleDirectory(node.path);

                        RecursiveProject(node.subnodes);
                    }
                    else
                    {
                        HandleFile(node.path);
                    }
                }
            }

            try
            {
                var packageDirectories = Directory.GetDirectories(Path.Combine(basePath, "Cache", "Packages"));

                foreach (var directory in packageDirectories)
                {
                    RecursiveFile(directory);
                }
            }
            catch (Exception)
            {
            }

            RecursiveProject(projectBrowserNodes);

            if (missingFiles.Count == 0)
            {
                StapleEditor.instance.HideBackgroundProcess();

                ThreadHelper.Dispatch(onFinish);

                return;
            }

            var chunkCount = JobScheduler.ChunkSize(missingFiles.Count);

            var counter = missingFiles.Count;

            var l = new Lock();

            for (var i = 0; i < missingFiles.Count; i += chunkCount)
            {
                var start = i;
                var end = i + chunkCount > missingFiles.Count ? missingFiles.Count - i : chunkCount;

                JobScheduler.Schedule(new ActionJob(() =>
                {
                    var slice = CollectionsMarshal.AsSpan(missingFiles)
                        .Slice(start, end);

                    for (var j = 0; j < slice.Length; j++)
                    {
                        ProcessFile(slice[j]);

                        StapleEditor.instance.SetBackgroundProgress(1 - (counter / (float)missingFiles.Count),
                            $"Processing {missingFiles.Count - counter}/{missingFiles.Count} files...");

                        lock (l)
                        {
                            counter--;

                            if (counter <= 0)
                            {
                                StapleEditor.instance.HideBackgroundProcess();

                                ThreadHelper.Dispatch(onFinish);
                            }
                        }
                    }
                }));
            }
        }));
    }

    /// <summary>
    /// Clears the current item selection
    /// </summary>
    public void ClearSelection()
    {
        foreach(var item in currentContentBrowserNodes)
        {
            item.selected = false;
        }
    }

    /// <summary>
    /// Draws the project browser in the editor GUI
    /// </summary>
    /// <param name="io">The ImGUI IO Pointer</param>
    /// <returns>Whether the window is hovered</returns>
    public bool Draw(ImGuiIOPtr io)
    {
        ImGui.Columns(2, "ProjectBrowserContent");

        var nextNode = currentContentNode;

        editorResources.TryGetValue("FolderIcon", out var folderTexture);

        void Recursive(ProjectBrowserNode node)
        {
            if (node.type != ProjectBrowserNodeType.Folder)
            {
                return;
            }

            var hasChildren = node.subnodes.Any(x => x.type == ProjectBrowserNodeType.Folder);

            EditorGUI.TreeNodeIcon(folderTexture, node.name, node.name, !hasChildren, ref node.open, () =>
            {
                if (hasChildren)
                {
                    foreach (var subnode in node.subnodes)
                    {
                        Recursive(subnode);
                    }
                }
            },
            () =>
            {
                nextNode = node;
            });
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

        ImGui.BeginChild("ProjectBrowserContentAssets", ImGuiChildFlags.None);

        EditorGUI.Disabled(currentContentNode == null || currentContentNode.path == basePath, () =>
        {
            EditorGUI.Button("Up", "ProjectBrowser.Up", () =>
            {
                currentContentNode = currentContentNode.parent;

                UpdateCurrentContentNodes(currentContentNode.subnodes);
            });
        });

        EditorGUI.SameLine();

        var previousSearch = assetSearchQuery;

        assetSearchQuery = EditorGUI.TextField("Search:", "ProjectBrowser.Search", assetSearchQuery);

        if (assetSearchQuery != previousSearch)
        {
            lastAssetSearchTimer = 0;
        }
        else if(lastAssetSearch != assetSearchQuery)
        {
            lastAssetSearchTimer += Time.deltaTime;

            if(lastAssetSearchTimer >= 0.6f)
            {
                lastAssetSearch = assetSearchQuery;

                searchContentBrowserNodes.Clear();

                var assets = AssetDatabase.SearchAssets(null, assetSearchQuery);

                foreach (var asset in assets)
                {
                    var assetName = AssetDatabase.GetAssetName(asset);

                    var extension = "";

                    var path = AssetDatabase.GetAssetPath(asset);

                    if (path != null)
                    {
                        extension = Path.GetExtension(path);

                        try
                        {
                            path = EditorUtils.GetLocalPath(path);

                            if (File.Exists(Path.Combine(basePath, path)))
                            {
                                path = Path.Combine(basePath, path);
                            }
                            else
                            {
                                path = asset;
                            }
                        }
                        catch(Exception)
                        {
                        }
                    }

                    var node = new ProjectBrowserNode()
                    {
                        name = assetName,
                        extension = extension,
                        path = path,
                        type = ProjectBrowserNodeType.File,
                    };

                    node.UpdateData();

                    var item = new ContentGridItem(this, node, true);

                    searchContentBrowserNodes.Add(item);
                }
            }
        }

        var isSearch = !string.IsNullOrEmpty(assetSearchQuery);

        ImGuiUtils.ContentGrid(isSearch ? searchContentBrowserNodes : currentContentBrowserNodes,
            contentPanelPadding, contentPanelThumbnailSize, "ASSET", !isSearch, !isSearch);

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
