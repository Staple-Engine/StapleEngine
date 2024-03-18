using ImGuiNET;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private bool CreateEntityMenu(Transform parent)
    {
        var result = false;

        EditorGUI.Menu("Create", () =>
        {
            foreach (var t in registeredEntityTemplates)
            {
                EditorGUI.MenuItem(t.Name, () =>
                {
                    var e = t.Create();

                    if (e.IsValid)
                    {
                        var transform = e.GetComponent<Transform>() ?? e.AddComponent<Transform>();

                        transform.SetParent(parent);
                    }

                    result = true;
                });

                if (result)
                {
                    break;
                }
            }
        });

        return result;
    }

    internal static string GetProperFileName(string current, string fileName, string path, string extension)
    {
        try
        {
            if (File.Exists(path))
            {
                var counter = 1;

                for (; ; )
                {
                    path = Path.Combine(current, $"{fileName}{counter++}.{extension}");

                    if (File.Exists(path) == false)
                    {
                        break;
                    }
                }
            }

            return path;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void CreateAssetMenu()
    {
        foreach (var pair in registeredAssetTemplates)
        {
            var name = pair.Key;
            var fileName = Path.GetFileNameWithoutExtension(name);
            var extension = Path.GetExtension(name).Substring(1);

            EditorGUI.MenuItem(fileName, () =>
            {
                var currentPath = projectBrowser.currentContentNode?.path ?? Path.Combine(projectBrowser.basePath, "Assets");
                var assetPath = GetProperFileName(currentPath, fileName, Path.Combine(currentPath, $"{fileName}.{extension}"), extension);

                try
                {
                    File.WriteAllBytes(assetPath, pair.Value);

                    RefreshAssets(assetPath.EndsWith(".cs"));
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create asset at {assetPath}: {e}");
                }
            });
        }

        foreach (var pair in registeredAssetTypes)
        {
            var name = pair.Value.Name;

            EditorGUI.MenuItem(name, () =>
            {
                var fileName = name;
                var currentPath = projectBrowser.currentContentNode?.path ?? Path.Combine(projectBrowser.basePath, "Assets");
                var assetPath = GetProperFileName(currentPath, fileName, Path.Combine(currentPath, $"{fileName}.asset"), "asset");

                try
                {
                    var assetInstance = (IStapleAsset)Activator.CreateInstance(pair.Value);

                    if (assetInstance != null && SaveAsset(assetPath, assetInstance))
                    {
                        RefreshAssets(false);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create asset at {assetPath}: {e}");
                }
            });
        }
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
            EditorGUI.Menu("File", () =>
            {
                EditorGUI.MenuItem("New Project", () =>
                {
                    ImGuiNewProject();
                });

                EditorGUI.MenuItem("Open Project", () =>
                {
                    ImGuiOpenProject();
                });

                EditorGUI.MenuItem("Project Settings", () =>
                {
                    var window = EditorWindow.GetWindow<AppSettingsWindow>();

                    window.projectAppSettings = projectAppSettings;
                    window.basePath = basePath;
                });

                EditorGUI.MenuItem("Open Solution", () =>
                {
                    csProjManager.GenerateGameCSProj(currentPlatform, true);
                    csProjManager.OpenGameSolution();
                });

                EditorGUI.MenuItem("Save", () =>
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
                });

                EditorGUI.Separator();

                EditorGUI.MenuItem("Build", () =>
                {
                    var window = EditorWindow.GetWindow<BuildWindow>();

                    window.basePath = basePath;
                });

                EditorGUI.Separator();

                EditorGUI.MenuItem("Exit", () =>
                {
                    window.shouldStop = true;
                });
            });

            EditorGUI.Menu("Edit", () =>
            {
                EditorGUI.MenuItem("Recreate render device", () =>
                {
                    window.forceContextLoss = true;
                });
            });

            EditorGUI.Menu("Assets", () =>
            {
                EditorGUI.Menu("Create", () =>
                {
                    CreateAssetMenu();
                });
            });

            void Recursive(MenuItemInfo parent)
            {
                EditorGUI.MenuItem(parent.name, () =>
                {
                    if (parent.children.Count == 0)
                    {
                        if (parent.onClick != null)
                        {
                            try
                            {
                                parent.onClick();
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    else
                    {
                        foreach (var child in parent.children)
                        {
                            Recursive(child);
                        }
                    }
                });
            }

            foreach(var item in menuItems)
            {
                Recursive(item);
            }

            ImGui.EndMainMenuBar();
        }

        ImGui.End();
    }

    /// <summary>
    /// Renders the entities panel
    /// </summary>
    /// <param name="io">The current ImGUI IO</param>
    private void Entities(ImGuiIOPtr io)
    {
        ImGui.Begin("Entities");

        ImGui.BeginChildFrame(ImGui.GetID("EntityFrame"), new Vector2(0, 0));

        if (Scene.current != null)
        {
            bool skip = false;

            void Recursive(Transform transform)
            {
                if(skip || transform == null)
                {
                    return;
                }

                var entityName = transform.entity.Name;

                EditorGUI.TreeNode(entityName, transform.ChildCount == 0, true, () =>
                {
                    if (ImGui.BeginDragDropTarget())
                    {
                        dropTargetEntity = transform.entity;

                        var payload = ImGui.AcceptDragDropPayload("ENTITY");

                        unsafe
                        {
                            if (payload.NativePtr != null)
                            {
                                var t = draggedEntity.GetComponent<Transform>();

                                t?.SetParent(transform);

                                draggedEntity = default;
                            }
                        }

                        payload = ImGui.AcceptDragDropPayload("ASSET");

                        unsafe
                        {
                            if (payload.NativePtr != null && dragDropPayloads.TryGetValue("ASSET", out var p))
                            {
                                Staple.Editor.ProjectBrowser.dropType = ProjectBrowserDropType.Asset;

                                p.action(p.index, p.item);

                                dragDropPayloads.Clear();
                                dropTargetEntity = default;
                            }
                        }

                        ImGui.EndDragDropTarget();

                        return;
                    }
                    else if (ImGui.BeginDragDropSource())
                    {
                        draggedEntity = transform.entity;

                        ImGui.SetDragDropPayload("ENTITY", nint.Zero, 0);

                        ImGui.EndDragDropSource();

                        return;
                    }

                    if (ImGui.IsItemHovered())
                    {
                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup($"{transform.entity.Identifier}_Context");
                        }
                        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            SetSelectedEntity(transform.entity);
                        }
                    }

                    if (ImGui.BeginPopup($"{transform.entity.Identifier}_Context"))
                    {
                        if (CreateEntityMenu(transform))
                        {
                            ImGui.EndPopup();

                            skip = true;

                            return;
                        }

                        if (ImGui.MenuItem("Duplicate"))
                        {
                            Entity.Instantiate(transform.entity, transform.parent);
                        }

                        if (ImGui.MenuItem("Delete"))
                        {
                            transform.entity.Destroy();

                            ImGui.EndMenu();

                            ImGui.EndPopup();

                            skip = true;

                            return;
                        }

                        ImGui.EndPopup();
                    }

                    foreach (var child in transform)
                    {
                        var childEntity = World.Current.FindEntity(child.entity.Identifier.ID);

                        if (childEntity.IsValid)
                        {
                            var t = childEntity.GetComponent<Transform>();

                            if (t != null)
                            {
                                Recursive(t);
                            }
                        }

                        if (skip)
                        {
                            break;
                        }
                    }
                });
            }

            Scene.IterateEntities((entity) =>
            {
                var transform = entity.GetComponent<Transform>();

                if(transform == null)
                {
                    return;
                }

                if (transform.parent == null)
                {
                    Recursive(transform);
                }
            });
        }

        if (Scene.current != null &&
            ImGui.IsWindowHovered() &&
            ImGui.IsAnyItemHovered() == false &&
            ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("EntityPanelContext");
        }

        if(ImGui.BeginPopup("EntityPanelContext"))
        {
            CreateEntityMenu(null);

            ImGui.EndPopup();
        }

        ImGui.EndChildFrame();

        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload("ENTITY");

            unsafe
            {
                if (payload.NativePtr != null)
                {
                    var t = draggedEntity.GetComponent<Transform>();

                    t?.SetParent(null);
                }
            }

            payload = ImGui.AcceptDragDropPayload("ASSET");

            unsafe
            {
                if (payload.NativePtr != null && dragDropPayloads.TryGetValue("ASSET", out var p))
                {
                    Staple.Editor.ProjectBrowser.dropType = ProjectBrowserDropType.Asset;

                    p.action(p.index, p.item);

                    dragDropPayloads.Clear();
                    dropTargetEntity = default;
                }
            }

            ImGui.EndDragDropTarget();
        }

        ImGui.End();
    }

    /// <summary>
    /// Renders the viewport window
    /// </summary>
    /// <param name="io">The current ImGUI IO</param>
    private void Viewport(ImGuiIOPtr io)
    {
        ImGui.Begin("Viewport", ImGuiWindowFlags.NoBackground);

        mouseIsHoveringImGui = (viewportType == ViewportType.Scene && ImGui.IsItemActive()) == false;

        EditorGUI.TabBar(["Scene", "Game"], (tabIndex) =>
        {
            switch (tabIndex)
            {
                case 0:

                    viewportType = ViewportType.Scene;

                    break;

                case 1:

                    viewportType = ViewportType.Game;

                    break;
            }
        });

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
                EditorGUI.Texture(texture, new Vector2(width, height));
            }

            ImGui.End();
        }

        ImGui.End();
    }

    /// <summary>
    /// Renders the inspector
    /// </summary>
    /// <param name="io">The current ImGUI IO</param>
    private void Inspector(ImGuiIOPtr io)
    {
        EditorGUI.Changed = false;

        ImGui.Begin("Inspector");

        ImGui.BeginChildFrame(ImGui.GetID("Toolbar"), new Vector2(0, 0));

        if (selectedEntity != null && selectedEntity.IsValid)
        {
            var name = selectedEntity.Name;

            var newName = EditorGUI.TextField("Name", name);

            if(name != newName)
            {
                selectedEntity.Name = newName;
            }

            var enabled = selectedEntity.Enabled;

            var newValue = EditorGUI.Toggle("Enabled", enabled);

            if(newValue != enabled)
            {
                selectedEntity.Enabled = newValue;
            }

            var currentLayer = selectedEntity.Layer;
            var layers = LayerMask.AllLayers;

            if (ImGui.BeginCombo("Layer", currentLayer < layers.Count ? layers[(int)currentLayer] : ""))
            {
                for (var j = 0; j < layers.Count; j++)
                {
                    bool selected = j == currentLayer;

                    if (ImGui.Selectable(layers[j], selected))
                    {
                        selectedEntity.Layer = (uint)j;
                    }
                }

                ImGui.EndCombo();
            }

            var counter = 0;

            selectedEntity.IterateComponents((ref IComponent component) =>
            {
                var localComponent = component;
                var removed = false;

                EditorGUI.TreeNode(component.GetType().Name.ExpandCamelCaseName(), false, false, () =>
                {
                    if(removed)
                    {
                        return;
                    }

                    if (localComponent is Transform transform)
                    {
                        transform.LocalPosition = EditorGUI.Vector3Field("Position", transform.LocalPosition);

                        var rotation = transform.LocalRotation.ToEulerAngles();

                        var newRotation = EditorGUI.Vector3Field("Rotation", rotation);

                        if (rotation != newRotation)
                        {
                            transform.LocalRotation = Math.FromEulerAngles(newRotation);
                        }

                        transform.LocalScale = EditorGUI.Vector3Field("Scale", transform.LocalScale);
                    }
                    else
                    {
                        if (cachedEditors.TryGetValue($"{counter}{localComponent.GetType().FullName}", out var editor))
                        {
                            editor.OnInspectorGUI();
                        }
                        else
                        {
                            defaultEditor.target = localComponent;

                            defaultEditor.OnInspectorGUI();
                        }
                    }

                    if (EditorGUI.Changed)
                    {
                        selectedEntity.UpdateComponent(localComponent);
                    }
                },
                () =>
                {
                    EditorGUI.SameLine();

                    EditorGUI.Button("X", () =>
                    {
                        selectedEntity.RemoveComponent(localComponent.GetType());

                        removed = true;
                        resetSelection = true;
                    });
                });
            });

            if(ImGui.Button("Add Component"))
            {
                ImGui.OpenPopup("SelectedEntityComponentList");
            }

            if(ImGui.BeginPopup("SelectedEntityComponentList"))
            {
                ImGui.SetNextWindowPos(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y));
                ImGui.SetNextWindowSize(new Vector2(ImGui.GetContentRegionAvail().X, 0));

                if(ImGui.Begin("##ComponentsList", ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.Tooltip |
                    ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.ChildWindow))
                {
                    foreach(var component in registeredComponents)
                    {
                        if(selectedEntity.GetComponent(component) != null)
                        {
                            continue;
                        }

                        ImGui.Selectable($"{component.Name}##0");

                        if (ImGui.IsItemClicked())
                        {
                            selectedEntity.AddComponent(component);

                            resetSelection = true;

                            ImGui.CloseCurrentPopup();

                            break;
                        }
                    }

                    ImGui.End();
                }

                ImGui.EndPopup();
            }
        }
        else if(selectedProjectNode != null && selectedProjectNodeData != null)
        {
            if (cachedEditors.Count > 0)
            {
                var editor = cachedEditors.First().Value;

                editor.target = selectedProjectNodeData;

                editor.OnInspectorGUI();
            }
        }

        ImGui.EndChildFrame();

        ImGui.End();
    }

    /// <summary>
    /// Renders the bottom panel
    /// </summary>
    /// <param name="io">The current ImGUI IO</param>
    private void BottomPanel(ImGuiIOPtr io)
    {
        ImGui.Begin("BottomPanel");

        ImGui.BeginChildFrame(ImGui.GetID("Toolbar"), new Vector2(0, 32));

        EditorGUI.TabBar(["Project", "Log"], (tabIndex) =>
        {
            activeBottomTab = tabIndex;
        });

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

        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();

        ImGui.End();

        if (ImGui.IsMouseHoveringRect(pos, pos + size, false) &&
            ImGui.IsAnyItemHovered() == false &&
            ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("ProjectBrowserContext");
        }

        if (ImGui.BeginPopup("ProjectBrowserContext"))
        {
            if (ImGui.BeginMenu("Create"))
            {
                CreateAssetMenu();

                ImGui.EndMenu();
            }

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Renders the project browser
    /// </summary>
    /// <param name="io">The current ImGUI IO</param>
    private void ProjectBrowser(ImGuiIOPtr io)
    {
        projectBrowser.Draw(io, (item) =>
        {
            if(item == selectedProjectNode)
            {
                return;
            }

            selectedEntity = default;
            selectedProjectNode = item;
            selectedProjectNodeData = null;

            foreach (var editor in cachedEditors)
            {
                editor.Value?.Destroy();
            }

            cachedEditors.Clear();
            cachedGizmoEditors.Clear();
            EditorGUI.pendingObjectPickers.Clear();

            EditorWindow.GetWindow<AssetPickerWindow>().Close();

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

                var cachePath = ProjectNodeCachePath(item.path);

                if (item.typeName == typeof(Texture).FullName)
                {
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
                }
                else if (item.typeName == typeof(Mesh).FullName)
                {
                    try
                    {
                        original = JsonConvert.DeserializeObject<MeshAssetMetadata>(data);
                        selectedProjectNodeData = JsonConvert.DeserializeObject<MeshAssetMetadata>(data);
                    }
                    catch (Exception)
                    {
                    }

                    if (original != null && selectedProjectNodeData != null)
                    {
                        var editor = new MeshAssetEditor()
                        {
                            original = original as MeshAssetMetadata,
                            path = $"{item.path}.meta",
                            cachePath = cachePath,
                            target = selectedProjectNodeData,
                        };

                        cachedEditors.Add("", editor);
                    }
                }
                else if (item.typeName == typeof(Material).FullName)
                {
                    try
                    {
                        var d = File.ReadAllText(item.path);

                        original = JsonConvert.DeserializeObject<MaterialMetadata>(d);
                        selectedProjectNodeData = JsonConvert.DeserializeObject<MaterialMetadata>(d);

                        if (original != null && selectedProjectNodeData != null)
                        {
                            var guid = AssetDatabase.GetAssetGuid(item.path);

                            var o = original as MaterialMetadata;
                            var t = selectedProjectNodeData as MaterialMetadata;

                            o.guid = t.guid = guid;

                            var editor = new MaterialEditor()
                            {
                                original = original as MaterialMetadata,
                                path = item.path,
                                cachePath = cachePath,
                                target = selectedProjectNodeData,
                            };

                            cachedEditors.Add("", editor);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (item.typeName == typeof(Shader).FullName)
                {
                    try
                    {
                        original = JsonConvert.DeserializeObject<ShaderMetadata>(data);
                        selectedProjectNodeData = JsonConvert.DeserializeObject<ShaderMetadata>(data);
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (item.typeName == typeof(AudioClip).FullName)
                {
                    try
                    {
                        original = JsonConvert.DeserializeObject<AudioClipMetadata>(data);
                        selectedProjectNodeData = JsonConvert.DeserializeObject<AudioClipMetadata>(data);

                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                        if (editor != null)
                        {
                            editor.original = original;
                            editor.path = $"{item.path}.meta";
                            editor.cachePath = cachePath;

                            cachedEditors.Add("", editor);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (item.typeName == typeof(FolderAsset).FullName)
                {
                    try
                    {
                        original = JsonConvert.DeserializeObject<FolderAsset>(data);
                        selectedProjectNodeData = JsonConvert.DeserializeObject<FolderAsset>(data);

                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                        if(editor != null)
                        {
                            editor.original = original;
                            editor.path = $"{item.path}.meta";
                            editor.cachePath = $"{cachePath}.meta";

                            cachedEditors.Add("", editor);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    var type = TypeCache.GetType(item.typeName);

                    if(type != null && typeof(IStapleAsset).IsAssignableFrom(type))
                    {
                        try
                        {
                            var byteData = File.ReadAllBytes(item.path);

                            using var stream = new MemoryStream(byteData);

                            var header = MessagePackSerializer.Deserialize<SerializableStapleAssetHeader>(stream);

                            if(header.header.SequenceEqual(SerializableStapleAssetHeader.ValidHeader) &&
                                header.version == SerializableStapleAssetHeader.ValidVersion)
                            {
                                var asset = MessagePackSerializer.Deserialize<SerializableStapleAsset>(stream);

                                if(asset != null)
                                {
                                    selectedProjectNodeData = AssetSerialization.Deserialize(asset);
                                    original = AssetSerialization.Deserialize(asset);

                                    if (selectedProjectNodeData != null)
                                    {
                                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                                        if (editor == null)
                                        {
                                            editor = new StapleAssetEditor()
                                            {
                                                target = selectedProjectNodeData,
                                            };
                                        }

                                        editor.original = original;
                                        editor.path = item.path;
                                        editor.cachePath = cachePath;

                                        cachedEditors.Add("", editor);
                                    }
                                }
                            }
                        }
                        catch(Exception)
                        {
                        }
                    }
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

                    Scene.SetActiveScene(scene);

                    if (scene != null)
                    {
                        lastOpenScene = item.path;

                        ResetScenePhysics();

                        UpdateLastSession();
                    }

                    break;
            }
        });
    }

    /// <summary>
    /// Renders the console
    /// </summary>
    /// <param name="io">The current ImGUI IO</param>
    private void Console(ImGuiIOPtr io)
    {
    }

    /// <summary>
    /// Shows the progress popup for the current background task
    /// </summary>
    /// <param name="io">The current ImGUI IO</param>
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
