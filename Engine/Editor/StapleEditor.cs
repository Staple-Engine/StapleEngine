using Bgfx;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Newtonsoft.Json;
using Staple.Internal;
using Staple.Jobs;
using Staple.JoltPhysics;
using Staple.OpenALAudio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

[assembly: InternalsVisibleTo("StapleEditorApp")]

namespace Staple.Editor;

internal partial class StapleEditor
{
    public static readonly string StapleVersion = $"{Platform.StapleVersionMajor}.{Platform.StapleVersionMinor}";

    internal const string RenderTargetLayerName = "STAPLE_EDITOR_RENDER_TARGET_LAYER";

    #region Classes
    enum ViewportType
    {
        Scene,
        Game
    }

    internal class DragDropPayload
    {
        public int index;
        public ImGuiUtils.ContentGridItem item;
        public Action<int, ImGuiUtils.ContentGridItem> action;
    }

    [Serializable]
    class ProjectInfo
    {
        public string stapleVersion;
    }

    [Serializable]
    class LastSessionInfo
    {
        public string lastOpenScene;
        public AppPlatform currentPlatform;
        public bool debugBuild = false;
        public bool nativeBuild = false;
        public bool debugRedists = false;

        public Dictionary<AppPlatform, string> lastPickedBuildDirectories = new();
    }

    [Serializable]
    class LastProjectItem
    {
        public string name;
        public string path;
        public DateTime date;
    }

    [Serializable]
    class LastProjectInfo
    {
        public string lastOpenProject = "";
        public List<LastProjectItem> items = new();
    }

    class EntityBody
    {
        public AABB bounds;
        public IBody3D body;
    }

    class MenuItemInfo
    {
        public string name;

        public Action onClick;

        public List<MenuItemInfo> children = [];
    }

    public class StapleAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver resolver;

        private Func<(string[], string[])> assemblyPathsCallback;

        public StapleAssemblyLoadContext(string path, Func<(string[], string[])> assemblyPathsCallback) : base(true)
        {
            this.assemblyPathsCallback = assemblyPathsCallback;

            resolver = new AssemblyDependencyResolver(path);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);

            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            var (paths, validAssemblies) = assemblyPathsCallback();

            if(Array.IndexOf(validAssemblies, assemblyName.Name) < 0)
            {
                return null;
            }

            foreach(var path in paths)
            {
                try
                {
                    var p = Path.Combine(path, $"{assemblyName.Name}.dll");

                    if (File.Exists(p))
                    {
                        return LoadFromAssemblyPath(p);
                    }
                }
                catch(Exception)
                {
                }
            }

            return null;
        }
    }

    public class ModuleLoadInfo
    {
        public StapleAssemblyLoadContext contextLoader;
        public ModuleInitializer module;
        public string moduleName;
    }
    #endregion

    internal static string StapleBasePath => Storage.StapleBasePath;

    private static Color PrefabColor = new Color32("#00CED1");

    internal const int ClearView = 0;
    internal const int MeshRenderView = 252;
    internal const int SceneView = 253;
    internal const int WireframeView = 254;

    #region Background Tasks
    private readonly Lock backgroundLock = new();

    private readonly List<JobHandle> backgroundHandles = [];

    internal bool showingProgress = false;

    internal bool wasShowingProgress = false;

    internal float progressFraction = 0;

    internal string progressMessage = "";
    #endregion

    #region Rendering
    private RenderWindow window;

    private ImGuiProxy imgui;

    private readonly FrustumCuller frustumCuller = new();

    private int activeBottomTab = 0;

    private RenderTarget gameRenderTarget;

    private const int TargetFramerate = 30;

    private Color32 clearColor = new("#7393B3");

    private ViewportType viewportType = ViewportType.Scene;

    private readonly Camera camera = new();

    private readonly Transform cameraTransform = new();

    internal Material wireframeMaterial;

    internal Mesh wireframeMesh;

    private PlayerSettings playerSettings;

    private Material componentIconMaterial;

    public bool mouseIsHoveringImGui = false;

    private bool hadFocus = true;

    private bool transforming = false;

    private Vector3 transformPosition;

    private Vector3 transformScale;

    private Quaternion transformRotation;

    private Mesh gridMesh;
    #endregion

    #region Entities
    private Entity selectedEntity;

    private bool resetSelection = false;

    internal Entity draggedEntity;

    internal Entity dropTargetEntity;
    #endregion

    #region Project
    private ProjectBrowserNode selectedProjectNode;

    private object selectedProjectNodeData;

    private string basePath;

    private string lastOpenScene;

    internal Dictionary<AppPlatform, string> lastPickedBuildDirectories = [];

    private readonly AppSettings editorSettings = AppSettings.Default;

    private AppSettings projectAppSettings;

    private readonly ProjectBrowser projectBrowser = new();

    private LastProjectInfo lastProjects = new();

    internal Dictionary<string, DragDropPayload> dragDropPayloads = new();
    #endregion

    #region Editor
    private readonly Dictionary<Entity, EntityBody> pickEntityBodies = [];

    private readonly Dictionary<string, Editor> cachedEditors = [];

    private readonly Dictionary<int, GizmoEditor> cachedGizmoEditors = [];

    private readonly Editor defaultEditor = new();

    private readonly Dictionary<string, byte[]> registeredAssetTemplates = [];

    private readonly Dictionary<string, Type> registeredAssetTypes = [];

    private List<IEntityTemplate> registeredEntityTemplates = [];

    private List<Type> registeredComponents = [];

    internal List<EditorWindow> editorWindows = [];

    private List<MenuItemInfo> menuItems = [];

    private Dictionary<Entity, Texture> componentIcons = [];

    private ImGuizmoMode transformMode = ImGuizmoMode.Local;

    private ImGuizmoOperation transformOperation = ImGuizmoOperation.Translate;

    internal readonly UndoStack undoStack = new();
    #endregion

    #region Game
    private StapleAssemblyLoadContext gameAssemblyLoadContext;

    private WeakReference<Assembly> gameAssembly;
    #endregion

    #region Build
    internal string buildBackend;

    internal AppPlatform currentPlatform = AppPlatform.Windows;

    internal bool buildPlayerDebug = false;

    internal bool buildPlayerNativeAOT = false;

    internal bool buildPlayerDebugRedists = false;

    private readonly CSProjManager csProjManager = new();

    private bool needsGameRecompile = false;

    private bool needsRefreshStaging = false;

    private bool gameLoadDisabled = false;

    private bool buildingGame = false;

    private FileSystemWatcher fileSystemWatcher;

    private bool wasShowingMessageBox = false;

    private bool showingMessageBox = false;

    private string messageBoxMessage;

    private string messageBoxYesTitle;

    private string messageBoxNoTitle;

    private Action messageBoxYesAction;

    private Action messageBoxNoAction;

    internal Dictionary<ModuleType, List<ModuleLoadInfo>> modulesList = [];

    private bool refreshingAssets = false;
    #endregion

    private static WeakReference<StapleEditor> privInstance;

    public static StapleEditor instance => privInstance.TryGetTarget(out var target) ? target : null;

    public void Run()
    {
        privInstance = new WeakReference<StapleEditor>(this);

        LoadModules();

        ReloadTypeCache();

        Platform.IsPlaying = false;
        Platform.IsEditor = true;

        editorSettings.runInBackground = true;
        editorSettings.appName = "Staple Editor";
        editorSettings.companyName = "Staple Engine";

        LayerMask.SetLayers(CollectionsMarshal.AsSpan(editorSettings.layers), CollectionsMarshal.AsSpan(editorSettings.sortingLayers));

        AppSettings.Current = editorSettings;

        AssetDatabase.assetPathResolver = CachePathResolver;

        Storage.Update(editorSettings.appName, editorSettings.companyName);

        Log.SetLog(new FSLog(Path.Combine(Storage.PersistentDataPath, "EditorLog.log")));

        Log.Instance.onLog += (type, message) =>
        {
            System.Console.WriteLine($"[{type}] {message}");
        };

        PlayerBackendManager.Instance.Initialize();

        Log.Info($"Current Platform: {Platform.CurrentPlatform.Value}");

        currentPlatform = Platform.CurrentPlatform.Value;

        buildBackend = PlayerBackendManager.Instance.GetBackend(currentPlatform).name;

        if (ResourceManager.instance.LoadPak(Path.Combine(Storage.StapleBasePath, "DefaultResources", $"DefaultResources-{Platform.CurrentPlatform.Value}.pak")) == false)
        {
            Log.Error("Failed to load default resources pak");

            return;
        }

        AudioSystem.AudioListenerImpl = typeof(OpenALAudioListener);
        AudioSystem.AudioSourceImpl = typeof(OpenALAudioSource);
        AudioSystem.AudioClipImpl = typeof(OpenALAudioClip);
        AudioSystem.AudioDeviceImpl = typeof(OpenALAudioDevice);

        SubsystemManager.instance.RegisterSubsystem(AudioSystem.Instance, AudioSystem.Priority);

        ReloadAssetTemplates();

        playerSettings = PlayerSettings.Load(editorSettings);

        if (playerSettings.screenWidth <= 0 || playerSettings.screenHeight <= 0 || playerSettings.windowPosition.X < -1000 || playerSettings.windowPosition.Y < -1000)
        {
            playerSettings.screenWidth = editorSettings.defaultWindowWidth;
            playerSettings.screenHeight = editorSettings.defaultWindowHeight;

            playerSettings.windowPosition = Vector2Int.Zero;
        }

        window = RenderWindow.Create(playerSettings.screenWidth, playerSettings.screenHeight, true, WindowMode.Windowed,
            playerSettings.windowPosition != Vector2Int.Zero ? playerSettings.windowPosition : null,
            playerSettings.maximized, playerSettings.monitorIndex, RenderSystem.ResetFlags(playerSettings.videoFlags));

        if(window == null)
        {
            return;
        }

        window.OnInit = () =>
        {
            AssetDatabase.Reload();

            Time.fixedDeltaTime = 1000.0f / TargetFramerate / 1000.0f;

            projectBrowser.LoadEditorTextures();

            var iconPath = Path.Combine(EditorUtils.EditorPath.Value, "Editor Resources", "Icon.png");

            ThumbnailCache.GetTexture(iconPath, force: true);

            if(ThumbnailCache.TryGetTextureData(iconPath, out var icon))
            {
                window.window.SetIcon(icon);
            }

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

            SetupImGuiStyle();

            var style = ImGui.GetStyle();

            style.WindowPadding = Vector2.Zero;

            bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);
            bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.UIntValue, 1, 0);

            bgfx.set_view_rect_ratio(SceneView, 0, 0, bgfx.BackbufferRatio.Equal);
            bgfx.set_view_clear(SceneView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.UIntValue, 1, 0);

            bgfx.set_view_rect_ratio(WireframeView, 0, 0, bgfx.BackbufferRatio.Equal);
            bgfx.set_view_clear(WireframeView, (ushort)bgfx.ClearFlags.Depth, 0, 1, 0);

            Physics3D.Instance = new Physics3D(new JoltPhysics3D());

            Physics3D.Instance.Startup();

            wireframeMaterial = SpriteRenderSystem.DefaultMaterial.Value;

            wireframeMesh = new Mesh(true, true)
            {
                Vertices =
                [
                    new Vector3(-0.5f, 0.5f, 0.5f),
                    Vector3.One * 0.5f,
                    new Vector3(-0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, -0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f),
                    Vector3.One * -0.5f,
                    new Vector3(0.5f, -0.5f, -0.5f),
                ],

                Indices =
                [
                    0, 1, 2,
                    3, 7, 1,
                    5, 0, 4,
                    2, 6, 7,
                    4, 5
                ],

                MeshTopology = MeshTopology.LineStrip,
            };

            wireframeMesh.Guid.Guid = "WIREFRAME_MESH";

            ResourceManager.instance.LockAsset(wireframeMesh.Guid.Guid);

            RenderSystem.Instance.Startup();

            World.AddChangeReceiver(RenderSystem.Instance);

            var canvasSystem = RenderSystem.Instance.Get<UICanvasSystem>();

            if(canvasSystem != null)
            {
                var vertexLayout = new VertexLayoutBuilder()
                    .Add(VertexAttribute.Position, 3, VertexAttributeType.Float)
                    .Build();

                canvasSystem.observer = (p, s, e) =>
                {
                    p.X++;
                    p.Y++;

                    var vertices = new Vector3[]
                    {
                        new(p.X, p.Y, 0),
                        new(p.X, p.Y + s.Y, 0),
                        new(p.X + s.X, p.Y + s.Y, 0),
                        new(p.X + s.X, p.Y, 0),
                    };

                    Graphics.RenderSimple(vertices.AsSpan(), vertexLayout, [0, 1, 2, 2, 3, 0], SpriteRenderSystem.DefaultMaterial.Value,
                        Vector3.Zero, Matrix4x4.Identity, MeshTopology.LineStrip, MaterialLighting.Unlit, UICanvasSystem.UIViewID);
                };
            }

            cameraTransform.Position = new Vector3(0, 0, 5);

            try
            {
                var json = File.ReadAllText(Path.Combine(Storage.PersistentDataPath, "ProjectList.json"));

                lastProjects = JsonConvert.DeserializeObject<LastProjectInfo>(json);
            }
            catch(Exception)
            {
            }

            lastProjects ??= new();

            //Might re-enable this later
            /*
            if((lastProjects.lastOpenProject?.Length ?? 0) > 0)
            {
                LoadProject(lastProjects.lastOpenProject);
            }
            */
        };

        window.OnUpdate = () =>
        {
            var io = ImGui.GetIO();

            bgfx.touch(ClearView);

            if(window.width == 0 || window.height == 0)
            {
                return;
            }

            io.DisplaySize = new Vector2(window.width, window.height);
            io.DisplayFramebufferScale = new Vector2(1, 1);

            ThumbnailCache.OnFrameStart();
            EditorGUI.OnFrameStart();
            imgui.BeginFrame();

            if (viewportType == ViewportType.Scene)
            {
                var axis = Vector3.Zero;

                if(Input.GetKey(KeyCode.A))
                {
                    axis += cameraTransform.Left;
                }

                if(Input.GetKey(KeyCode.D))
                {
                    axis += cameraTransform.Right;
                }

                if(Input.GetKey(KeyCode.W))
                {
                    axis += cameraTransform.Forward;
                }

                if(Input.GetKey(KeyCode.S))
                {
                    axis += cameraTransform.Back;
                }

                cameraTransform.LocalPosition += axis * 10 * Time.unscaledDeltaTime;

                if(Input.GetMouseButton(MouseButton.Right))
                {
                    var rotation = cameraTransform.LocalRotation.ToEulerAngles();

                    rotation.X -= Input.MouseRelativePosition.Y;
                    rotation.Y -= Input.MouseRelativePosition.X;

                    cameraTransform.LocalRotation = Math.FromEulerAngles(rotation);
                }
            }

            if (gameRenderTarget != null && Scene.current != null)
            {
                RenderTarget.SetActive(1, gameRenderTarget);
                RenderTarget.SetActive(UICanvasSystem.UIViewID, gameRenderTarget);

                Screen.Width = gameRenderTarget.width;
                Screen.Height = gameRenderTarget.height;

                RenderSystem.Instance.Update();

                Screen.Width = window.width;
                Screen.Height = window.height;
            }

            if(resetSelection)
            {
                resetSelection = false;

                SetSelectedEntity(selectedEntity);
            }

            if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.Z))
            {
                undoStack.Undo();
            }

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.Y))
            {
                undoStack.Redo();
            }

            mouseIsHoveringImGui = true;

            var viewport = ImGui.GetMainViewport();

            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);

            if (projectAppSettings == null)
            {
                ImGui.Begin("ProjectListContainer", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoDecoration);

                ImGui.SetNextWindowSize(new Vector2(800, 400));
                ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X * 0.5f, io.DisplaySize.Y * 0.5f), ImGuiCond.Always, Vector2.One * 0.5f);

                ImGui.Begin("ProjectListContent", ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoDocking |
                    ImGuiWindowFlags.NoDecoration);

                ImGui.Text("Project List");

                ImGui.Spacing();

                if (ImGui.Button("New"))
                {
                    ImGuiNewProject();
                }

                ImGui.SameLine();

                if (ImGui.Button("Open"))
                {
                    ImGuiOpenProject();
                }

                ImGui.Spacing();

                if(ImGui.BeginTable("##ProjectsList", 3))
                {
                    var items = lastProjects.items.OrderByDescending(x => x.date).ToArray();

                    for (var i = 0; i < items.Length; i++)
                    {
                        var item = items[i];

                        try
                        {
                            if (Directory.Exists(item.path) == false)
                            {
                                continue;
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        ImGui.TableNextRow();

                        ImGui.TableSetColumnIndex(0);

                        var selected = false;

                        ImGui.Selectable($"{item.name}##PL{i}0");

                        selected |= ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);

                        ImGui.TableSetColumnIndex(1);

                        ImGui.Selectable(item.path);

                        selected |= ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);

                        ImGui.TableSetColumnIndex(2);

                        ImGui.Selectable(item.date.ToShortDateString());

                        selected |= ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);

                        if (selected)
                        {
                            LoadProject(item.path);

                            break;
                        }
                    }

                    ImGui.EndTable();
                }

                ImGui.End();

                ImGui.End();
            }
            else
            {
                ImGui.SetNextWindowViewport(viewport.ID);

                Dockspace();
                Viewport(io);
                Entities(io);
                Inspector(io);
                BottomPanel(io);
            }

            var currentWindows = new List<EditorWindow>(editorWindows);

            for(var i = 0; i < currentWindows.Count; i++)
            {
                var window = currentWindows[i];
                var shouldShow = false;

                var flags = ImGuiWindowFlags.None;

                if(window.windowFlags.HasFlag(EditorWindowFlags.HasMenuBar))
                {
                    flags |= ImGuiWindowFlags.MenuBar;
                }

                if(window.windowFlags.HasFlag(EditorWindowFlags.Dockable) == false)
                {
                    flags |= ImGuiWindowFlags.NoDocking;
                }

                if(window.windowFlags.HasFlag(EditorWindowFlags.Resizable) == false)
                {
                    flags |= ImGuiWindowFlags.NoResize;

                    ImGui.SetNextWindowSize(new Vector2(window.size.X, window.size.Y));

                    if(window.windowFlags.HasFlag(EditorWindowFlags.Centered))
                    {
                        ImGui.SetNextWindowPos(new Vector2((io.DisplaySize.X - window.size.X) / 2, (io.DisplaySize.Y - window.size.Y) / 2));
                    }
                }

                var isOpen = true;

                switch (window.windowType)
                {
                    case EditorWindowType.Modal:
                    case EditorWindowType.Popup:

                        if (window.opened == false)
                        {
                            window.opened = true;

                            ImGui.OpenPopup($"{window.title}##Popup{window.GetType().Name}");
                        }

                        ImGui.SetNextWindowSize(new Vector2(window.size.X, window.size.Y));
                        ImGui.SetNextWindowPos(new Vector2((io.DisplaySize.X - window.size.X) / 2, (io.DisplaySize.Y - window.size.Y) / 2));

                        if(window.windowType == EditorWindowType.Popup)
                        {
                            shouldShow = ImGui.BeginPopup($"{window.title}##Popup{window.GetType().Name}");
                        }
                        else
                        {
                            shouldShow = ImGui.BeginPopupModal($"{window.title}##Popup{window.GetType().Name}");
                        }

                        if (shouldShow == false)
                        {
                            ImGui.CloseCurrentPopup();

                            editorWindows.Remove(window);
                        }

                        break;

                    default:

                        shouldShow = ImGui.Begin($"{window.title}##{window.GetType().FullName}", ref isOpen, flags);

                        break;
                }

                if (shouldShow)
                {
                    try
                    {
                        window.OnGUI();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Window {window.GetType().FullName} Error: {e}");
                    }

                    switch (window.windowType)
                    {
                        case EditorWindowType.Popup:
                        case EditorWindowType.Modal:

                            ImGui.EndPopup();

                            break;

                        default:

                            ImGui.End();

                            break;
                    }

                    if(isOpen == false)
                    {
                        window.Close();
                    }

                    var size = ImGui.GetWindowSize();

                    window.size = new Vector2Int((int)size.X, (int)size.Y);
                }
            }

            lock(backgroundLock)
            {
                if (window.HasFocus && showingProgress == false && (backgroundHandles.Count == 0 || backgroundHandles.All(x => x.Completed)))
                {
                    if (needsGameRecompile)
                    {
                        needsGameRecompile = false;

                        UnloadGame();

                        RefreshStaging(currentPlatform, null);
                    }
                    else if (needsRefreshStaging)
                    {
                        RefreshStaging(currentPlatform, null, false);
                    }
                }
            }

            /*
            ImGui.Begin("Debug", ImGuiWindowFlags.NoDocking);

            if (World.Current != null)
            {
                var mouseRay = Camera.ScreenPointToRay(Input.MousePosition, default, camera, cameraTransform);

                var hit = Physics.RayCast3D(mouseRay, out var body, out _, LayerMask.Everything, maxDistance: 10);

                ImGui.Text($"Mouse Ray:");

                ImGui.Text($"Position: {mouseRay.position.X}, {mouseRay.position.Y}, {mouseRay.position.Z}");

                ImGui.Text($"Direction: {mouseRay.direction.X}, {mouseRay.direction.Y}, {mouseRay.direction.Z}");

                ImGui.Checkbox("Hit", ref hit);

                ImGui.Text($"RenderTarget size: {gameRenderTarget?.width ?? 0} {gameRenderTarget?.height ?? 0}");

                ImGui.Text($"Culled Renderers: {RenderSystem.CulledRenderers}");
            }

            ImGui.End();
            */

            ProgressPopup(io);
            MessageBoxPopup(io);

            imgui.EndFrame();

            if (World.Current != null && Input.GetMouseButton(MouseButton.Left) && mouseIsHoveringImGui == false && ImGuizmo.IsUsingAny() == false)
            {
                var ray = Camera.ScreenPointToRay(Input.MousePosition, default, camera, cameraTransform);

                if (Physics3D.Instance.RayCast(ray, out var body, out _, LayerMask.Everything, PhysicsTriggerQuery.Ignore, 1000))
                {
                    SetSelectedEntity(body.Entity);
                }
                else
                {
                    SetSelectedEntity(default);
                }
            }
        };

        window.OnScreenSizeChange = (hasFocus) =>
        {
            var flags = RenderSystem.ResetFlags(playerSettings.videoFlags);

            Screen.Width = playerSettings.screenWidth = window.width;
            Screen.Height = playerSettings.screenHeight = window.height;

            playerSettings.monitorIndex = window.MonitorIndex;
            playerSettings.maximized = window.Maximized;

            PlayerSettings.Save(playerSettings);

            bgfx.reset((uint)window.width, (uint)window.height, (uint)flags, bgfx.TextureFormat.RGBA8);

            bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);
            bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.UIntValue, 1, 0);

            bgfx.set_view_rect_ratio(SceneView, 0, 0, bgfx.BackbufferRatio.Equal);
            bgfx.set_view_clear(SceneView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.UIntValue, 1, 0);
            
            bgfx.set_view_rect_ratio(WireframeView, 0, 0, bgfx.BackbufferRatio.Equal);
            bgfx.set_view_clear(WireframeView, (ushort)bgfx.ClearFlags.Depth, 0, 1, 0);

            if(hadFocus != hasFocus && hasFocus)
            {
                if (csProjManager.NeedsGameRecompile() && refreshingAssets == false)
                {
                    needsGameRecompile = true;
                }
            }

            hadFocus = hasFocus;
        };

        window.OnMove = (position) =>
        {
            playerSettings.windowPosition = position;

            playerSettings.monitorIndex = window.MonitorIndex;
            playerSettings.maximized = window.Maximized;

            PlayerSettings.Save(playerSettings);
        };

        window.OnCleanup = () =>
        {
            for(; ; )
            {
                if (backgroundHandles.Count == 0 ||
                    backgroundHandles.All(x => x.Completed))
                {
                    break;
                }
            }

            imgui.Destroy();

            RenderSystem.Instance.Shutdown();

            SubsystemManager.instance.Destroy();

            ResourceManager.instance.Destroy(ResourceManager.DestroyMode.Final);
        };

        window.Run();
    }

    private void SaveLastProjects()
    {
        try
        {
            var json = JsonConvert.SerializeObject(lastProjects, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

            File.WriteAllText(Path.Combine(Storage.PersistentDataPath, "ProjectList.json"), json);
        }
        catch(Exception)
        {
        }
    }

    private bool CreateProject(string path)
    {
        try
        {
            var directory = new DirectoryInfo(path);

            if(directory.GetDirectories().Length != 0 || directory.GetFiles().Length != 0)
            {
                Log.Error($"Failed to create project: Directory not empty");

                return false;
            }
        }
        catch(Exception)
        {
            Log.Error($"Failed to create project: Directory not valid");

            return false;
        }

        try
        {
            var json = JsonConvert.SerializeObject(new ProjectInfo()
            {
                stapleVersion = StapleVersion,
            }, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

            File.WriteAllText(Path.Combine(path, "ProjectInfo.json"), json);
        }
        catch(Exception)
        {
            Log.Error($"Failed to create project: Failed to save project info json");

            return false;
        }

        EditorUtils.CreateDirectory(Path.Combine(path, "Assets"));

        if(EditorUtils.CopyDirectory(Path.Combine(EditorUtils.EditorPath.Value, "Editor Resources", "ProjectSettings"), Path.Combine(path, "Settings")) == false)
        {
            Log.Error($"Failed to create project: Failed to copy editor resources");

            return false;
        }

        return true;
    }

    private void AddMenuItem(string path, Action onClick)
    {
        MenuItemInfo item = null;
        var pieces = path.Split("/".ToCharArray()).ToList();

        while (pieces.Count > 0)
        {
            if (item == null)
            {
                item = menuItems.Find(x => x.name == pieces[0]);

                if (item == null)
                {
                    item = new MenuItemInfo()
                    {
                        name = pieces[0],
                        onClick = pieces.Count == 1 ? onClick : null,
                    };

                    menuItems.Add(item);
                }
            }
            else
            {
                var child = item.children.Find(x => x.name == pieces[0]);

                if (child == null)
                {
                    child = new MenuItemInfo()
                    {
                        name = pieces[0],
                        onClick = pieces.Count == 1 ? onClick : null,
                    };

                    item.children.Add(child);
                }

                item = child;
            }

            pieces.RemoveAt(0);
        }
    }

    private MenuItemInfo FindMenuItem(string path)
    {
        MenuItemInfo item = null;
        var pieces = path.Split("/".ToCharArray()).ToList();

        while(pieces.Count > 0)
        {
            if(item == null)
            {
                item = menuItems.Find(x => x.name == pieces[0]);

                if(item == null)
                {
                    return null;
                }
            }
            else
            {
                item = item.children.Find(x => x.name == pieces[0]);

                if (item == null)
                {
                    return null;
                }
            }

            pieces.RemoveAt(0);
        }

        return item;
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

    internal void UpdateLastSession()
    {
        UpdateLastSession(new LastSessionInfo()
        {
            currentPlatform = currentPlatform,
            lastOpenScene = lastOpenScene,
            lastPickedBuildDirectories = lastPickedBuildDirectories,
            debugBuild = buildPlayerDebug,
            nativeBuild = buildPlayerNativeAOT,
            debugRedists = buildPlayerDebugRedists,
        });
    }

    private void UpdateLastSession(LastSessionInfo info)
    {
        var path = Path.Combine(basePath, "Cache", "LastSession.json");

        try
        {
            var text = JsonConvert.SerializeObject(info, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

            File.WriteAllText(path, text);
        }
        catch(Exception)
        {
        }
    }

    private string CachePathResolver(string path)
    {
        if(basePath == null)
        {
            return path;
        }

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

    public void ShowAssetPicker(Type type, string key, string[] ignoredGuids)
    {
        var window = EditorWindow.GetWindow<AssetPickerWindow>();

        window.assetPickerKey = key;
        window.assetPickerSearch = "";
        window.assetPickerType = type;
        window.currentPlatform = currentPlatform;
        window.basePath = basePath;
        window.projectBrowser = projectBrowser;
        window.ignoredGuids = ignoredGuids;
    }

    public void ShowSpritePicker(Texture texture, List<TextureSpriteInfo> sprites, Action<int> onFinish)
    {
        var window = EditorWindow.GetWindow<SpritePicker>();

        window.texture = texture;
        window.sprites = sprites;
        window.onFinish = onFinish;
    }

    private void SetSelectedEntity(Entity entity)
    {
        selectedEntity = entity;
        selectedProjectNode = null;
        selectedProjectNodeData = null;

        foreach(var editor in cachedEditors)
        {
            editor.Value?.Destroy();
        }

        cachedEditors.Clear();
        cachedGizmoEditors.Clear();
        EditorGUI.pendingObjectPickers.Clear();

        EditorWindow.GetWindow<AssetPickerWindow>().Close();

        if(selectedEntity == default)
        {
            return;
        }

        var counter = 0;

        selectedEntity.IterateComponents((ref IComponent component) =>
        {
            counter++;

            if (component is Transform transform)
            {
                return;
            }

            var editor = Editor.CreateEditor(component);

            if (editor != null)
            {
                cachedEditors.Add($"{counter}{component.GetType().FullName}", editor);
            }

            var gizmoEditor = GizmoEditor.CreateGizmoEditor(component);

            if(gizmoEditor != null)
            {
                cachedGizmoEditors.Add(counter - 1, gizmoEditor);
            }
        });
    }

    private void ReloadAssetTemplates()
    {
        registeredAssetTemplates.Clear();

        string[] files;

        try
        {
            files = Directory.GetFiles(Path.Combine(EditorUtils.EditorPath.Value, "Editor Resources", "AssetTemplates"));
        }
        catch(Exception)
        {
            return;
        }

        foreach(var file in files)
        {
            try
            {
                registeredAssetTemplates.Add(Path.GetFileName(file), File.ReadAllBytes(file));
            }
            catch(Exception)
            {
            }
        }
    }

    internal void AddEditorLayers()
    {
        LayerMask.AllLayers.Add(RenderTargetLayerName);
    }

    private void LoadModules()
    {
        try
        {
            var moduleBasePath = Path.Combine(EditorUtils.EditorPath.Value, "Player Backends", currentPlatform.ToString(), "Modules");

            var directories = Directory.GetDirectories(moduleBasePath);

            foreach (var directory in directories)
            {
                var file = Path.Combine(directory, "Assembly", "Debug", $"{Path.GetFileName(directory)}.dll");

                try
                {
                    var loader = new StapleAssemblyLoadContext(AppContext.BaseDirectory, () =>
                    {
                        return ([], []);
                    });

                    using var stream = new MemoryStream(File.ReadAllBytes(file));

                    var assembly = loader.LoadFromStream(stream);

                    if (assembly != null)
                    {
                        var initializers = assembly.GetTypes()
                            .Where(x => x.IsSubclassOf(typeof(ModuleInitializer)))
                            .ToArray();

                        if (initializers.Length == 0)
                        {
                            loader.Unload();

                            continue;
                        }

                        foreach (var initializer in initializers)
                        {
                            var instance = ObjectCreation.CreateObject<ModuleInitializer>(initializer);

                            if (instance == null)
                            {
                                loader.Unload();

                                continue;
                            }

                            if (modulesList.TryGetValue(instance.Kind(), out var list) == false)
                            {
                                list = [];

                                modulesList.Add(instance.Kind(), list);
                            }

                            list.Add(new()
                            {
                                contextLoader = loader,
                                module = instance,
                                moduleName = Path.GetFileNameWithoutExtension(file),
                            });

                            System.Console.WriteLine($"Loaded module {Path.GetFileNameWithoutExtension(file)}");
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        catch(Exception e)
        {
            System.Console.WriteLine($"Failed to load modules: {e}");
        }
    }
}
