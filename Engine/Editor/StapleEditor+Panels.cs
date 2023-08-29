using ImGuiNET;
using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void Entities(ImGuiIOPtr io)
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

                            cachedEditors.Clear();

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

        public void Viewport(ImGuiIOPtr io)
        {
            var width = (ushort)(io.DisplaySize.X * (2 / 3.0f));
            var height = (ushort)(io.DisplaySize.Y / 1.5f);

            if (gameRenderTarget == null || gameRenderTarget.width != width || gameRenderTarget.height != height)
            {
                gameRenderTarget?.Destroy();

                gameRenderTarget = RenderTarget.Create(width, height);
            }

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

            if (viewportType == ViewportType.Game && gameRenderTarget != null)
            {
                var texture = gameRenderTarget.GetTexture();

                if (texture != null)
                {
                    ImGui.BeginChildFrame(ImGui.GetID("GameView"), new Vector2(0, 0), ImGuiWindowFlags.NoBackground);
                    ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
                    ImGui.End();
                }
            }

            ImGui.End();
        }

        public void Components(ImGuiIOPtr io)
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

            ImGui.EndChildFrame();

            ImGui.End();
        }

        public void BottomPanel(ImGuiIOPtr io)
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

        public void ProjectBrowser(ImGuiIOPtr io)
        {
            ImGui.BeginChildFrame(ImGui.GetID("FolderTree"), new Vector2(150, 300));

            void Recursive(ProjectBrowserNode node)
            {
                if(node.type != ProjectBrowserNodeType.Folder)
                {
                    return;
                }

                var flags = ImGuiTreeNodeFlags.SpanFullWidth;
                var hasChildren = node.subnodes.Any(x => x.type == ProjectBrowserNodeType.Folder);

                if (hasChildren == false)
                {
                    flags |= ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Leaf;
                }

                if (ImGui.TreeNodeEx($"{node.name}##0", flags))
                {
                    if(hasChildren)
                    {
                        foreach (var subnode in node.subnodes)
                        {
                            Recursive(subnode);
                        }

                        ImGui.TreePop();
                    }

                    if(ImGui.IsItemClicked())
                    {
                        currentContentNode = node;

                        UpdateCurrentContentNodes(node.subnodes);
                    }
                }
            }

            if(ImGui.TreeNodeEx("Assets", ImGuiTreeNodeFlags.SpanFullWidth))
            {
                if (ImGui.IsItemClicked())
                {
                    currentContentNode = null;

                    UpdateCurrentContentNodes(projectBrowserNodes);
                }

                foreach (var node in projectBrowserNodes)
                {
                    Recursive(node);
                }

                ImGui.TreePop();
            }

            ImGui.EndChildFrame();

            ImGui.SameLine();

            ImGui.BeginChildFrame(ImGui.GetID("ProjectBrowser"), new Vector2(0, 0));

            ImGuiUtils.ContentGrid(currentContentBrowserNodes, contentPanelPadding, contentPanelThumbnailSize,
                null,
                (index, _) =>
                {
                    ProjectBrowserNode item = null;

                    if(currentContentNode == null)
                    {
                        currentContentNode = projectBrowserNodes[index];

                        item = currentContentNode;
                    }
                    else
                    {
                        item = index >= 0 && index < currentContentNode.subnodes.Count ? currentContentNode.subnodes[index] : null;
                    }

                    if (item == null)
                    {
                        return;
                    }

                    if(item.subnodes.Count == 0)
                    {
                        if(item.type == ProjectBrowserNodeType.File)
                        {
                            switch(item.action)
                            {
                                case ProjectBrowserNodeAction.InspectScene:

                                    var scene = ResourceManager.instance.LoadRawSceneFromPath(item.path);

                                    if (scene != null)
                                    {
                                        lastOpenScene = item.path;
                                        Scene.current = scene;

                                        ResetScenePhysics();
                                    }

                                    break;
                            }
                        }
                    }
                    else
                    {
                        currentContentNode = item;

                        UpdateCurrentContentNodes(item.subnodes);
                    }
                });

            ImGui.EndChildFrame();
        }

        public void Console(ImGuiIOPtr io)
        {
        }

        public void Dockspace()
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

                            var text = JsonConvert.SerializeObject(serializableScene.objects, Formatting.Indented);

                            try
                            {
                                File.WriteAllText(lastOpenScene, text);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

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
    }
}
