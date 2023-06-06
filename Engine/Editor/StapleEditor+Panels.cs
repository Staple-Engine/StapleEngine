using ImGuiNET;
using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void Entities(ImGuiIOPtr io)
        {
            ImGui.SetNextWindowPos(new Vector2(0, 20));
            ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X / 6, io.DisplaySize.Y / 1.5f));

            ImGui.Begin("Entities", mainPanelFlags);

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
                        if (ImGui.IsItemClicked())
                        {
                            selectedEntity = transform.entity;
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

            if (sceneRenderTarget == null || sceneRenderTarget.width != width || sceneRenderTarget.height != height)
            {
                sceneRenderTarget?.Destroy();

                sceneRenderTarget = RenderTarget.Create(width, height);

                gameRenderTarget?.Destroy();

                gameRenderTarget = RenderTarget.Create(width, height);
            }

            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X / 6, 20));
            ImGui.SetNextWindowSize(new Vector2(width, height));

            ImGui.Begin("Viewport", mainPanelFlags);

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

            RenderTarget target = null;

            switch (viewportType)
            {
                case ViewportType.Scene:

                    target = sceneRenderTarget;

                    break;

                case ViewportType.Game:

                    target = gameRenderTarget;

                    break;
            }

            if (target != null)
            {
                var texture = target.GetTexture();

                if (texture != null)
                {
                    ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), new Vector2(width, height));
                }
            }

            ImGui.End();
        }

        public void Components(ImGuiIOPtr io)
        {
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X / 6 + (io.DisplaySize.X - io.DisplaySize.X / 3), 20));
            ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X / 6, io.DisplaySize.Y / 1.5f));

            ImGui.Begin("Inspector", mainPanelFlags);

            ImGui.BeginChildFrame(ImGui.GetID("Toolbar"), new Vector2(0, 0));

            if (selectedEntity != null && Scene.current != null && Scene.current.world.IsValidEntity(selectedEntity))
            {
                var name = Scene.current.world.GetEntityName(selectedEntity);

                if(ImGui.InputText("Name: ", ref name, 120))
                {
                    Scene.current.world.SetEntityName(selectedEntity, name);
                }

                var currentLayer = Scene.current.world.GetEntityLayer(selectedEntity);
                var layers = LayerMask.AllLayers;

                if (ImGui.BeginCombo("Layer: ", currentLayer < layers.Count ? layers[(int)currentLayer] : ""))
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
                        if(component is Transform transform)
                        {
                            var position = transform.LocalPosition;

                            if (ImGui.InputFloat3("Position", ref position))
                            {
                                transform.LocalPosition = position;
                            }

                            var rotation = Math.ToEulerAngles(transform.LocalRotation);

                            if (ImGui.InputFloat3("Rotation", ref rotation))
                            {
                                transform.LocalRotation = Math.FromEulerAngles(rotation);
                            }

                            var scale = transform.LocalScale;

                            if (ImGui.InputFloat3("Scale", ref scale))
                            {
                                transform.LocalScale = scale;
                            }
                        }
                        else
                        {
                            var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

                            foreach (var field in fields)
                            {
                                var type = field.FieldType;

                                if (type.IsEnum)
                                {
                                    var values = Enum.GetValues(type)
                                        .OfType<Enum>()
                                        .ToList();

                                    var value = (Enum)field.GetValue(component);

                                    var current = values.IndexOf(value);

                                    var valueStrings = values
                                        .Select(x => x.ToString())
                                        .ToList();

                                    if (ImGui.BeginCombo(field.Name, value.ToString()))
                                    {
                                        for (var j = 0; j < valueStrings.Count; j++)
                                        {
                                            bool selected = j == current;

                                            if (ImGui.Selectable(valueStrings[j], selected))
                                            {
                                                field.SetValue(component, values[j]);
                                            }
                                        }

                                        ImGui.EndCombo();
                                    }
                                }
                                else if (type == typeof(string))
                                {
                                    var value = (string)field.GetValue(component);

                                    if (ImGui.InputText(field.Name, ref value, uint.MaxValue))
                                    {
                                        field.SetValue(component, value);
                                    }
                                }
                                else if (type == typeof(Vector2))
                                {
                                    var value = (Vector2)field.GetValue(component);

                                    if (ImGui.InputFloat2(field.Name, ref value))
                                    {
                                        field.SetValue(component, value);
                                    }
                                }
                                else if (type == typeof(Vector3))
                                {
                                    var value = (Vector3)field.GetValue(component);

                                    if (ImGui.InputFloat3(field.Name, ref value))
                                    {
                                        field.SetValue(component, value);
                                    }
                                }
                                else if (type == typeof(Vector4))
                                {
                                    var value = (Vector4)field.GetValue(component);

                                    if (ImGui.InputFloat4(field.Name, ref value))
                                    {
                                        field.SetValue(component, value);
                                    }
                                }
                                else if (type == typeof(Quaternion))
                                {
                                    var quaternion = (Quaternion)field.GetValue(component);

                                    var value = Math.ToEulerAngles(quaternion);

                                    if (ImGui.InputFloat3(field.Name, ref value))
                                    {
                                        quaternion = Math.FromEulerAngles(value);

                                        field.SetValue(component, quaternion);
                                    }
                                }
                                else if (type == typeof(int))
                                {
                                    var value = (int)field.GetValue(component);

                                    if (ImGui.InputInt(field.Name, ref value))
                                    {
                                        field.SetValue(component, value);
                                    }
                                }
                                else if (type == typeof(bool))
                                {
                                    var value = (bool)field.GetValue(component);

                                    if (ImGui.Checkbox(field.Name, ref value))
                                    {
                                        field.SetValue(component, value);
                                    }
                                }
                                else if (type == typeof(float))
                                {
                                    var value = (float)field.GetValue(component);

                                    if (ImGui.InputFloat(field.Name, ref value))
                                    {
                                        field.SetValue(component, value);
                                    }
                                }
                                else if (type == typeof(double))
                                {
                                    var value = (double)field.GetValue(component);

                                    if (ImGui.InputDouble(field.Name, ref value))
                                    {
                                        field.SetValue(component, value);
                                    }
                                }
                                else if (type == typeof(byte))
                                {
                                    var current = (byte)field.GetValue(component);
                                    var value = (int)current;

                                    if (ImGui.InputInt(field.Name, ref value))
                                    {
                                        if (value < 0)
                                        {
                                            value = 0;
                                        }

                                        if (value > 255)
                                        {
                                            value = 255;
                                        }

                                        field.SetValue(component, (byte)value);
                                    }
                                }
                                else if (type == typeof(short))
                                {
                                    var current = (short)field.GetValue(component);
                                    var value = (int)current;

                                    if (ImGui.InputInt(field.Name, ref value))
                                    {
                                        if (value < short.MinValue)
                                        {
                                            value = short.MinValue;
                                        }

                                        if (value > short.MaxValue)
                                        {
                                            value = short.MaxValue;
                                        }

                                        field.SetValue(component, value);
                                    }
                                }
                                else if(type == typeof(Color) || type == typeof(Color32))
                                {
                                    Color c;

                                    if(type == typeof(Color))
                                    {
                                        c = (Color)field.GetValue(component);
                                    }
                                    else
                                    {
                                        c = (Color)((Color32)field.GetValue(component));
                                    }

                                    var colorComponents = new Vector4(c.r, c.g, c.b, c.a);

                                    if(ImGui.ColorPicker4(field.Name, ref colorComponents))
                                    {
                                        var newColor = new Color(colorComponents.X, colorComponents.Y, colorComponents.Z, colorComponents.W);

                                        if (type == typeof(Color))
                                        {
                                            field.SetValue(component, newColor);
                                        }
                                        else
                                        {
                                            var c2 = (Color32)newColor;

                                            field.SetValue(component, c2);
                                        }
                                    }
                                }
                            }
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
            ImGui.SetNextWindowPos(new Vector2(0, io.DisplaySize.Y / 1.5f + 20));
            ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X, io.DisplaySize.Y - (io.DisplaySize.Y / 1.5f + 20)));

            ImGui.Begin("BottomPanel", mainPanelFlags);

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
            ImGui.BeginChildFrame(ImGui.GetID("ProjectBrowser"), new Vector2(0, 0));

            void Recursive(ProjectBrowserNode node)
            {
                switch (node.type)
                {
                    case ProjectBrowserNodeType.File:

                        var typeString = node.TypeString;

                        if (typeString.Length != 0)
                        {
                            typeString = $"({typeString})";
                        }

                        if (ImGui.TreeNodeEx($"{node.name} {typeString}", ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen))
                        {
                            if (ImGui.IsItemClicked())
                            {
                                if (lastSelectedNode == node)
                                {
                                    switch (node.action)
                                    {
                                        case ProjectBrowserNodeAction.InspectScene:

                                            var scene = ResourceManager.instance.LoadRawSceneFromPath(node.path);

                                            if (scene != null)
                                            {
                                                lastOpenScene = node.path;
                                                Scene.current = scene;
                                            }

                                            break;
                                    }
                                }

                                lastSelectedNode = node;
                            }
                        }

                        break;

                    case ProjectBrowserNodeType.Folder:

                        var flags = ImGuiTreeNodeFlags.SpanFullWidth;

                        if (node.subnodes.Count == 0)
                        {
                            flags |= ImGuiTreeNodeFlags.NoTreePushOnOpen;
                        }

                        if (ImGui.TreeNodeEx(node.name, flags))
                        {
                            if (node.subnodes.Count > 0)
                            {
                                foreach (var subnode in node.subnodes)
                                {
                                    Recursive(subnode);
                                }

                                ImGui.TreePop();
                            }
                        }

                        break;
                }
            }

            foreach (var node in projectBrowserNodes)
            {
                Recursive(node);
            }

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

            ImGui.DockSpace(dockID, new Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode);

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
