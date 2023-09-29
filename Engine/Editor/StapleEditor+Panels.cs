using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NfdSharp;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        private void Entities(ImGuiIOPtr io)
        {
            ImGui.Begin("Entities");

            ImGui.BeginChildFrame(ImGui.GetID("EntityFrame"), new Vector2(0, 0));

            if (Scene.current != null)
            {
                void Recursive(Transform transform)
                {
                    var flags = ImGuiTreeNodeFlags.SpanFullWidth;

                    if (transform.ChildCount == 0)
                    {
                        flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                    }

                    var entityName = Scene.current.world.GetEntityName(transform.entity);

                    if (ImGui.TreeNodeEx($"{entityName}##0", flags))
                    {
                        if (ImGui.IsItemClicked() && ImGui.IsItemToggledOpen() == false)
                        {
                            selectedEntity = transform.entity;
                            selectedProjectNode = null;
                            selectedProjectNodeData = null;

                            cachedEditors.Clear();
                            EditorGUI.pendingObjectPickers.Clear();

                            showingAssetPicker = false;

                            var counter = 0;

                            Scene.current.world.IterateComponents(selectedEntity, (ref IComponent component) =>
                            {
                                counter++;

                                if (component is Transform transform)
                                {
                                    return;
                                }

                                var editor = Editor.CreateEditor(component);

                                if(editor != null)
                                {
                                    cachedEditors.Add($"{counter}{component.GetType().FullName}", editor);
                                }
                            });
                        }

                        foreach (var child in transform)
                        {
                            var childEntity = Scene.current.FindEntity(child.entity.ID);

                            if (childEntity != Entity.Empty)
                            {
                                var t = Scene.current.GetComponent<Transform>(childEntity);

                                Recursive(t);
                            }
                        }

                        if (transform.ChildCount > 0)
                        {
                            ImGui.TreePop();
                        }
                    }
                }

                Scene.current.world.Iterate((entity) =>
                {
                    var transform = Scene.current.GetComponent<Transform>(entity);

                    if (transform.parent == null)
                    {
                        Recursive(transform);
                    }
                });
            }

            ImGui.EndChildFrame();

            ImGui.End();
        }

        private void Viewport(ImGuiIOPtr io)
        {
            ImGui.Begin("Viewport", ImGuiWindowFlags.NoBackground);

            if (ImGui.BeginTabBar("Viewport Tab"))
            {
                if (ImGui.BeginTabItem("Scene"))
                {
                    viewportType = ViewportType.Scene;

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Game"))
                {
                    viewportType = ViewportType.Game;

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            if (viewportType == ViewportType.Game)
            {
                ImGui.BeginChildFrame(ImGui.GetID("GameView"), new Vector2(0, 0), ImGuiWindowFlags.NoBackground);

                var width = (ushort)ImGui.GetContentRegionAvail().X;
                var height = (ushort)ImGui.GetContentRegionAvail().Y;

                if (gameRenderTarget == null || gameRenderTarget.width != width || gameRenderTarget.height != height)
                {
                    gameRenderTarget?.Destroy();

                    gameRenderTarget = RenderTarget.Create(width, height);
                }

                var texture = gameRenderTarget.GetTexture();

                if (texture != null)
                {
                    ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), new Vector2(width, height));
                }

                ImGui.End();
            }

            ImGui.End();
        }

        private void Inspector(ImGuiIOPtr io)
        {
            ImGui.Begin("Inspector");

            ImGui.BeginChildFrame(ImGui.GetID("Toolbar"), new Vector2(0, 0));

            if (selectedEntity != null && Scene.current != null && Scene.current.world.IsValidEntity(selectedEntity))
            {
                var name = Scene.current.world.GetEntityName(selectedEntity);

                if(ImGui.InputText("Name", ref name, 120))
                {
                    Scene.current.world.SetEntityName(selectedEntity, name);
                }

                var currentLayer = Scene.current.world.GetEntityLayer(selectedEntity);
                var layers = LayerMask.AllLayers;

                if (ImGui.BeginCombo("Layer", currentLayer < layers.Count ? layers[(int)currentLayer] : ""))
                {
                    for (var j = 0; j < layers.Count; j++)
                    {
                        bool selected = j == currentLayer;

                        if (ImGui.Selectable(layers[j], selected))
                        {
                            Scene.current.world.SetEntityLayer(selectedEntity, (uint)j);
                        }
                    }

                    ImGui.EndCombo();
                }

                var counter = 0;

                Scene.current.world.IterateComponents(selectedEntity, (ref IComponent component) =>
                {
                    if (ImGui.TreeNodeEx(component.GetType().Name + $"##{counter++}", ImGuiTreeNodeFlags.SpanFullWidth))
                    {
                        if (component is Transform transform)
                        {
                            transform.LocalPosition = EditorGUI.Vector3Field("Position", transform.LocalPosition);

                            var rotation = Math.ToEulerAngles(transform.LocalRotation);

                            var newRotation = EditorGUI.Vector3Field("Rotation", rotation);

                            if (rotation != newRotation)
                            {
                                transform.LocalRotation = Math.FromEulerAngles(newRotation);
                            }

                            transform.LocalScale = EditorGUI.Vector3Field("Scale", transform.LocalScale);
                        }
                        else if (cachedEditors.TryGetValue($"{counter}{component.GetType().FullName}", out var editor))
                        {
                            EditorGUI.Changed = false;

                            editor.OnInspectorGUI();
                        }
                        else
                        {
                            defaultEditor.target = component;

                            EditorGUI.Changed = false;

                            defaultEditor.OnInspectorGUI();
                        }

                        Scene.current.UpdateComponent(selectedEntity, component);

                        ImGui.TreePop();
                    }
                });
            }
            else if(selectedProjectNode != null && selectedProjectNodeData != null)
            {
                if(cachedEditors.Count > 0)
                {
                    var editor = cachedEditors.First().Value;

                    editor.target = selectedProjectNodeData;

                    editor.OnInspectorGUI();
                }
            }

            ImGui.EndChildFrame();

            ImGui.End();
        }

        private void BottomPanel(ImGuiIOPtr io)
        {
            ImGui.Begin("BottomPanel");

            ImGui.BeginChildFrame(ImGui.GetID("Toolbar"), new Vector2(0, 32));

            if (ImGui.BeginTabBar("BottomTabBar"))
            {
                if (ImGui.TabItemButton("Project"))
                {
                    activeBottomTab = 0;
                }

                if (ImGui.TabItemButton("Log"))
                {
                    activeBottomTab = 1;
                }

                ImGui.EndTabBar();
            }

            ImGui.EndChildFrame();

            switch (activeBottomTab)
            {
                case 0:

                    ProjectBrowser(io);

                    break;

                case 1:

                    Console(io);

                    break;
            }

            ImGui.End();
        }

        private void ProjectBrowser(ImGuiIOPtr io)
        {
            projectBrowser.Draw(io, (item) =>
            {
                selectedEntity = Entity.Empty;
                selectedProjectNode = item.type == ProjectBrowserNodeType.File ? item : null;
                selectedProjectNodeData = null;

                cachedEditors.Clear();
                EditorGUI.pendingObjectPickers.Clear();

                showingAssetPicker = false;

                if (selectedProjectNode == null)
                {
                    return;
                }

                var data = string.Empty;

                try
                {
                    data = File.ReadAllText($"{item.path}.meta");
                }
                catch (Exception)
                {
                }

                if (data.Length > 0)
                {
                    object original = null;

                    switch (item.resourceType)
                    {
                        case ProjectResourceType.Texture:

                            try
                            {
                                original = JsonConvert.DeserializeObject<TextureMetadata>(data);
                                selectedProjectNodeData = JsonConvert.DeserializeObject<TextureMetadata>(data);
                            }
                            catch (Exception)
                            {
                            }

                            if (original != null && selectedProjectNodeData != null)
                            {
                                var cachePath = item.path;

                                var pathIndex = item.path.IndexOf("Assets");

                                if (pathIndex >= 0)
                                {
                                    cachePath = Path.Combine(basePath, "Cache", "Staging", currentPlatform.ToString(), item.path.Substring(pathIndex + "Assets\\".Length));
                                }

                                var editor = new TextureAssetEditor()
                                {
                                    original = original as TextureMetadata,
                                    path = $"{item.path}.meta",
                                    cachePath = cachePath,
                                    target = selectedProjectNodeData,
                                };

                                cachedEditors.Add("", editor);

                                editor.UpdatePreview();
                            }

                            break;

                        case ProjectResourceType.Material:

                            try
                            {
                                selectedProjectNodeData = JsonConvert.DeserializeObject<MaterialMetadata>(data);
                            }
                            catch (Exception)
                            {
                            }

                            break;

                        case ProjectResourceType.Shader:

                            try
                            {
                                selectedProjectNodeData = JsonConvert.DeserializeObject<ShaderMetadata>(data);
                            }
                            catch (Exception)
                            {
                            }

                            break;
                    }
                }
            },
            (item) =>
            {
                if(item.type != ProjectBrowserNodeType.File)
                {
                    return;
                }

                switch (item.action)
                {
                    case ProjectBrowserNodeAction.InspectScene:

                        var scene = ResourceManager.instance.LoadRawSceneFromPath(item.path);

                        if (scene != null)
                        {
                            lastOpenScene = item.path;
                            Scene.current = scene;

                            ResetScenePhysics();

                            UpdateLastSession(new LastSessionInfo()
                            {
                                currentPlatform = currentPlatform,
                                lastOpenScene = lastOpenScene,
                            });
                        }

                        break;
                }
            });
        }

        private void Console(ImGuiIOPtr io)
        {
        }

        private void Dockspace()
        {
            var windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoBackground;

            ImGui.Begin("Dockspace", windowFlags);

            var dockID = ImGui.GetID("Dockspace");

            ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);

            ImGui.DockSpace(dockID, new Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode);

            ImGui.PopStyleColor();

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Save"))
                    {
                        if (Scene.current != null && lastOpenScene != null)
                        {
                            var serializableScene = Scene.current.Serialize();

                            var text = JsonConvert.SerializeObject(serializableScene.objects, Formatting.Indented, new JsonSerializerSettings()
                            {
                                Converters =
                                {
                                    new StringEnumConverter(),
                                }
                            });

                            try
                            {
                                File.WriteAllText(lastOpenScene, text);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Build"))
                    {
                        showingBuildWindow = true;
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Exit"))
                    {
                        window.shouldStop = true;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }

            ImGui.End();
        }

        private void AssetPicker(ImGuiIOPtr io)
        {
            if(showingAssetPicker)
            {
                ImGui.Begin("AssetPicker", ImGuiWindowFlags.NoDocking);

                ImGui.InputText("Search", ref assetPickerSearch, uint.MaxValue);

                ImGui.BeginChildFrame(ImGui.GetID("AssetList"), Vector2.Zero);

                var validItems = new List<ProjectBrowserNode>();

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

                            switch (assetPickerType)
                            {
                                case Type t when t == typeof(Texture) || t.IsSubclassOf(typeof(Texture)):

                                    if (child.resourceType == ProjectResourceType.Texture)
                                    {
                                        validItems.Add(child);
                                    }

                                    break;

                                case Type t when t == typeof(Material):

                                    if (child.resourceType == ProjectResourceType.Material)
                                    {
                                        validItems.Add(child);
                                    }

                                    break;
                            }

                            break;
                    }
                }

                void Recursive(ProjectBrowserNode source)
                {
                    foreach(var child in source.subnodes)
                    {
                        Handle(child);
                    }
                }

                foreach(var node in projectBrowser.projectBrowserNodes)
                {
                    switch(node.type)
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
                        name = x.name,
                        texture = texture,
                        ensureValidTexture = (_) => texture,
                    }).ToList();

                ImGuiUtils.ContentGrid(gridItems, Staple.Editor.ProjectBrowser.contentPanelPadding, Staple.Editor.ProjectBrowser.contentPanelThumbnailSize,
                    (index, item) =>
                    {
                    },
                    (index, item) =>
                    {
                        var i = validItems[index];

                        switch(i.resourceType)
                        {
                            case ProjectResourceType.Material:

                                break;

                            case ProjectResourceType.Texture:

                                {
                                    var cachePath = i.path;

                                    var cacheIndex = i.path.IndexOf("Assets");

                                    if (cacheIndex >= 0)
                                    {
                                        cachePath = Path.Combine(basePath, "Cache", "Staging", currentPlatform.ToString(), i.path.Substring(cacheIndex + "Assets\\".Length));
                                    }

                                    try
                                    {
                                        texture = ResourceManager.instance.LoadTexture(cachePath);
                                    }
                                    catch (System.Exception)
                                    {
                                    }

                                    if(texture != null)
                                    {
                                        if(EditorGUI.pendingObjectPickers.ContainsKey(assetPickerKey))
                                        {
                                            EditorGUI.pendingObjectPickers[assetPickerKey] = texture;
                                        }
                                    }
                                }

                                break;

                            case ProjectResourceType.Shader:

                                break;
                        }

                        showingAssetPicker = false;
                    });

                ImGui.EndChildFrame();

                ImGui.End();
            }
        }

        private void BuildWindow(ImGuiIOPtr io)
        {
            if(showingBuildWindow)
            {
                ImGui.Begin("Build", ImGuiWindowFlags.NoDocking);

                var values = Enum.GetValues<AppPlatform>()
                    .Where(x => projectAppSettings.renderers.Keys.Any(y => y == x))
                    .ToList();

                var current = values.IndexOf(buildPlatform);

                var valueStrings = values
                    .Select(x => x.ToString())
                    .ToArray();

                buildPlatform = values[EditorGUI.Dropdown("Platform", valueStrings, current)];

                buildPlayerDebug = EditorGUI.Toggle("Debug Build", buildPlayerDebug);

                if(EditorGUI.Button("Build"))
                {
                    var result = Nfd.PickFolder(Path.GetFullPath(basePath), out var path);

                    if (result == Nfd.NfdResult.NFD_OKAY)
                    {
                        showingProgress = true;
                        progressFraction = 0;

                        ImGui.OpenPopup("ShowingProgress");

                        StartBackgroundTask((ref float progressFraction) =>
                        {
                            BuildPlayer(buildPlatform, path, buildPlayerDebug);

                            return true;
                        });
                    }
                    else
                    {
                        Log.Error($"Failed to open file dialog: {Nfd.GetError()}");
                    }
                }

                if(EditorGUI.Button("Close"))
                {
                    showingBuildWindow = false;
                }

                ImGui.End();
            }
        }

        private void ProgressPopup(ImGuiIOPtr io)
        {
            if(wasShowingProgress != showingProgress && showingProgress)
            {
                ImGui.OpenPopup("ShowingProgress");
            }

            if (showingProgress)
            {
                ImGui.SetNextWindowPos(new Vector2((io.DisplaySize.X - 300) / 2, (io.DisplaySize.Y - 200) / 2));
                ImGui.SetNextWindowSize(new Vector2(300, 200));

                ImGui.BeginPopupModal("ShowingProgress", ref showingProgress,
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoDocking |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove);

                ImGui.ProgressBar(progressFraction, new Vector2(250, 20));

                ImGui.EndPopup();

                lock (backgroundLock)
                {
                    if (backgroundThreads.Count == 0)
                    {
                        showingProgress = false;

                        ImGui.CloseCurrentPopup();
                    }
                }
            }

            wasShowingProgress = showingProgress;
        }
    }
}
