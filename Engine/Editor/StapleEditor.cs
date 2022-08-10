using Bgfx;
using ImGuiNET;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
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

        class ProjectBrowserNode
        {
            public string name;
            public string path;
            public ProjectBrowserNodeType type;
            public string extension;
            public List<ProjectBrowserNode> subnodes = new List<ProjectBrowserNode>();
            public bool open = false;

            public string TypeString
            {
                get
                {
                    switch(extension)
                    {
                        case ".mat":

                            return "Material";

                        case ".stsh":

                            return "Shader";

                        case ".stsc":

                            return "Scene";

                        case ".png":
                        case ".jpg":
                        case ".jpeg":
                        case ".gif":
                        case ".bmp":

                            return "Texture";

                        default:

                            return "";
                    }
                }
            }
        }

        internal const int ClearView = 0;

        private RenderWindow window;

        private ImGuiProxy imgui;

        private ImGuiWindowFlags mainPanelFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus;

        private Entity selectedEntity;

        private int activeBottomTab = 0;

        private string basePath;

        private List<ProjectBrowserNode> projectBrowserNodes = new List<ProjectBrowserNode>();

        public void Run()
        {
            window = RenderWindow.Create(1024, 768, true, PlayerSettings.WindowMode.Windowed, new AppSettings()
            {
                appName = "Staple Editor",
            }, 0, bgfx.ResetFlags.Vsync, true);

            if(window == null)
            {
                return;
            }

            ResourceManager.instance.basePath = $"{Environment.CurrentDirectory}/Data";

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

            basePath = Environment.CurrentDirectory;

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

                ImGui.SameLine();

                if (ImGui.BeginPopup("ComponentListPopup"))
                {
                    ImGui.Text("Components");
                    ImGui.Separator();

                    ImGui.EndPopup();
                }

                ImGui.SameLine();
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
                            open = false,
                            path = directory,
                            type = ProjectBrowserNodeType.Folder,
                            subnodes = subnodes,
                        });
                    }

                    foreach(var file in files)
                    {
                        nodes.Add(new ProjectBrowserNode()
                        {
                            name = Path.GetFileNameWithoutExtension(file),
                            extension = Path.GetExtension(file),
                            path = file,
                            open = false,
                            subnodes = new List<ProjectBrowserNode>(),
                            type = ProjectBrowserNodeType.File
                        });
                    }
                }

                Recursive(basePath, projectBrowserNodes);
            }
        }
    }
}