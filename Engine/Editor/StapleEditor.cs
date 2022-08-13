using Bgfx;
using ImGuiNET;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("StapleEditorApp")]

namespace Staple.Editor
{
    internal class StapleEditor
    {
        enum ProjectBrowserNodeType
        {
            File,
            Folder
        }

        enum ProjectBrowserNodeAction
        {
            None,
            InspectScene,
        }

        class ProjectBrowserNode
        {
            public string name;
            public string path;
            public ProjectBrowserNodeType type;
            public string extension;
            public List<ProjectBrowserNode> subnodes = new List<ProjectBrowserNode>();
            public ProjectBrowserNodeAction action = ProjectBrowserNodeAction.None;

            public string TypeString;
        }

        internal const int ClearView = 0;

        private RenderWindow window;

        private ImGuiProxy imgui;

        private ImGuiWindowFlags mainPanelFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus;

        private Entity selectedEntity;

        private int activeBottomTab = 0;

        private string basePath;

        private ProjectBrowserNode lastSelectedNode;

        private List<ProjectBrowserNode> projectBrowserNodes = new List<ProjectBrowserNode>();

        public void Run()
        {
            window = RenderWindow.Create(1024, 768, true, PlayerSettings.WindowMode.Windowed, new AppSettings()
            {
                runInBackground = true,
                appName = "Staple Editor",
            }, 0, bgfx.ResetFlags.Vsync);

            if(window == null)
            {
                return;
            }

            ResourceManager.instance.resourcePaths.Add($"{Environment.CurrentDirectory}/Data");

            imgui = new ImGuiProxy();

            if(imgui.Initialize() == false)
            {
                window.Cleanup();

                return;
            }

            var io = ImGui.GetIO();

            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            var style = ImGui.GetStyle();

            style.PopupRounding = style.ChildRounding = style.FrameRounding = style.ScrollbarRounding = style.GrabRounding = style.TabRounding = 3;

            style.WindowPadding = new Vector2(4, 4);
            style.FramePadding = new Vector2(6, 4);
            style.ItemSpacing = new Vector2(6, 2);

            style.ScrollbarSize = 18;

            style.WindowBorderSize = style.ChildBorderSize = style.PopupBorderSize = style.FrameBorderSize = style.TabBorderSize = 1;

            style.WindowRounding = 0;

            basePath = Path.Combine(Environment.CurrentDirectory, "..", "Test Project");

            UpdateProjectBrowserNodes();

            window.OnUpdate = () =>
            {
                bgfx.touch(ClearView);

                io.DisplaySize = new Vector2(window.screenWidth, window.screenHeight);
                io.DisplayFramebufferScale = new Vector2(1, 1);

                imgui.BeginFrame();

                var viewport = ImGui.GetMainViewport();

                ImGui.SetNextWindowPos(viewport.Pos);
                ImGui.SetNextWindowSize(viewport.Size);
                ImGui.SetNextWindowViewport(viewport.ID);

                Dockspace();
                Viewport(io);
                Entities(io);
                Components(io);
                BottomPanel(io);

                imgui.EndFrame();
            };

            window.OnScreenSizeChange = (hasFocus) =>
            {
                var flags = AppPlayer.ResetFlags(PlayerSettings.VideoFlags.Vsync);

                bgfx.reset((uint)window.screenWidth, (uint)window.screenHeight, (uint)flags, bgfx.TextureFormat.RGBA8);
                bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);
                bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0x334455FF, 0, 0);
            };

            window.Run();

            imgui.Destroy();

            ResourceManager.instance.Destroy();

            window.Cleanup();
        }

        public void Entities(ImGuiIOPtr io)
        {
            ImGui.SetNextWindowPos(new Vector2(0, 20));
            ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X / 6, io.DisplaySize.Y / 1.5f));

            ImGui.Begin("Entities", mainPanelFlags);

            ImGui.BeginChildFrame(ImGui.GetID("EntityFrame"), new Vector2(0, 0));

            if(Scene.current != null)
            {
                void Recursive(Entity entity)
                {
                    var flags = ImGuiTreeNodeFlags.SpanFullWidth;

                    if(entity.Transform.ChildCount == 0)
                    {
                        flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                    }

                    if(ImGui.TreeNodeEx($"{entity.Name}##0", flags))
                    {
                        if(ImGui.IsItemClicked())
                        {
                            selectedEntity = entity;
                        }

                        foreach(var child in entity.Transform)
                        {
                            if(child.entity.TryGetTarget(out var childEntity))
                            {
                                Recursive(childEntity);
                            }
                        }

                        if(entity.Transform.ChildCount > 0)
                        {
                            ImGui.TreePop();
                        }
                    }
                }

                foreach(var entity in Scene.current.entities)
                {
                    if(entity.Transform.parent == null)
                    {
                        Recursive(entity);
                    }
                }
            }

            ImGui.EndChildFrame();

            ImGui.End();
        }

        public void Viewport(ImGuiIOPtr io)
        {
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X / 6, 20));
            ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X - io.DisplaySize.X / 3, io.DisplaySize.Y / 1.5f));

            ImGui.Begin("Scene", mainPanelFlags);

            ImGui.BeginChildFrame(ImGui.GetID("SceneFrame"), new Vector2(0, 0), ImGuiWindowFlags.NoScrollbar);

            ImGui.EndChildFrame();

            ImGui.End();
        }

        public void Components(ImGuiIOPtr io)
        {
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X / 6 + (io.DisplaySize.X - io.DisplaySize.X / 3), 20));
            ImGui.SetNextWindowSize(new Vector2(io.DisplaySize.X / 6, io.DisplaySize.Y / 1.5f));

            ImGui.Begin("Inspector", mainPanelFlags);

            ImGui.BeginChildFrame(ImGui.GetID("Toolbar"), new Vector2(0, 0));

            if(selectedEntity != null)
            {
                ImGui.Button("Add Component");

                if(ImGui.TreeNodeEx("Transform", ImGuiTreeNodeFlags.SpanFullWidth))
                {
                    var position = selectedEntity.Transform.LocalPosition;

                    if(ImGui.InputFloat3("Position", ref position))
                    {
                        selectedEntity.Transform.LocalPosition = position;
                    }

                    var rotation = selectedEntity.Transform.LocalRotation.ToEulerAngles();

                    if (ImGui.InputFloat3("Rotation", ref rotation))
                    {
                        selectedEntity.Transform.LocalRotation = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z);
                    }

                    var scale = selectedEntity.Transform.LocalScale;

                    if (ImGui.InputFloat3("Scale", ref scale))
                    {
                        selectedEntity.Transform.LocalScale = scale;
                    }

                    ImGui.TreePop();
                }

                for(var i = 0; i < selectedEntity.components.Count; i++)
                {
                    var component = selectedEntity.components[i];

                    if (ImGui.TreeNodeEx(component.GetType().Name + $"##{i}", ImGuiTreeNodeFlags.SpanFullWidth))
                    {
                        var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

                        foreach(var field in fields)
                        {
                            var type = field.FieldType;

                            if(type.IsEnum)
                            {
                                var values = Enum.GetValues(type)
                                    .OfType<Enum>()
                                    .ToList();

                                var value = (Enum)field.GetValue(component);

                                var current = values.IndexOf(value);

                                var valueStrings = values
                                    .Select(x => x.ToString())
                                    .ToList();

                                if(ImGui.BeginCombo(field.Name, value.ToString()))
                                {
                                    for(var j = 0; j < valueStrings.Count; j++)
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
                            else if(type == typeof(string))
                            {
                                var value = (string)field.GetValue(component);

                                if(ImGui.InputText(field.Name, ref value, uint.MaxValue))
                                {
                                    field.SetValue(component, value);
                                }
                            }
                            else if(type == typeof(Vector2))
                            {
                                var value = (Vector2)field.GetValue(component);

                                if (ImGui.InputFloat2(field.Name, ref value))
                                {
                                    field.SetValue(component, value);
                                }
                            }
                            else if(type == typeof(Vector3))
                            {
                                var value = (Vector3)field.GetValue(component);

                                if (ImGui.InputFloat3(field.Name, ref value))
                                {
                                    field.SetValue(component, value);
                                }
                            }
                            else if(type == typeof(Vector4))
                            {
                                var value = (Vector4)field.GetValue(component);

                                if (ImGui.InputFloat4(field.Name, ref value))
                                {
                                    field.SetValue(component, value);
                                }
                            }
                            else if(type == typeof(Quaternion))
                            {
                                var quaternion = (Quaternion)field.GetValue(component);

                                var value = quaternion.ToEulerAngles();

                                if (ImGui.InputFloat3(field.Name, ref value))
                                {
                                    quaternion = Quaternion.CreateFromYawPitchRoll(value.X, value.Y, value.Z);

                                    field.SetValue(component, quaternion);
                                }
                            }
                            else if(type == typeof(int))
                            {
                                var value = (int)field.GetValue(component);

                                if (ImGui.InputInt(field.Name, ref value))
                                {
                                    field.SetValue(component, value);
                                }
                            }
                            else if(type == typeof(bool))
                            {
                                var value = (bool)field.GetValue(component);

                                if (ImGui.Checkbox(field.Name, ref value))
                                {
                                    field.SetValue(component, value);
                                }
                            }
                            else if(type == typeof(float))
                            {
                                var value = (float)field.GetValue(component);

                                if (ImGui.InputFloat(field.Name, ref value))
                                {
                                    field.SetValue(component, value);
                                }
                            }
                            else if(type == typeof(double))
                            {
                                var value = (double)field.GetValue(component);

                                if (ImGui.InputDouble(field.Name, ref value))
                                {
                                    field.SetValue(component, value);
                                }
                            }
                            else if(type == typeof(byte))
                            {
                                var current = (byte)field.GetValue(component);
                                var value = (int)current;

                                if (ImGui.InputInt(field.Name, ref value))
                                {
                                    if(value < 0)
                                    {
                                        value = 0;
                                    }

                                    if(value > 255)
                                    {
                                        value = 255;
                                    }

                                    field.SetValue(component, (byte)value);
                                }
                            }
                            else if(type == typeof(short))
                            {
                                var current = (short)field.GetValue(component);
                                var value = (int)current;

                                if (ImGui.InputInt(field.Name, ref value))
                                {
                                    if(value < short.MinValue)
                                    {
                                        value = short.MinValue;
                                    }

                                    if(value > short.MaxValue)
                                    {
                                        value = short.MaxValue;
                                    }

                                    field.SetValue(component, value);
                                }
                            }
                        }

                        ImGui.TreePop();
                    }
                }
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

            if(ImGui.BeginTabBar("BottomTabBar"))
            {
                if(ImGui.TabItemButton("Project"))
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
                switch(node.type)
                {
                    case ProjectBrowserNodeType.File:

                        var typeString = node.TypeString;

                        if(typeString.Length != 0)
                        {
                            typeString = $"({typeString})";
                        }

                        if (ImGui.TreeNodeEx($"{node.name} {typeString}", ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen))
                        {
                            if(ImGui.IsItemClicked())
                            {
                                if (lastSelectedNode == node)
                                {
                                    switch (node.action)
                                    {
                                        case ProjectBrowserNodeAction.InspectScene:

                                            var scene = ResourceManager.instance.LoadRawSceneFromPath(node.path);

                                            if (scene != null)
                                            {
                                                Scene.current = scene;
                                            }

                                            break;
                                    }
                                }
                            }

                            lastSelectedNode = node;
                        }

                        break;

                    case ProjectBrowserNodeType.Folder:

                        var flags = ImGuiTreeNodeFlags.SpanFullWidth;

                        if(node.subnodes.Count == 0)
                        {
                            flags |= ImGuiTreeNodeFlags.NoTreePushOnOpen;
                        }

                        if (ImGui.TreeNodeEx(node.name, flags))
                        {
                            if(node.subnodes.Count > 0)
                            {
                                foreach(var subnode in node.subnodes)
                                {
                                    Recursive(subnode);
                                }

                                ImGui.TreePop();
                            }
                        }

                        break;
                }
            }

            foreach(var node in projectBrowserNodes)
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

        public void UpdateProjectBrowserNodes()
        {
            if(basePath == null)
            {
                projectBrowserNodes = new List<ProjectBrowserNode>();
            }
            else
            {
                projectBrowserNodes = new List<ProjectBrowserNode>();

                void Recursive(string p, List<ProjectBrowserNode> nodes)
                {
                    string[] directories = new string[0];
                    string[] files = new string[0];

                    try
                    {
                        directories = Directory.GetDirectories(p);
                    }
                    catch(Exception)
                    {
                    }

                    try
                    {
                        files = Directory.GetFiles(p);
                    }
                    catch (Exception)
                    {
                    }

                    foreach(var directory in directories)
                    {
                        var subnodes = new List<ProjectBrowserNode>();

                        Recursive(directory, subnodes);

                        nodes.Add(new ProjectBrowserNode()
                        {
                            name = Path.GetFileName(directory),
                            extension = "",
                            path = directory,
                            type = ProjectBrowserNodeType.Folder,
                            subnodes = subnodes,
                        });
                    }

                    foreach(var file in files)
                    {
                        var node = new ProjectBrowserNode()
                        {
                            name = Path.GetFileNameWithoutExtension(file),
                            extension = Path.GetExtension(file),
                            path = file,
                            subnodes = new List<ProjectBrowserNode>(),
                            type = ProjectBrowserNodeType.File
                        };

                        nodes.Add(node);

                        switch (node.extension)
                        {
                            case ".mat":

                                node.TypeString = "Material";

                                break;

                            case ".stsh":

                                node.TypeString = "Shader";

                                break;

                            case ".stsc":

                                node.TypeString = "Scene";
                                node.action = ProjectBrowserNodeAction.InspectScene;

                                break;

                            case ".png":
                            case ".jpg":
                            case ".jpeg":
                            case ".gif":
                            case ".bmp":

                                node.TypeString = "Texture";

                                break;

                            default:

                                node.TypeString = "";

                                break;
                        }
                    }
                }

                Recursive(basePath, projectBrowserNodes);
            }
        }
    }
}