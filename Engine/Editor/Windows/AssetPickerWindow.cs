using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Staple.Internal;

namespace Staple.Editor;

internal class AssetPickerWindow : EditorWindow
{
    public string assetPickerSearch = "";
    public string assetPickerKey;
    public Type assetPickerType;
    public ProjectBrowser projectBrowser;
    public AppPlatform currentPlatform;
    public string basePath;

    public AssetPickerWindow()
    {
        allowDocking = false;
        windowType = EditorWindowType.Popup;
    }

    public override void OnGUI()
    {
        base.OnGUI();

        string newValue = assetPickerSearch;

        assetPickerSearch = EditorGUI.TextField("Search", "AssetPickerSearch", newValue);

        var validItems = new List<ProjectBrowserNode>
        {
            null
        };

        foreach(var asset in AssetDatabase.assets)
        {
            if ((assetPickerSearch?.Length ?? 0) > 0 &&
                asset.name.Contains(assetPickerSearch, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                continue;
            }

            if (assetPickerType.FullName == asset.typeName)
            {
                validItems.Add(new ProjectBrowserNode()
                {
                    path = asset.path,
                    name = asset.name,
                    typeName = asset.typeName,
                    extension = Path.GetExtension(asset.path.Replace(".meta", "")).ToLowerInvariant(),
                });
            }
        }

        projectBrowser.editorResources.TryGetValue("FileIcon", out var texture);

        var gridItems = validItems
            .Select(x => new ImGuiUtils.ContentGridItem()
            {
                name = x?.name ?? "(None)",
                texture = texture,
                ensureValidTexture = (_) => texture,
            }).ToList();

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
                            var font = ResourceManager.instance.LoadPrefab(guid);

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
                }

                Close();
            },
            null, null, null);
    }
}
