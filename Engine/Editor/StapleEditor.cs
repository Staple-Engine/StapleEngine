using Bgfx;
using ImGuiNET;
using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;

[assembly: InternalsVisibleTo("StapleEditorApp")]

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        enum ViewportType
        {
            Scene,
            Game
        }

        [Serializable]
        class LastSessionInfo
        {
            public string lastOpenScene;
            public AppPlatform currentPlatform;
        }

        class EntityBody
        {
            public AABB bounds;
            public IBody3D body;
        }

        class GameAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver resolver;

            public GameAssemblyLoadContext(string path) : base(true)
            {
                resolver = new AssemblyDependencyResolver(path);
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);

                if (assemblyPath != null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                return null;
            }
        }

        internal static string StapleBasePath => Storage.StapleBasePath;

        internal const int ClearView = 0;
        internal const int SceneView = 254;

        internal delegate bool BackgroundTaskProgressCallback(ref float progress);

        private readonly List<Thread> backgroundThreads = new();
        private readonly object backgroundLock = new();

        private RenderWindow window;

        private ImGuiProxy imgui;

        private Entity selectedEntity;

        private ProjectBrowserNode selectedProjectNode;

        private object selectedProjectNodeData;

        private int activeBottomTab = 0;

        private string basePath;

        private string lastOpenScene;

        private RenderTarget gameRenderTarget;

        private readonly RenderSystem renderSystem = new();

        private const int TargetFramerate = 30;

        private Color32 clearColor = new Color(0, 0, 0, 0);

        private ViewportType viewportType = ViewportType.Scene;

        private readonly Camera camera = new();

        private readonly Transform cameraTransform = new();

        private readonly AppSettings editorSettings = AppSettings.Default;

        private AppSettings projectAppSettings;

        private readonly Dictionary<Entity, EntityBody> pickEntityBodies = new();

        private Material debugHighlightMaterial;

        private readonly Dictionary<string, Editor> cachedEditors = new();

        private readonly Editor defaultEditor = new();

        private GameAssemblyLoadContext gameAssemblyLoadContext;

        private WeakReference<Assembly> gameAssembly;

        private bool showingBuildWindow = false;

        private AppPlatform buildPlatform = AppPlatform.Windows;

        private AppPlatform currentPlatform = AppPlatform.Windows;

        private bool buildPlayerDebug = false;

        internal bool showingProgress = false;

        internal bool wasShowingProgress = false;

        internal float progressFraction = 0;

        private bool shouldTerminate = false;

        internal bool showingAssetPicker = false;

        internal Type assetPickerType;

        internal string assetPickerSearch = "";

        internal string assetPickerKey;

        internal bool wasShowingSpritePicker = false;

        internal bool showingSpritePicker = false;

        internal Texture spritePickerTexture;

        internal List<TextureSpriteInfo> spritePickerSprites;

        internal Action<int> spritePickerCallback;

        private PlayerSettings playerSettings;

        private readonly CSProjManager csProjManager = new();

        private readonly ProjectBrowser projectBrowser = new();

        private Dictionary<string, Type> registeredAssetTypes = new();

        public static WeakReference<StapleEditor> instance;

        public void Run()
        {
            instance = new WeakReference<StapleEditor>(this);

            ReloadTypeCache();

            editorSettings.runInBackground = true;
            editorSettings.appName = "Staple Editor";
            editorSettings.companyName = "Staple Engine";

            LayerMask.AllLayers = editorSettings.layers;
            LayerMask.AllSortingLayers = editorSettings.sortingLayers;

            Storage.Update(editorSettings.appName, editorSettings.companyName);

            Log.SetLog(new FSLog(Path.Combine(Storage.PersistentDataPath, "EditorLog.log")));

            Log.Instance.onLog += (type, message) =>
            {
                System.Console.WriteLine($"[{type}] {message}");
            };

            Log.Info($"Current Platform: {Platform.CurrentPlatform.Value}");

            currentPlatform = buildPlatform = Platform.CurrentPlatform.Value;

            if (ResourceManager.instance.LoadPak(Path.Combine(Storage.StapleBasePath, "DefaultResources", $"DefaultResources-{Platform.CurrentPlatform.Value}.pak")) == false)
            {
                Log.Error("Failed to load default resources pak");

                return;
            }

            playerSettings = PlayerSettings.Load(editorSettings);

            window = RenderWindow.Create(playerSettings.screenWidth, playerSettings.screenHeight, true, WindowMode.Windowed, editorSettings,
                playerSettings.monitorIndex, RenderSystem.ResetFlags(playerSettings.videoFlags));

            if(window == null)
            {
                return;
            }

            window.OnInit = () =>
            {
                Time.fixedDeltaTime = 1000.0f / TargetFramerate / 1000.0f;

                projectBrowser.LoadEditorTexture("FolderIcon", "Textures/open-folder.png");
                projectBrowser.LoadEditorTexture("FileIcon", "Textures/files.png");

                imgui = new ImGuiProxy();

                if (imgui.Initialize() == false)
                {
                    imgui.Destroy();

                    window.Cleanup();

                    window.shouldStop = true;

                    return;
                }

                var io = ImGui.GetIO();

                io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
                io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
                io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

                EditorGUI.io = io;
                EditorGUI.editor = this;

                var style = ImGui.GetStyle();

                style.Colors[(int)ImGuiCol.Text] = new Vector4(1, 1, 1, 1);
                style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.40f, 0.40f, 0.40f, 1.00f);
                style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
                style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
                style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
                style.Colors[(int)ImGuiCol.Border] = new Vector4(0.12f, 0.12f, 0.12f, 0.71f);
                style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
                style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.42f, 0.42f, 0.42f, 0.54f);
                style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.42f, 0.42f, 0.42f, 0.40f);
                style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.56f, 0.56f, 0.56f, 0.67f);
                style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.19f, 0.19f, 0.19f, 1.00f);
                style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
                style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.17f, 0.17f, 0.17f, 0.90f);
                style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.335f, 0.335f, 0.335f, 1.000f);
                style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.24f, 0.24f, 0.24f, 0.53f);
                style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.41f, 0.41f, 0.41f, 1.00f);
                style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.52f, 0.52f, 0.52f, 1.00f);
                style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.76f, 0.76f, 0.76f, 1.00f);
                style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.65f, 0.65f, 0.65f, 1.00f);
                style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.52f, 0.52f, 0.52f, 1.00f);
                style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.64f, 0.64f, 0.64f, 1.00f);
                style.Colors[(int)ImGuiCol.Button] = new Vector4(0.54f, 0.54f, 0.54f, 0.35f);
                style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.52f, 0.52f, 0.52f, 0.59f);
                style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.76f, 0.76f, 0.76f, 1.00f);
                style.Colors[(int)ImGuiCol.Header] = new Vector4(0.38f, 0.38f, 0.38f, 1.00f);
                style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.47f, 0.47f, 0.47f, 1.00f);
                style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.76f, 0.76f, 0.76f, 0.77f);
                style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.000f, 0.000f, 0.000f, 0.137f);
                style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.700f, 0.671f, 0.600f, 0.290f);
                style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.702f, 0.671f, 0.600f, 0.674f);
                style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.26f, 0.59f, 0.98f, 0.25f);
                style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
                style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
                style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
                style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
                style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
                style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
                style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.73f, 0.73f, 0.73f, 0.35f);
                style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);
                style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
                style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
                style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
                style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
                style.Colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(0.38f, 0.38f, 0.38f, 1.00f);
                style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
                style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.40f, 0.40f, 0.40f, 1.00f);
                style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.33f, 0.33f, 0.33f, 1.00f);
                style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
                style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.33f, 0.33f, 0.33f, 1.00f);
                style.Colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.85f, 0.85f, 0.85f, 0.28f);

                style.PopupRounding = 3;

                style.WindowPadding = style.ItemInnerSpacing = new Vector2(4, 4);
                style.FramePadding = new Vector2(6, 4);
                style.ItemSpacing = new Vector2(6, 2);

                style.ScrollbarSize = 18;

                style.WindowBorderSize = style.ChildBorderSize = style.PopupBorderSize = 1;
                style.FrameBorderSize = style.TabBorderSize = 2;

                style.WindowRounding = style.ChildRounding = style.FrameRounding = style.ScrollbarRounding = style.GrabRounding = style.TabRounding = 3;

                bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);
                bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.UIntValue, 1, 0);

                bgfx.set_view_rect_ratio(SceneView, 0, 0, bgfx.BackbufferRatio.Equal);
                bgfx.set_view_clear(SceneView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.UIntValue, 1, 0);

                Physics3D.Instance = new Physics3D(new JoltPhysics3D());

                debugHighlightMaterial = ResourceManager.instance.LoadMaterial("Materials/DebugHighlight.mat");

                LoadProject(Path.Combine(Environment.CurrentDirectory, "..", "Test Project"));

                renderSystem.Startup();
            };

            window.OnUpdate = () =>
            {
                var io = ImGui.GetIO();

                bgfx.touch(ClearView);

                if(window.width == 0 || window.height == 0)
                {
                    return;
                }

                if(viewportType == ViewportType.Scene)
                {
                    RenderScene();

                    var axis = Vector3.Zero;

                    if(Input.GetKey(KeyCode.A))
                    {
                        axis = cameraTransform.Left;
                    }

                    if(Input.GetKey(KeyCode.D))
                    {
                        axis = cameraTransform.Right;
                    }

                    if(Input.GetKey(KeyCode.W))
                    {
                        axis = cameraTransform.Forward;
                    }

                    if(Input.GetKey(KeyCode.S))
                    {
                        axis = cameraTransform.Back;
                    }

                    cameraTransform.LocalPosition += axis * 10 * Time.deltaTime;

                    if(Input.GetMouseButton(MouseButton.Right))
                    {
                        var rotation = Math.ToEulerAngles(cameraTransform.LocalRotation);

                        rotation.X -= Input.MouseRelativePosition.Y;
                        rotation.Y -= Input.MouseRelativePosition.X;

                        cameraTransform.LocalRotation = Math.FromEulerAngles(rotation);
                    }
                }

                io.DisplaySize = new Vector2(window.width, window.height);
                io.DisplayFramebufferScale = new Vector2(1, 1);

                if (gameRenderTarget != null && Scene.current != null)
                {
                    RenderTarget.SetActive(1, gameRenderTarget);

                    AppPlayer.ScreenWidth = gameRenderTarget.width;
                    AppPlayer.ScreenHeight = gameRenderTarget.height;

                    renderSystem.Update();

                    AppPlayer.ScreenWidth = window.width;
                    AppPlayer.ScreenHeight = window.height;
                }

                ThumbnailCache.OnFrameStart();
                imgui.BeginFrame();

                var viewport = ImGui.GetMainViewport();

                ImGui.SetNextWindowPos(viewport.Pos);
                ImGui.SetNextWindowSize(viewport.Size);
                ImGui.SetNextWindowViewport(viewport.ID);

                Dockspace();
                Viewport(io);
                Entities(io);
                Inspector(io);
                BottomPanel(io);
                AssetPicker(io);
                BuildWindow(io);
                ProgressPopup(io);
                SpritePicker(io);

                if (Scene.current?.world != null)
                {
                    var mouseRay = Camera.ScreenPointToRay(Input.MousePosition, Scene.current.world, Entity.Empty, camera, cameraTransform);

                    var hit = Physics.RayCast3D(mouseRay, out var body, out _, maxDistance: 10);

                    ImGui.Begin("Debug", ImGuiWindowFlags.NoDocking);

                    ImGui.Text($"Mouse Ray:");

                    ImGui.Text($"Position: {mouseRay.position.X}, {mouseRay.position.Y}, {mouseRay.position.Z}");

                    ImGui.Text($"Direction: {mouseRay.direction.X}, {mouseRay.direction.Y}, {mouseRay.direction.Z}");

                    ImGui.Checkbox("Hit", ref hit);

                    ImGui.Text($"RenderTarget size: {gameRenderTarget?.width ?? 0} {gameRenderTarget?.height ?? 0}");

                    ImGui.End();
                }

                imgui.EndFrame();
            };

            window.OnScreenSizeChange = (hasFocus) =>
            {
                var flags = RenderSystem.ResetFlags(playerSettings.videoFlags);

                AppPlayer.ScreenWidth = playerSettings.screenWidth = window.width;
                AppPlayer.ScreenHeight = playerSettings.screenHeight = window.height;

                PlayerSettings.Save(playerSettings);

                bgfx.reset((uint)window.width, (uint)window.height, (uint)flags, bgfx.TextureFormat.RGBA8);

                bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);
                bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.UIntValue, 1, 0);

                bgfx.set_view_rect_ratio(SceneView, 0, 0, bgfx.BackbufferRatio.Equal);
                bgfx.set_view_clear(SceneView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.UIntValue, 1, 0);
            };

            window.OnCleanup = () =>
            {
                lock(backgroundLock)
                {
                    shouldTerminate = true;
                }

                for(; ; )
                {
                    lock(backgroundLock)
                    {
                        if(backgroundThreads.Count == 0)
                        {
                            break;
                        }
                    }
                }

                imgui.Destroy();

                renderSystem.Shutdown();

                ResourceManager.instance.Destroy();
            };

            window.Run();
        }

        private LastSessionInfo GetLastSession()
        {
            var path = Path.Combine(basePath, "Cache", "LastSession.json");

            try
            {
                var text = File.ReadAllText(path);

                return JsonConvert.DeserializeObject<LastSessionInfo>(text);
            }
            catch(Exception)
            {
                return null;
            }
        }

        private void UpdateLastSession(LastSessionInfo info)
        {
            var path = Path.Combine(basePath, "Cache", "LastSession.json");

            try
            {
                var text = JsonConvert.SerializeObject(info, Formatting.Indented);

                File.WriteAllText(path, text);
            }
            catch(Exception)
            {
            }
        }

        private string CachePathResolver(string path)
        {
            var p = Path.Combine(basePath, "Cache", "Staging", currentPlatform.ToString(), path);

            try
            {
                if (File.Exists(p))
                {
                    return p;
                }
            }
            catch(Exception)
            {
            }

            p = Path.Combine(basePath, "Assets", path);

            try
            {
                if (File.Exists(p))
                {
                    return p;
                }
            }
            catch (Exception)
            {
            }

            return path;
        }
    }
}