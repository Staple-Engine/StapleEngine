using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Staple.Internal;

namespace Staple.Editor
{
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

            ImGui.InputText("Search", ref assetPickerSearch, uint.MaxValue);

            ImGui.BeginChildFrame(ImGui.GetID("AssetList"), Vector2.Zero);

            var validItems = new List<ProjectBrowserNode>
            {
                null
            };

            void Handle(ProjectBrowserNode child)
            {
                switch (child.type)
                {
                    case ProjectBrowserNodeType.Folder:

                        Recursive(child);

                        break;

                    case ProjectBrowserNodeType.File:

                        if ((assetPickerSearch?.Length ?? 0) > 0 &&
                            child.name.Contains(assetPickerSearch, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            return;
                        }

                        if (assetPickerType.FullName == child.typeName)
                        {
                            validItems.Add(child);
                        }

                        break;
                }
            }

            void Recursive(ProjectBrowserNode source)
            {
                foreach (var child in source.subnodes)
                {
                    Handle(child);
                }
            }

            foreach (var node in projectBrowser.projectBrowserNodes)
            {
                switch (node.type)
                {
                    case ProjectBrowserNodeType.Folder:

                        Recursive(node);

                        break;

                    case ProjectBrowserNodeType.File:

                        Handle(node);

                        break;
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

                    var cachePath = i.path;

                    var cacheIndex = i.path.IndexOf("Assets");

                    if (cacheIndex >= 0)
                    {
                        cachePath = Path.Combine(basePath, "Cache", "Staging", currentPlatform.ToString(), i.path.Substring(cacheIndex + "Assets\\".Length));
                    }

                    var type = ProjectBrowser.ResourceTypeForExtension(i.extension);

                    switch (type)
                    {
                        case ProjectBrowserResourceType.Texture:

                            try
                            {
                                texture = ResourceManager.instance.LoadTexture(cachePath);
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

                        case ProjectBrowserResourceType.Asset:

                            try
                            {
                                var asset = ResourceManager.instance.LoadAsset(i.path);

                                if (asset != null && asset.GetType() == assetPickerType)
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
                                var material = ResourceManager.instance.LoadMaterial(cachePath);

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
                                var shader = ResourceManager.instance.LoadShader(cachePath);

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
                                var audioClip = ResourceManager.instance.LoadAudioClip(cachePath.Replace(".meta", ""));

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
                    }

                    Close();
                });

            ImGui.EndChildFrame();
        }
    }
}
