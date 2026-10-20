using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Staple.Internal;
using Staple.ProjectManagement;

namespace Staple.Editor;

internal class AssetPickerWindow : EditorWindow
{
    internal class ContentGridItem : ImGuiUtils.ContentGridItem
    {
        public readonly string basePath;
        public readonly ProjectBrowser projectBrowser;
        public readonly ProjectBrowserNode node;
        public readonly Texture fileIcon;

        public AssetPickerWindow owner;

        public ContentGridItem(AssetPickerWindow owner, string basePath, ProjectBrowser projectBrowser, ProjectBrowserNode node,
            Texture fileIcon)
        {
            this.owner = owner;
            this.basePath = basePath;
            this.projectBrowser = projectBrowser;
            this.node = node;
            this.fileIcon = fileIcon;

            name = node?.name ?? "(None)";
            ID = $"{node?.name}.{node?.extension}";
        }

        string ThumbnailPath(string path)
        {
            if (File.Exists(path))
            {
                return path;
            }

            if (File.Exists(Path.Combine(basePath, path)))
            {
                return Path.Combine(basePath, path);
            }

            return path;
        }

        public override Texture EnsureValidTexture(Texture existing)
        {
            if (StapleEditor.instance.RefreshingAssets)
            {
                return texture;
            }

            if (node != null && ((texture?.Disposed ?? true) || ThumbnailCache.HasCachedThumbnail(ThumbnailPath(node.path))))
            {
                return ThumbnailCache.GetThumbnail(ThumbnailPath(node.path)) ??
                    projectBrowser.GetResourceIcon(ProjectBrowser.ResourceTypeForExtension(node.extension));
            }

            return texture ?? fileIcon;
        }

        public override void NotVisible()
        {
            if (string.IsNullOrEmpty(node?.path))
            {
                return;
            }

            ThumbnailCache.RemoveRenderRequest(node.path);
        }

        public override void OnClick()
        {
        }

        public override void OnDelete()
        {
        }

        public override void OnDoubleClick()
        {
            owner.OnProjectNodeDoubleClicked(node);
        }

        public override void OnDragDropped()
        {
        }

        public override void OnRename(string name)
        {
        }
    }

    public string assetPickerSearch = "";
    public string assetPickerKey;
    public Type assetPickerType;
    public ProjectBrowser projectBrowser;
    public AppPlatform currentPlatform;
    public string basePath;
    public string[] ignoredGuids = [];

    public Func<string, bool> filter;

    private readonly List<ImGuiUtils.ContentGridItem> gridItems = [];

    public AssetPickerWindow()
    {
        windowFlags = EditorWindowFlags.VerticalScrollbar;

        windowType = EditorWindowType.Popup;

        size = new(800, 600);
    }

    private void OnProjectNodeDoubleClicked(ProjectBrowserNode node)
    {
        if (node == null)
        {
            if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
            {
                EditorGUI.pendingObjectPickers[assetPickerKey] = null;
            }

            Close();

            return;
        }

        if (assetPickerType == typeof(Mesh) && Mesh.defaultMeshes.TryGetValue(node.path, out var defaultMesh))
        {
            if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
            {
                EditorGUI.pendingObjectPickers[assetPickerKey] = defaultMesh;
            }

            Close();

            return;
        }

        var cachePath = EditorUtils.GetLocalPath(node.path);

        var guid = AssetDatabase.GetAssetGuid(cachePath);

        if (guid == null)
        {
            Close();

            return;
        }

        var type = ProjectBrowser.ResourceTypeForExtension(node.extension);

        switch (type)
        {
            case ProjectBrowserResourceType.Texture:

                Texture texture = null;

                try
                {
                    texture = ResourceManager.instance.LoadTexture(guid);
                }
                catch (System.Exception)
                {
                }

                if (texture != null)
                {
                    if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                    {
                        EditorGUI.pendingObjectPickers[assetPickerKey] = texture;
                    }
                }

                break;

            case ProjectBrowserResourceType.Mesh:

                try
                {
                    var mesh = ResourceManager.instance.LoadMesh(guid);

                    if (mesh != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = mesh;
                        }
                    }
                }
                catch (System.Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.Asset:

                try
                {
                    var asset = ResourceManager.instance.LoadAsset(guid);

                    if (asset != null && asset.GetType().FullName == assetPickerType.FullName)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = asset;
                        }
                    }
                }
                catch (System.Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.Material:

                try
                {
                    var material = ResourceManager.instance.LoadMaterial(guid);

                    if (material != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = material;
                        }
                    }
                }
                catch (System.Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.Shader:

                try
                {
                    var shader = ResourceManager.instance.LoadShader(guid);

                    if (shader != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = shader;
                        }
                    }
                }
                catch (System.Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.ComputeShader:

                try
                {
                    var shader = ResourceManager.instance.LoadComputeShader(guid);

                    if (shader != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = shader;
                        }
                    }
                }
                catch (System.Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.Audio:

                try
                {
                    var audioClip = ResourceManager.instance.LoadAudioClip(guid);

                    if (audioClip != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = audioClip;
                        }
                    }
                }
                catch (System.Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.Font:

                try
                {
                    var font = ResourceManager.instance.LoadFont(guid);

                    if (font != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = font;
                        }
                    }
                }
                catch (System.Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.Prefab:

                try
                {
                    var prefab = ResourceManager.instance.LoadPrefab(guid);

                    if (prefab != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = prefab;
                        }
                    }
                }
                catch (System.Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.AssemblyDefinition:

                try
                {
                    var asmDef = (AssemblyDefinition)AssemblyDefinition.Create(guid);

                    if (asmDef != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = asmDef;
                        }
                    }
                }
                catch (Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.Plugin:

                try
                {
                    var plugin = PluginAsset.Create(guid);

                    if (plugin != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = plugin;
                        }
                    }
                }
                catch (Exception)
                {
                }

                break;

            case ProjectBrowserResourceType.Text:

                try
                {
                    var textAsset = ResourceManager.instance.LoadTextAsset(guid);

                    if (textAsset != null)
                    {
                        if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                        {
                            EditorGUI.pendingObjectPickers[assetPickerKey] = textAsset;
                        }
                    }
                }
                catch (Exception)
                {
                }

                break;
        }

        Close();
    }

    private void Refresh()
    {
        if(assetPickerType == null)
        {
            return;
        }

        projectBrowser.editorResources.TryGetValue("FileIcon", out var fileIcon);

        gridItems.Clear();

        var validItems = new List<ContentGridItem>
        {
            new(this, basePath, projectBrowser, null, fileIcon),
        };

        foreach (var pair in AssetDatabase.database.assets)
        {
            if ((assetPickerSearch?.Length ?? 0) > 0 &&
                (pair.Value.Count == 0 ||
                !pair.Value[0].name.Contains(assetPickerSearch, StringComparison.InvariantCultureIgnoreCase)))
            {
                continue;
            }

            if(pair.Value.Count == 0)
            {
                continue;
            }

            var asset = pair.Value[0];

            if(Array.IndexOf(ignoredGuids, asset.guid) >= 0 ||
                assetPickerType.FullName != asset.typeName ||
                !(filter?.Invoke(asset.guid) ?? true))
            {
                continue;
            }

            gridItems.Add(new ContentGridItem(this, basePath, projectBrowser, new ProjectBrowserNode()
            {
                path = asset.path,
                name = asset.name,
                typeName = asset.typeName,
                extension = Path.GetExtension(asset.path.Replace(".meta", "")).ToLowerInvariant(),
            }, fileIcon));
        }
    }

    public override void OnGUI()
    {
        string newValue = assetPickerSearch;

        assetPickerSearch = EditorGUI.TextField("Search", "AssetPickerSearch", newValue);

        if (newValue != assetPickerSearch || gridItems.Count == 0)
        {
            Refresh();
        }

        ImGuiUtils.ContentGrid(gridItems, ProjectBrowser.contentPanelPadding, ProjectBrowser.contentPanelThumbnailSize,
            null, false, false);
    }
}
