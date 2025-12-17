using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using MessagePack;
using Newtonsoft.Json;
using Staple.Internal;
using Staple.ProjectManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private bool CreateEntityMenu(Transform parent)
    {
        var result = false;

        EditorGUI.Menu("Create", $"{parent?.Entity}Create", () =>
        {
            foreach (var t in registeredEntityTemplates)
            {
                EditorGUI.MenuItem(t.Name, $"{parent?.Entity}Create{t.Name}", () =>
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

    internal static string GetProperFolderName(string current, string name, string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                var counter = 1;

                for (; ; )
                {
                    path = Path.Combine(current, $"{name}{counter++}");

                    if (Directory.Exists(path) == false)
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
        EditorGUI.MenuItem("Folder", "FOLDER", () =>
        {
            try
            {
                var currentPath = projectBrowser.currentContentNode?.path ?? Path.Combine(projectBrowser.basePath, "Assets");
                var path = GetProperFolderName(currentPath, "New Folder", Path.Combine(currentPath, "New Folder"));

                Directory.CreateDirectory(path);

                RefreshAssets(false, null);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to create folder: {e}");
            }
        });

        foreach(var pair in GeneratorAssetManager.generators)
        {
            var name = pair.Value.instance.GetType().Name.ExpandCamelCaseName();
            var fileName = pair.Value.instance.GetType().Name;
            var extension = pair.Value.extension;

            EditorGUI.MenuItem(name, $"{pair.Key}Create", () =>
            {
                var currentPath = projectBrowser.currentContentNode?.path ?? Path.Combine(projectBrowser.basePath, "Assets");
                var assetPath = GetProperFileName(currentPath, fileName, Path.Combine(currentPath, $"{fileName}.{extension}"), extension);

                try
                {
                    var result = pair.Value.instance.CreateNew();

                    if(result != null)
                    {
                        if(pair.Value.isText)
                        {
                            var text = Encoding.UTF8.GetString(result);

                            File.WriteAllText(assetPath, text);
                        }
                        else
                        {
                            File.WriteAllBytes(assetPath, result);
                        }

                        RefreshAssets(false, null);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create asset at {assetPath}: {e}");
                }
            });
        }

        foreach (var pair in registeredAssetTemplates)
        {
            var name = pair.Key;
            var fileName = Path.GetFileNameWithoutExtension(name);
            var extension = Path.GetExtension(name).Substring(1);

            EditorGUI.MenuItem(fileName, $"{pair.Key}Create", () =>
            {
                var currentPath = projectBrowser.currentContentNode?.path ?? Path.Combine(projectBrowser.basePath, "Assets");
                var assetPath = GetProperFileName(currentPath, fileName, Path.Combine(currentPath, $"{fileName}.{extension}"), extension);

                try
                {
                    File.WriteAllBytes(assetPath, pair.Value);

                    RefreshAssets(false, null);
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

            EditorGUI.MenuItem(name, $"{pair.Key}Create", () =>
            {
                var fileName = name;
                var currentPath = projectBrowser.currentContentNode?.path ?? Path.Combine(projectBrowser.basePath, "Assets");
                var assetPath = GetProperFileName(currentPath, fileName, Path.Combine(currentPath, $"{fileName}.asset"), "asset");

                try
                {
                    var assetInstance = (IStapleAsset)Activator.CreateInstance(pair.Value);

                    if (assetInstance != null)
                    {
                        if(assetInstance is IGuidAsset guidAsset)
                        {
                            guidAsset.Guid.Guid = GuidGenerator.Generate().ToString();
                        }

                        if(SaveAsset(assetPath, assetInstance))
                        {
                            RefreshAssets(false, null);
                        }
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
            EditorGUI.Menu("File", "File.Menu", () =>
            {
                EditorGUI.Disabled(playMode != PlayMode.Stopped, () =>
                {
                    EditorGUI.MenuItem("New Project", "NewProject.Menu", () =>
                    {
                        ImGuiNewProject();
                    });

                    EditorGUI.MenuItem("Open Project", "OpenProject.Menu", () =>
                    {
                        ImGuiOpenProject();
                    });

                    EditorGUI.MenuItem("Editor Settings", "EditorSettings.Menu", () =>
                    {
                        var window = EditorWindow.GetWindow<EditorSettingsWindow>();

                        window.editorSettings = editorSettings;
                    });

                    EditorGUI.MenuItem("Save", "Save.Menu", () =>
                    {
                        if (Scene.current != null && lastOpenScene != null)
                        {
                            if (sceneMode == SceneMode.Prefab)
                            {
                                var targetEntity = new Entity();

                                World.Current.Iterate((entity) =>
                                {
                                    if (targetEntity.IsValid)
                                    {
                                        return;
                                    }

                                    if (entity.TryGetComponent<Transform>(out var t) && t.Parent == null)
                                    {
                                        targetEntity = entity;
                                    }
                                });

                                if (targetEntity.IsValid)
                                {
                                    var prefab = SceneSerialization.SerializeIntoPrefab(targetEntity);

                                    if (prefab != null)
                                    {
                                        //TODO: Support prefab local ID and GUIDs properly
                                        prefab.mainObject.prefabLocalID = 0;
                                        prefab.mainObject.prefabGuid = null;

                                        foreach (var child in prefab.children)
                                        {
                                            child.prefabLocalID = 0;
                                            child.prefabGuid = null;
                                        }

                                        var previous = ResourceManager.instance.LoadRawPrefabFromPath(lastOpenScene);

                                        if (previous != null)
                                        {
                                            prefab.guid = previous.Guid.Guid;
                                        }

                                        var text = JsonConvert.SerializeObject(prefab, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                        try
                                        {
                                            File.WriteAllText(lastOpenScene, text);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var serializableScene = Scene.current.Serialize();

                                var text = JsonConvert.SerializeObject(serializableScene.objects, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                                try
                                {
                                    File.WriteAllText(lastOpenScene, text);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    });

                    EditorGUI.Separator();

                    EditorGUI.MenuItem("Build", "Build.Menu", () =>
                    {
                        var window = EditorWindow.GetWindow<BuildWindow>();

                        window.basePath = BasePath;
                    });
                });

                EditorGUI.Separator();

                EditorGUI.MenuItem("Exit", "Exit.Menu", () =>
                {
                    window.shouldStop = true;
                });
            });

            EditorGUI.Menu("Edit", "Edit.Menu", () =>
            {
                EditorGUI.MenuItem("Recreate render device", "ResetDevice.Menu", () =>
                {
                    window.forceContextLoss = true;
                });
            });

            EditorGUI.Menu("Project", "Project.Menu", () =>
            {
                EditorGUI.Disabled(playMode != PlayMode.Stopped, () =>
                {
                    EditorGUI.MenuItem("Project Settings", "ProjectSettings.Menu", () =>
                    {
                        var window = EditorWindow.GetWindow<AppSettingsWindow>();

                        window.projectAppSettings = projectAppSettings;
                        window.basePath = BasePath;
                    });

                    EditorGUI.MenuItem("Package Manager", "PackageManager.Menu", () =>
                    {
                        var window = EditorWindow.GetWindow<PackageManagerWindow>();

                        window.basePath = BasePath;
                    });

                    EditorGUI.MenuItem("Rebuild", "RebuildProject.Menu", () =>
                    {
                        RefreshStaging(currentPlatform, null, true, false);
                    });
                });

                EditorGUI.MenuItem("Open Solution", "OpenSolution.Menu", () =>
                {
                    var backend = PlayerBackendManager.Instance.GetBackend(buildBackend);

                    if (backend == null)
                    {
                        return;
                    }

                    ProjectManager.Instance.GenerateGameCSProj(backend, projectAppSettings, currentPlatform, true);
                    ProjectManager.Instance.OpenGameSolution();
                });
            });

            EditorGUI.Menu("Assets", "Assets.Menu", () =>
            {
                EditorGUI.Disabled(playMode != PlayMode.Stopped, () =>
                {
                    EditorGUI.Menu("Create", "AssetsCreate.Menu", () =>
                    {
                        CreateAssetMenu();
                    });

                    EditorGUI.Separator();

                    EditorGUI.MenuItem("Reimport all", "Assets.Reimport.Menu", () =>
                    {
                        try
                        {
                            Directory.Delete(Path.Combine(BasePath, "Cache", "Staging"), true);
                            Directory.Delete(Path.Combine(BasePath, "Cache", "Thumbnails"), true);
                        }
                        catch (Exception)
                        {
                        }

                        RefreshStaging(currentPlatform, null, false);
                    });
                });
            });

            if(needsGameRecompile && playMode == PlayMode.Stopped)
            {
                EditorGUI.Menu("Recompile Game", "Game.Rebuild", () =>
                {
                    needsGameRecompile = false;

                    UnloadGame();

                    RefreshStaging(currentPlatform, null, true);
                });
            }

            void Recursive(MenuItemInfo parent)
            {
                if(parent.children.Count > 0)
                {
                    EditorGUI.Menu(parent.name, $"{parent.name}{parent.children.Count}", () =>
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
                else
                {
                    EditorGUI.MenuItem(parent.name, $"{parent.name}{parent.children.Count}", () =>
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

        ImGui.BeginChild(ImGui.GetID("EntityFrame"), new Vector2(0, 0), ImGuiChildFlags.None);

        if (Scene.current != null)
        {
            var skip = false;

            var entityIcon = projectBrowser.GetEditorResource("EntityIcon");

            void Recursive(Transform transform)
            {
                if(skip ||
                    transform == null ||
                    transform.Entity.Layer == LayerMask.NameToLayer(RenderTargetLayerName) ||
                    transform.Entity.HierarchyVisibility == EntityHierarchyVisibility.Hide ||
                    transform.Entity.HierarchyVisibility == EntityHierarchyVisibility.HideAndDontSave)
                {
                    return;
                }

                var entityName = transform.Entity.Name;

                var hasPrefab = transform.Entity.TryGetPrefab(out _, out _);

                if(hasPrefab)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiProxy.ImGuiRGBA(PrefabColor));
                }

                void HandleReorder(bool before)
                {
                    var beforeString = before ? "BEFORE" : "AFTER";

                    ImGui.Selectable($"##{transform.Entity.Name}-{transform.Entity.Identifier.ID}-{beforeString}",
                        draggedEntity.IsValid ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled,
                        new Vector2(ImGui.GetContentRegionAvail().X, 2));

                    if (ImGui.BeginDragDropTarget())
                    {
                        var targetTransform = transform.Parent;
                        var siblingIndex = transform.SiblingIndex + (draggedEntity.GetComponent<Transform>() == transform.Parent ? 0 : before ? -1 : 1);
                        var childCount = (transform.Parent?.ChildCount ?? 0);

                        if (siblingIndex < 0)
                        {
                            siblingIndex = 0;
                        }
                        else if(siblingIndex >= childCount && childCount > 0)
                        {
                            siblingIndex = childCount - 1;
                        }

                        var payload = ImGui.AcceptDragDropPayload("ENTITY");

                        unsafe
                        {
                            if (payload.Handle != null)
                            {
                                var t = draggedEntity.GetComponent<Transform>();

                                t?.SetParent(targetTransform);
                                t?.SetSiblingIndex(siblingIndex);

                                draggedEntity = default;
                            }
                        }

                        ImGui.EndDragDropTarget();

                        return;
                    }
                }

                if(transform.SiblingIndex == 0 && transform.Parent != null)
                {
                    HandleReorder(true);
                }

                if(entityTreeStates.TryGetValue(transform.Entity, out var open) == false)
                {
                    entityTreeStates.Add(transform.Entity, open);
                }

                EditorGUI.TreeNodeIcon(entityIcon, hasPrefab ? PrefabColor : Color.White, entityName,
                    $"{transform.Entity}", transform.ChildCount == 0, ref open, () =>
                {
                    foreach (var child in transform.Children)
                    {
                        var childEntity = World.Current.FindEntity(child.Entity.Identifier.ID);

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
                },
                () =>
                {
                    if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        ZoomInEntity(transform.Entity);
                    }
                    else if (Input.GetMouseButtonUp(MouseButton.Right))
                    {
                        ImGui.OpenPopup($"{transform.Entity.Identifier}_Context");
                    }
                    else if (Input.GetMouseButtonUp(MouseButton.Left))
                    {
                        SetSelectedEntity(transform.Entity);
                    }
                },
                () =>
                {
                    if (hasPrefab)
                    {
                        ImGui.PopStyleColor();
                    }

                    if (ImGui.BeginPopup($"{transform.Entity.Identifier}_Context"))
                    {
                        if (CreateEntityMenu(transform))
                        {
                            ImGui.EndPopup();

                            skip = true;

                            return;
                        }

                        if ((sceneMode != SceneMode.Prefab || transform.Parent != null) &&
                            ImGui.MenuItem("Duplicate"))
                        {
                            Entity.Instantiate(transform.Entity, transform.Parent);
                        }

                        if ((sceneMode != SceneMode.Prefab || transform.Parent != null) &&
                            ImGui.MenuItem("Delete"))
                        {
                            transform.Entity.Destroy();

                            ImGui.EndPopup();

                            skip = true;

                            return;
                        }

                        ImGui.EndPopup();
                    }

                    if (ImGui.BeginDragDropTarget())
                    {
                        dropTargetEntity = transform.Entity;

                        var payload = ImGui.AcceptDragDropPayload("ENTITY");

                        unsafe
                        {
                            if (payload.Handle != null)
                            {
                                var t = draggedEntity.GetComponent<Transform>();

                                t?.SetParent(transform);

                                draggedEntity = default;
                            }
                        }

                        payload = ImGui.AcceptDragDropPayload("ASSET");

                        unsafe
                        {
                            if (payload.Handle != null && dragDropPayloads.TryGetValue("ASSET", out var p))
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
                        draggedEntity = transform.Entity;

                        unsafe
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes("ENTITY");

                            fixed (byte* b = buffer)
                            {
                                ImGui.SetDragDropPayload(b, (void*)0, 0);
                            }
                        }

                        ImGui.EndDragDropSource();

                        return;
                    }
                });

                entityTreeStates[transform.Entity] = open;

                HandleReorder(false);
            }

            foreach(var (entity, transform) in Scene.RootEntities)
            {
                if (transform.Parent == null)
                {
                    Recursive(transform);
                }
            }
        }

        if (Scene.current != null &&
            ImGui.IsWindowHovered() &&
            ImGui.IsAnyItemHovered() == false &&
            Input.GetMouseButtonUp(MouseButton.Right) &&
            sceneMode != SceneMode.Prefab)
        {
            ImGui.OpenPopup("EntityPanelContext");
        }

        if(ImGui.BeginPopup("EntityPanelContext"))
        {
            CreateEntityMenu(null);

            ImGui.EndPopup();
        }

        ImGui.EndChild();

        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload("ENTITY");

            unsafe
            {
                if (payload.Handle != null)
                {
                    var t = draggedEntity.GetComponent<Transform>();

                    t?.SetParent(null);
                }
            }

            payload = ImGui.AcceptDragDropPayload("ASSET");

            unsafe
            {
                if (payload.Handle != null && dragDropPayloads.TryGetValue("ASSET", out var p))
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

        var horizontalSpace = EditorGUI.RemainingHorizontalSpace();

        var buttonSizes = EditorGUI.GetTextSize("Play").X + EditorGUI.GetTextSize("Stop").X + ImGui.GetStyle().FramePadding.X * 4;

        var centerSpace = (horizontalSpace - buttonSizes) / 2;

        EditorGUI.SetCurrentGUICursorPosition(EditorGUI.CurrentGUICursorPosition() + new Vector2(centerSpace, 0));

        EditorGUI.Disabled(World.Current == null, () =>
        {
            EditorGUI.Button(playMode == PlayMode.Playing ? "Pause" : "Play", "GameView.Play", () =>
            {
                switch (playMode)
                {
                    case PlayMode.Playing:

                        Platform.IsPlaying = false;

                        playMode = PlayMode.Paused;

                        break;

                    case PlayMode.Paused:

                        Platform.IsPlaying = true;

                        playMode = PlayMode.Playing;

                        if (forceCursorVisible)
                        {
                            forceCursorVisible = false;

                            Cursor.LockState = CursorLockMode.Locked;
                            Cursor.Visible = false;
                        }

                        break;

                    case PlayMode.Stopped:

                        RecordScene();

                        playMode = PlayMode.Playing;

                        viewportType = ViewportType.Game;

                        ResetScenePhysics(true);

                        RecreateRigidBodies();

                        Platform.IsPlaying = true;

                        //Ensure the components are properly initialized
                        World.Current.Iterate((entity) =>
                        {
                            World.Current.IterateComponents(entity, (ref IComponent component) =>
                            {
                                World.Current.EmitAddComponentEvent(entity, ref component);
                            });
                        });

                        forceCursorVisible = false;

                        EntitySystemManager.Instance.StartupAllSystems();

                        break;
                }
            });
        });

        EditorGUI.SameLine();

        EditorGUI.Disabled(World.Current == null || playMode == PlayMode.Stopped, () =>
        {
            EditorGUI.Button("Stop", "GameView.Stop", () =>
            {
                playMode = PlayMode.Stopped;

                EntitySystemManager.Instance.ShutdownAllSystems();

                Platform.IsPlaying = false;

                forceCursorVisible = false;

                ResetScenePhysics(true);

                LoadRecordedScene();

                ResetScenePhysics(false);
            });
        });

        EditorGUI.TabBar(["Scene", "Game"], "SCENEGAME", null, 
            (tabIndex) =>
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

        switch(viewportType)
        {
            case ViewportType.Scene:

                RenderScene();

                break;

            case ViewportType.Game:

                ImGui.BeginChild(ImGui.GetID("GameView"), new Vector2(0, 0), ImGuiWindowFlags.NoBackground);

                var width = (ushort)ImGui.GetContentRegionAvail().X;
                var height = (ushort)ImGui.GetContentRegionAvail().Y;

                if (gameRenderTarget == null || gameRenderTarget.width != width || gameRenderTarget.height != height)
                {
                    gameRenderTarget?.Destroy();

                    gameRenderTarget = RenderTarget.Create(width, height);
                }

                gameWindowPosition = ImGui.GetWindowPos();

                if (gameRenderTarget != null && Scene.current != null)
                {
                    ExecuteGameViewHandler(() =>
                    {
                        RenderSystem.Instance.Update();
                    });

                    /*
                    gameRenderTarget.colorTextures[0].ReadPixels((t, b) =>
                    {
                        var rawData = new RawTextureData()
                        {
                            colorComponents = StandardTextureColorComponents.RGBA,
                            data = b,
                            width = t.Width,
                            height = t.Height,
                        };

                        var pngData = rawData.EncodePNG();

                        File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, "Debug.png"), pngData);
                    });
                    */
                }

                var texture = gameRenderTarget.GetColorTexture();

                if (texture != null)
                {
                    EditorGUI.Texture(texture, new Vector2(width, height));

                    if (ImGui.IsItemHovered() && Input.GetMouseButtonUp(MouseButton.Left))
                    {
                        if (forceCursorVisible)
                        {
                            forceCursorVisible = false;

                            Cursor.LockState = CursorLockMode.Locked;
                            Cursor.Visible = false;
                        }
                    }
                }

                ImGui.EndChild();

                break;
        }

        ImGui.End();

        if(io.WantTextInput == false)
        {
            if (Input.GetKeyUp(KeyCode.E))
            {
                transformOperation = ImGuizmoOperation.Scale;
            }

            if (Input.GetKeyUp(KeyCode.T))
            {
                transformOperation = ImGuizmoOperation.Translate;
            }

            if (Input.GetKeyUp(KeyCode.R))
            {
                transformOperation = ImGuizmoOperation.Rotate;
            }

            if (Input.GetKeyUp(KeyCode.L))
            {
                transformMode = ImGuizmoMode.Local;
            }

            if (Input.GetKeyUp(KeyCode.K))
            {
                transformMode = ImGuizmoMode.World;
            }
        }
    }

    /// <summary>
    /// Renders the inspector
    /// </summary>
    /// <param name="io">The current ImGUI IO</param>
    private void Inspector(ImGuiIOPtr io)
    {
        EditorGUI.Changed = false;

        ImGui.Begin("Inspector");

        ImGui.BeginChild(ImGui.GetID("Toolbar"), new Vector2(0, 0), ImGuiChildFlags.None);

        if (selectedEntity != null && selectedEntity.IsValid)
        {
            var name = selectedEntity.Name;

            var newName = EditorGUI.TextField("Name", "SELECTEDNAME", name);

            if(name != newName)
            {
                selectedEntity.Name = newName;
            }

            var enabled = selectedEntity.Enabled;

            var newValue = EditorGUI.Toggle("Enabled", "SELECTEDENABLED", enabled);

            if(newValue != enabled)
            {
                selectedEntity.Enabled = newValue;
            }

            var currentLayer = selectedEntity.Layer;
            var layers = LayerMask.AllLayers
                .Where(x => x != RenderTargetLayerName)
                .ToArray();

            var newLayer = EditorGUI.Dropdown("Layer", "SELECTEDLAYER", layers, (int)currentLayer);

            if(newLayer != currentLayer)
            {
                selectedEntity.Layer = (uint)newLayer;
            }

            var counter = 0;

            selectedEntity.IterateComponents((ref IComponent component) =>
            {
                counter++;

                var localComponent = component;
                var removed = false;

                EditorGUI.CollapsingHeader(component.GetType().Name.ExpandCamelCaseName(), $"SELECTED{component.GetType().FullName}",
                    component is not Transform,
                    () =>
                    {
                        if(removed)
                        {
                            return;
                        }

                        if (localComponent is Transform transform)
                        {
                            transform.LocalPosition = EditorGUI.Vector3Field("Position", $"SELECTED{localComponent.GetType().FullName}POSITION",
                                transform.LocalPosition);

                            var rotation = transform.LocalRotation.ToEulerAngles();

                            var newRotation = EditorGUI.Vector3Field("Rotation", $"SELECTED{localComponent.GetType().FullName}ROTATION", rotation);

                            if (rotation != newRotation)
                            {
                                transform.LocalRotation = Quaternion.Euler(newRotation);
                            }

                            transform.LocalScale = EditorGUI.Vector3Field("Scale", $"SELECTED{localComponent.GetType().FullName}SCALE",
                                transform.LocalScale);
                        }
                        else
                        {
                            if (cachedEditors.TryGetValue($"{counter}{localComponent.GetType().FullName}", out var editor))
                            {
                                editor.OnInspectorGUI();
                            }
                            else
                            {
                                cachedEditors.Add($"{counter}{localComponent.GetType().FullName}", new Editor()
                                {
                                    target = localComponent
                                });
                            }
                        }

                        if (EditorGUI.Changed)
                        {
                            selectedEntity.SetComponent(localComponent);
                        }
                    },
                    () =>
                    {
                        selectedEntity.RemoveComponent(localComponent.GetType());

                        removed = true;
                        resetSelection = true;
                    }, true);
            });

            if(ImGui.Button("Add Component"))
            {
                ImGui.OpenPopup("SelectedEntityComponentList");
            }

            if(ImGui.BeginPopup("SelectedEntityComponentList"))
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

                ImGui.EndPopup();
            }
        }
        else if(selectedProjectNode != null && selectedProjectNodeData != null)
        {
            if (cachedEditors.Count > 0)
            {
                EditorGUI.Label($"{selectedProjectNode.name} ({selectedProjectNode.friendlyTypeName})");

                var editor = cachedEditors.First().Value;

                editor.OnInspectorGUI();
            }
        }

        ImGui.EndChild();

        ImGui.End();
    }

    /// <summary>
    /// Renders the bottom panel
    /// </summary>
    /// <param name="io">The current ImGUI IO</param>
    private void BottomPanel(ImGuiIOPtr io)
    {
        ImGui.Begin("Project");

        ImGui.BeginChild(ImGui.GetID("Toolbar"), new Vector2(0, 32), ImGuiChildFlags.None);

        EditorGUI.TabBar(["Folders", "Log"], "PROJECT", null, (tabIndex) =>
        {
            activeBottomTab = tabIndex;
        });

        ImGui.EndChild();

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
            Input.GetMouseButtonUp(MouseButton.Right))
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
            if (item == selectedProjectNode)
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

            if(EditorWindow.TryGetWindow<AssetPickerWindow>(out var w))
            {
                w.Close();
            }

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

                var cachePath = EditorUtils.GetAssetCachePath(BasePath, item.path, currentPlatform);

                bool GetAssetGUID(out string guid)
                {
                    try
                    {
                        var holder = JsonConvert.DeserializeObject<AssetHolder>(data);

                        if ((holder?.guid?.Length ?? 0) > 0)
                        {
                            guid = holder.guid;

                            return true;
                        }
                    }
                    catch (Exception)
                    {
                    }

                    guid = default;

                    return false;
                }

                if (GetAssetGUID(out var guid) && GeneratorAssetManager.TryGetGeneratorAsset(guid, out var generator))
                {
                    original = generator;
                    selectedProjectNodeData = generator;

                    var editor = Editor.CreateEditor(selectedProjectNodeData);

                    if (editor != null)
                    {
                        editor.original = original;
                        editor.path = item.path;
                        editor.cachePath = cachePath;

                        cachedEditors.Add("", editor);

                        return;
                    }
                }

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
                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                        if (editor is AssetEditor e)
                        {
                            e.original = original;
                            e.path = $"{item.path}.meta";
                            e.cachePath = cachePath;
                            e.recreateOriginal = () => JsonConvert.DeserializeObject<TextureMetadata>(File.ReadAllText($"{item.path}.meta"));
                        }

                        cachedEditors.Add("", editor);
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
                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                        if (editor is AssetEditor e)
                        {
                            e.original = original;
                            e.path = $"{item.path}.meta";
                            e.cachePath = cachePath;
                            e.recreateOriginal = () => JsonConvert.DeserializeObject<MeshAssetMetadata>(File.ReadAllText($"{item.path}.meta"));
                        }

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

                        if (original != null && selectedProjectNodeData != null &&
                            original is MaterialMetadata originalMetadata &&
                            selectedProjectNodeData is MaterialMetadata targetMetadata)
                        {
                            var assetHolder = JsonConvert.DeserializeObject<AssetHolder>(File.ReadAllText($"{item.path}.meta"));

                            originalMetadata.guid = targetMetadata.guid = assetHolder.guid;

                            var editor = Editor.CreateEditor(selectedProjectNodeData);

                            if (editor is AssetEditor e)
                            {
                                e.original = original;
                                e.path = item.path;
                                e.cachePath = cachePath;
                                e.recreateOriginal = () =>
                                {
                                    var assetHolder = JsonConvert.DeserializeObject<AssetHolder>(File.ReadAllText($"{item.path}.meta"));

                                    var o = JsonConvert.DeserializeObject<MaterialMetadata>(File.ReadAllText(item.path));

                                    o.guid = assetHolder.guid;

                                    return o;
                                };
                            }

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

                        if (editor is AssetEditor e)
                        {
                            e.original = original;
                            e.path = $"{item.path}.meta";
                            e.cachePath = cachePath;
                            e.recreateOriginal = () => JsonConvert.DeserializeObject<AudioClipMetadata>(File.ReadAllText($"{item.path}.meta"));
                        }

                        cachedEditors.Add("", editor);
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

                        if (editor is AssetEditor e)
                        {
                            e.original = original;
                            e.path = $"{item.path}.meta";
                            e.cachePath = $"{cachePath}.meta";
                            e.recreateOriginal = () => JsonConvert.DeserializeObject<FolderAsset>(File.ReadAllText($"{item.path}.meta"));
                        }

                        cachedEditors.Add("", editor);
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (item.typeName == typeof(FontAsset).FullName)
                {
                    try
                    {
                        original = JsonConvert.DeserializeObject<FontMetadata>(data);
                        selectedProjectNodeData = JsonConvert.DeserializeObject<FontMetadata>(data);
                    }
                    catch (Exception)
                    {
                    }

                    if (original != null && selectedProjectNodeData != null)
                    {
                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                        if (editor is AssetEditor e)
                        {
                            e.original = original;
                            e.path = $"{item.path}.meta";
                            e.cachePath = cachePath;
                            e.recreateOriginal = () => JsonConvert.DeserializeObject<FontMetadata>(File.ReadAllText($"{item.path}.meta"));

                            cachedEditors.Add("", editor);
                        }
                    }
                }
                else if (item.typeName == typeof(AssemblyDefinition).FullName)
                {
                    try
                    {
                        original = AssemblyDefinition.Create(guid);
                        selectedProjectNodeData = AssemblyDefinition.Create(guid);
                    }
                    catch (Exception)
                    {
                    }

                    if (original != null && selectedProjectNodeData != null)
                    {
                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                        if (editor is AssetEditor e)
                        {
                            e.original = original;
                            e.path = item.path;
                            e.cachePath = cachePath;
                            e.recreateOriginal = () => AssemblyDefinition.Create(guid);

                            cachedEditors.Add("", editor);
                        }
                    }
                }
                else if (item.typeName == typeof(PluginAsset).FullName)
                {
                    try
                    {
                        original = JsonConvert.DeserializeObject<PluginAsset>(data, Tooling.Utilities.JsonSettings);
                        selectedProjectNodeData = JsonConvert.DeserializeObject<PluginAsset>(data, Tooling.Utilities.JsonSettings);
                    }
                    catch (Exception)
                    {
                    }

                    if (original != null && selectedProjectNodeData != null)
                    {
                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                        if (editor is AssetEditor e)
                        {
                            e.original = original;
                            e.path = item.path;
                            e.cachePath = cachePath;
                            e.recreateOriginal = () => JsonConvert.DeserializeObject<PluginAsset>(File.ReadAllText($"{item.path}.meta"), Tooling.Utilities.JsonSettings);

                            cachedEditors.Add("", editor);
                        }
                    }
                }
                else if (item.typeName == typeof(TextAsset).FullName)
                {
                    try
                    {
                        original = ResourceManager.instance.LoadTextAsset(guid);
                        selectedProjectNodeData = ResourceManager.instance.LoadTextAsset(guid);
                    }
                    catch (Exception)
                    {
                    }

                    if (original != null && selectedProjectNodeData != null)
                    {
                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                        if (editor is Editor e)
                        {
                            e.original = original;
                            e.path = item.path;
                            e.cachePath = cachePath;

                            cachedEditors.Add("", editor);
                        }
                    }
                }
                else
                {
                    var type = TypeCache.GetType(item.typeName);

                    if (type != null && typeof(IStapleAsset).IsAssignableFrom(type))
                    {
                        try
                        {
                            var byteData = File.ReadAllBytes(cachePath);

                            using var stream = new MemoryStream(byteData);

                            var header = MessagePackSerializer.Deserialize<SerializableStapleAssetHeader>(stream);

                            if (header.header.SequenceEqual(SerializableStapleAssetHeader.ValidHeader) &&
                                header.version == SerializableStapleAssetHeader.ValidVersion)
                            {
                                var asset = MessagePackSerializer.Deserialize<SerializableStapleAsset>(stream);

                                if (asset != null)
                                {
                                    selectedProjectNodeData = AssetSerialization.Deserialize(asset, StapleSerializationMode.Binary);
                                    original = AssetSerialization.Deserialize(asset, StapleSerializationMode.Binary);

                                    if (selectedProjectNodeData != null)
                                    {
                                        var editor = Editor.CreateEditor(selectedProjectNodeData);

                                        editor ??= new StapleAssetEditor()
                                        {
                                            target = selectedProjectNodeData,
                                        };

                                        editor.original = original;
                                        editor.path = item.path;
                                        editor.cachePath = cachePath;

                                        if (editor is AssetEditor e)
                                        {
                                            e.recreateOriginal = () =>
                                            {
                                                var byteData = File.ReadAllBytes(cachePath);

                                                using var stream = new MemoryStream(byteData);

                                                var header = MessagePackSerializer.Deserialize<SerializableStapleAssetHeader>(stream);

                                                if (header.header.SequenceEqual(SerializableStapleAssetHeader.ValidHeader) &&
                                                    header.version == SerializableStapleAssetHeader.ValidVersion)
                                                {
                                                    var asset = MessagePackSerializer.Deserialize<SerializableStapleAsset>(stream);

                                                    if (asset != null)
                                                    {
                                                        return AssetSerialization.Deserialize(asset, StapleSerializationMode.Binary);
                                                    }
                                                }

                                                return default;
                                            };
                                        }

                                        cachedEditors.Add("", editor);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        },
        (item) =>
        {
            switch (item.type)
            {
                case ProjectBrowserNodeType.File:

                    switch (item.action)
                    {
                        case ProjectBrowserNodeAction.InspectScene:

                            World.Current?.Iterate((entity) =>
                            {
                                World.Current.IterateComponents(entity, (ref IComponent component) =>
                                {
                                    if (component is IComponentDisposable disposable)
                                    {
                                        disposable.DisposeComponent();
                                    }
                                });
                            });

                            ResourceManager.instance.Clear();

                            Scene scene = null;

                            if (item.path.EndsWith($".{AssetSerialization.PrefabExtension}"))
                            {
                                var prefab = ResourceManager.instance.LoadRawPrefabFromPath(item.path);

                                if (prefab != null)
                                {
                                    World.Current = new();
                                    scene = Scene.current = new();

                                    SceneSerialization.InstantiatePrefab(default, prefab.data);
                                }
                            }
                            else
                            {
                                scene = ResourceManager.instance.LoadRawSceneFromPath(item.path);
                            }

                            Scene.SetActiveScene(scene);

                            undoStack.Clear();

                            if (scene != null)
                            {
                                lastOpenScene = item.path;

                                sceneMode = lastOpenScene.EndsWith($".{AssetSerialization.PrefabExtension}") ? SceneMode.Prefab : SceneMode.Scene;

                                UpdateLastSession();

                                ResetScenePhysics(true);

                                UpdateWindowTitle();
                            }

                            break;
                    }

                    break;

                case ProjectBrowserNodeType.Folder:

                    projectBrowser.currentContentNode = item;

                    projectBrowser.UpdateCurrentContentNodes(item.subnodes);

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

            ImGui.Text(progressMessage ?? "");
            ImGui.ProgressBar(progressFraction, new Vector2(250, 20));

            ImGui.EndPopup();

            lock(backgroundLock)
            {
                showingProgress = backgroundHandles.Count == 0 || backgroundHandles.Any(x => x.Completed == false);

                if (showingProgress == false)
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }

        wasShowingProgress = showingProgress;
    }

    private void MessageBoxPopup(ImGuiIOPtr io)
    {
        if (wasShowingMessageBox != showingMessageBox && showingMessageBox)
        {
            ImGui.OpenPopup("ShowingMessageBox");
        }

        if (showingMessageBox)
        {
            ImGui.SetNextWindowPos(new Vector2((io.DisplaySize.X - 300) / 2, (io.DisplaySize.Y - 200) / 2));
            ImGui.SetNextWindowSize(new Vector2(300, 200));

            ImGui.BeginPopupModal("ShowingMessageBox", ref showingMessageBox,
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoDocking |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove);

            ImGui.TextWrapped(messageBoxMessage);

            if(messageBoxYesTitle != null && messageBoxNoTitle != null)
            {
                if (ImGui.Button($"{messageBoxYesTitle ?? ""}##MESSAGE_BOX_YES"))
                {
                    showingMessageBox = false;

                    EditorGUI.ExecuteHandler(messageBoxYesAction, "Message Box Yes");
                }

                ImGui.SameLine();

                if (ImGui.Button($"{messageBoxNoTitle ?? ""}##MESSAGE_BOX_NO"))
                {
                    showingMessageBox = false;

                    EditorGUI.ExecuteHandler(messageBoxNoAction, "Message Box Yes");
                }
            }
            else
            {
                if(ImGui.Button($"{messageBoxYesTitle ?? ""}##MESSAGE_BOX_YES"))
                {
                    showingMessageBox = false;

                    EditorGUI.ExecuteHandler(messageBoxYesAction, "Message Box Yes");
                }
            }

            ImGui.EndPopup();
        }

        wasShowingMessageBox = showingMessageBox;
    }

    private void ZoomInEntity(Entity entity)
    {
        if(entity.TryGetComponent<Transform>(out var transform) == false)
        {
            return;
        }

        var renderables = entity.GetComponentsInChildren<Renderable>(true);

        if(renderables.Length == 0)
        {
            cameraTransform.Position = transform.Position;

            return;
        }

        var minMax = new List<Vector3>();

        for(var i = 0; i < renderables.Length; i++)
        {
            var renderable = renderables[i];

            if(renderable.enabled)
            {
                minMax.Add(renderable.bounds.min);
                minMax.Add(renderable.bounds.min);
            }
        }

        if(minMax.Count == 0)
        {
            return;
        }

        var aabb = AABB.CreateFromPoints(CollectionsMarshal.AsSpan(minMax));

        cameraTransform.Position = aabb.center + aabb.size * 2 * cameraTransform.Back;
    }
}
