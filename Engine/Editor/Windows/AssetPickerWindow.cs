using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Staple.Internal;
using Staple.ProjectManagement;

namespace Staple.Editor;

internal class AssetPickerWindow : EditorWindow
{
    public string assetPickerSearch = "";
    public string assetPickerKey;
    public Type assetPickerType;
    public ProjectBrowser projectBrowser;
    public AppPlatform currentPlatform;
    public string basePath;
    public string[] ignoredGuids = [];

    public Func<string, bool> filter;

    private ProjectBrowserNode[] validItems = [];
    private List<ImGuiUtils.ContentGridItem> gridItems = [];

    public AssetPickerWindow()
    {
        windowFlags = EditorWindowFlags.VerticalScrollbar;

        windowType = EditorWindowType.Popup;

        size = new(800, 600);
    }

    private void Refresh()
    {
        var validItems = new List<ProjectBrowserNode>
        {
            null
        };

        foreach (var pair in AssetDatabase.assets)
        {
            if ((assetPickerSearch?.Length ?? 0) > 0 &&
                (pair.Value.Count == 0 ||
                pair.Value[0].name.Contains(assetPickerSearch, StringComparison.InvariantCultureIgnoreCase) == false))
            {
                continue;
            }

            var asset = pair.Value[0];

            if(Array.IndexOf(ignoredGuids, asset.guid) >= 0 ||
                assetPickerType.FullName != asset.typeName ||
                (filter?.Invoke(asset.guid) ?? true) == false)
            {
                continue;
            }

            validItems.Add(new ProjectBrowserNode()
            {
                path = asset.path,
                name = asset.name,
                typeName = asset.typeName,
                extension = Path.GetExtension(asset.path.Replace(".meta", "")).ToLowerInvariant(),
            });
        }

        this.validItems = validItems.ToArray();

        projectBrowser.editorResources.TryGetValue("FileIcon", out var fileIcon);

        string ThumbnailPath(string path)
        {
            if(File.Exists(path))
            {
                return path;
            }

            if(File.Exists(Path.Combine(basePath, path)))
            {
                return Path.Combine(basePath, path);
            }

            return path;
        }

        gridItems = validItems
            .Select(x => new ImGuiUtils.ContentGridItem()
            {
                name = x?.name ?? "(None)",
                ensureValidTexture = (texture) =>
                {
                    if (StapleEditor.instance.RefreshingAssets)
                    {
                        return texture;
                    }

                    if (x != null && ((texture?.Disposed ?? true) || ThumbnailCache.HasCachedThumbnail(ThumbnailPath(x.path))))
                    {
                        return ThumbnailCache.GetThumbnail(ThumbnailPath(x.path)) ?? projectBrowser.GetResourceIcon(ProjectBrowser.ResourceTypeForExtension(x.extension));
                    }

                    return texture ?? fileIcon;
                },
            }).ToList();
    }

    public override void OnGUI()
    {
        string newValue = assetPickerSearch;

        assetPickerSearch = EditorGUI.TextField("Search", "AssetPickerSearch", newValue);

        if (newValue != assetPickerSearch || validItems.Length == 0)
        {
            Refresh();
        }

        ImGuiUtils.ContentGrid(gridItems, ProjectBrowser.contentPanelPadding, ProjectBrowser.contentPanelThumbnailSize,
            null, false,
            (index, item) =>
            {
            },
            (index, item) =>
            {
                var i = validItems[index];

                if (i == null)
                {
                    if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                    {
                        EditorGUI.pendingObjectPickers[assetPickerKey] = null;
                    }

                    Close();

                    return;
                }

                if(assetPickerType == typeof(Mesh) && Mesh.defaultMeshes.TryGetValue(i.path, out var defaultMesh))
                {
                    if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                    {
                        EditorGUI.pendingObjectPickers[assetPickerKey] = defaultMesh;
                    }

                    Close();

                    return;
                }

                var cachePath = EditorUtils.GetLocalPath(i.path);

                var guid = AssetDatabase.GetAssetGuid(cachePath);

                if(guid == null)
                {
                    Close();

                    return;
                }

                var type = ProjectBrowser.ResourceTypeForExtension(i.extension);

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

                            if(asmDef != null)
                            {
                                if (EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                                {
                                    EditorGUI.pendingObjectPickers[assetPickerKey] = asmDef;
                                }
                            }
                        }
                        catch(Exception)
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
            },
            null, null, null);
    }
}
