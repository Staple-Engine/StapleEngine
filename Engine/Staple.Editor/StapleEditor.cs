using Bgfx;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Newtonsoft.Json;
using Staple.Editor.Templates;
using Staple.Internal;
using Staple.Jobs;
using Staple.JoltPhysics;
using Staple.OpenALAudio;
using Staple.PackageManagement;
using Staple.ProjectManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

[assembly: InternalsVisibleTo("Staple.Editor.App")]

namespace Staple.Editor;

internal partial class StapleEditor
{
    public static readonly string StapleVersion = $"{Platform.StapleVersionMajor}.{Platform.StapleVersionMinor}";

    internal const string RenderTargetLayerName = "STAPLE_EDITOR_RENDER_TARGET_LAYER";

    internal static string StapleBasePath => Storage.StapleBasePath;

    private static Color PrefabColor = new Color32("#00CED1");

    internal const int ClearView = 0;
    internal const int MeshRenderView = 252;
    internal const int SceneView = 253;
    internal const int WireframeView = 254;

    #region Classes
    enum ViewportType
    {
        Scene,
        Game,
    }

    public enum SceneMode
    {
        Scene,
        Prefab,
    }

    public enum EditorMode
    {
        Normal,
        Build,
    }
    
    enum PlayMode
    {
        Stopped,
        Playing,
        Paused,
    }

    internal class DragDropPayload
    {
        public int index;
        public ImGuiUtils.ContentGridItem item;
        public Action<int, ImGuiUtils.ContentGridItem> action;
    }

    [Serializable]
    public class EditorSettings
    {
        public string gitExternalPath;
        public bool autoRecompile = true;
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
        public bool publishSingleFile = true;

        public Dictionary<AppPlatform, string> lastPickedBuildDirectories = [];
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
        public List<LastProjectItem> items = [];
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

        private readonly Func<(string[], string[])> assemblyPathsCallback;

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
                var data = File.ReadAllBytes(assemblyPath);

                using var s = new MemoryStream(data);

                return LoadFromStream(s);
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
                        var data = File.ReadAllBytes(p);

                        using var s = new MemoryStream(data);

                        return LoadFromStream(s);
                    }
                }
                catch(Exception)
                {
                }
            }

            return null;
        }
    }

    class RenderQueue : IWorldChangeReceiver
    {
        public readonly SceneQuery<Transform> transforms = new(true);
        public readonly Dictionary<IRenderSystem, List<(Entity, Transform, IComponent)>> renderQueue = [];
        public readonly List<Entity> disabledEntities = [];

        public void WorldChanged()
        {
            renderQueue.Clear();
            disabledEntities.Clear();

            foreach (var (entity, transform) in transforms.Contents)
            {
                var layer = entity.Layer;

                if (layer == LayerMask.NameToLayer(RenderTargetLayerName))
                {
                    continue;
                }

                if(entity.EnabledInHierarchy == false)
                {
                    disabledEntities.Add(entity);

                    continue;
                }

                foreach (var system in RenderSystem.Instance.renderSystems)
                {
                    if (system.UsesOwnRenderProcess)
                    {
                        continue;
                    }

                    if (entity.TryGetComponent(system.RelatedComponent, out var component))
                    {
                        if (renderQueue.TryGetValue(system, out var content) == false)
                        {
                            content = [];

                            renderQueue.Add(system, content);
                        }

                        content.Add((entity, transform, component));
                    }
                }
            }
        }
    }
    #endregion

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

    private int activeBottomTab = 0;

    private RenderTarget gameRenderTarget;

    private Vector2 gameWindowPosition;

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

    private readonly RenderQueue renderQueue = new();
    #endregion

    #region Entities
    private Entity selectedEntity;

    private bool resetSelection = false;

    internal Entity draggedEntity;

    internal Entity dropTargetEntity;
    #endregion

    #region Project
    internal string BasePath { get; private set; }

    private ProjectBrowserNode selectedProjectNode;

    private object selectedProjectNodeData;

    private string lastOpenScene;

    internal Dictionary<AppPlatform, string> lastPickedBuildDirectories = [];

    private readonly AppSettings editorAppSettings = AppSettings.Default;

    internal AppSettings projectAppSettings;

    private readonly ProjectBrowser projectBrowser = new();

    private LastProjectInfo lastProjects = new();

    internal Dictionary<string, DragDropPayload> dragDropPayloads = [];

    internal Type dropTargetObjectPickerType = null;

    internal Action<object> dropTargetObjectPickerAction;
    #endregion

    #region Editor
    private readonly Dictionary<Entity, EntityBody> pickEntityBodies = [];

    private readonly Dictionary<string, Editor> cachedEditors = [];

    private readonly Dictionary<int, GizmoEditor> cachedGizmoEditors = [];

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

    public EditorMode editorMode = EditorMode.Normal;

    public EditorSettings editorSettings = new();

    public SceneMode sceneMode = SceneMode.Scene;

    private bool initialized = false;
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

    internal bool buildPlayerSingleFile = true;

    private bool needsGameRecompile = false;

    private bool forceGameRecompile = false;

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

    internal bool RefreshingAssets { get; private set; } = false;

    private string projectToLoad;

    private string buildOutputDirectory;

    #endregion

    #region PlayMode
    private PlayMode playMode = PlayMode.Stopped;
    private bool forceCursorVisible = false;
    #endregion

    private static WeakReference<StapleEditor> privInstance;

    public static StapleEditor instance => privInstance.TryGetTarget(out var target) ? target : null;

    public void Run(string[] args)
    {
        MessagePackInit.Initialize();

        privInstance = new WeakReference<StapleEditor>(this);

        Platform.IsPlaying = false;
        Platform.IsEditor = true;

        ReloadTypeCache();

        ModuleInitializer.LoadAll();

        editorAppSettings.runInBackground = true;
        editorAppSettings.appName = "Staple Editor";
        editorAppSettings.companyName = "Staple Engine";

        LayerMask.SetLayers(CollectionsMarshal.AsSpan(editorAppSettings.layers), CollectionsMarshal.AsSpan(editorAppSettings.sortingLayers));

        AppSettings.Current = editorAppSettings;

        AssetDatabase.assetPathResolver = CachePathResolver;

        Storage.Update(editorAppSettings.appName, editorAppSettings.companyName);

        Log.SetLog(new FSLog(Path.Combine(Storage.PersistentDataPath, "EditorLog.log")));

        Log.Instance.onLog += (type, message) =>
        {
            Platform.platformProvider.ConsoleLog($"[{type}] {message}");
        };

        LoadEditorSettings();

        PlayerBackendManager.Instance.Initialize(Path.Combine(EditorUtils.EditorPath.Value, "PlayerBackends"));

        Log.Info($"Current Platform: {Platform.CurrentPlatform.Value}");

        currentPlatform = Platform.CurrentPlatform.Value;

        buildBackend = PlayerBackendManager.Instance.GetBackend(currentPlatform).name;

        if (ResourceManager.instance.LoadPak(Path.Combine(Storage.StapleBasePath, "DefaultResources", $"DefaultResources-{Platform.CurrentPlatform.Value}.pak")) == false)
        {
            Log.Error("Failed to load default resources pak");

            return;
        }

        for(var i = 0; i < args.Length; i++)
        {
            switch(args[i].ToLowerInvariant())
            {
                case "-build":

                    editorMode = EditorMode.Build;

                    break;

                case "-project":

                    if(i + 1 < args.Length)
                    {
                        projectToLoad = args[i + 1];

                        i++;
                    }

                    break;

                case "-o":

                    if(i + 1 < args.Length)
                    {
                        buildOutputDirectory = args[i + 1];

                        i++;
                    }

                    break;

                case "-buildtarget":

                    if(i + 1 < args.Length)
                    {
                        var target = PlayerBackendManager.BackendNames.FirstOrDefault(x => x.Equals(args[i + 1],
                            StringComparison.InvariantCultureIgnoreCase));

                        if(target == null)
                        {
                            var backendList = new StringBuilder();

                            foreach (var name in PlayerBackendManager.BackendNames)
                            {
                                backendList.AppendLine($"\t{name}");
                            }

                            Log.Error($"Invalid backend {args[i + 1]}.\nList of available backends:\n{backendList}");

                            Environment.Exit(1);
                        }
                        else
                        {
                            buildBackend = target;
                        }

                        i++;
                    }

                    break;

                case "-builddebug":

                    if(i + 1 < args.Length)
                    {
                        var v = args[i + 1].ToLowerInvariant();

                        buildPlayerDebug = v == "true" || v == "1";

                        i++;
                    }

                    break;

                case "-buildnative":

                    if (i + 1 < args.Length)
                    {
                        var v = args[i + 1].ToLowerInvariant();

                        buildPlayerNativeAOT = v == "true" || v == "1";

                        i++;
                    }

                    break;

                case "-builddebugredists":

                    if (i + 1 < args.Length)
                    {
                        var v = args[i + 1].ToLowerInvariant();

                        buildPlayerDebugRedists = v == "true" || v == "1";

                        i++;
                    }

                    break;

                case "-buildsinglefile":

                    if (i + 1 < args.Length)
                    {
                        var v = args[i + 1].ToLowerInvariant();

                        buildPlayerSingleFile = v == "true" || v == "1";

                        i++;
                    }

                    break;
            }
        }

        ReloadAssetTemplates();

        switch(editorMode)
        {
            case EditorMode.Normal:

                NormalEditorLoop();

                break;

            case EditorMode.Build:

                if(projectToLoad == null)
                {
                    Log.Error($"Missing project to build. Did you forget to specify it with `-project path`?");

                    Environment.Exit(1);
                }

                if(buildOutputDirectory == null)
                {
                    Log.Error($"Missing build output directory. Did you forget to specify it with `-o path`?");

                    Environment.Exit(1);
                }

                if(buildBackend == null ||
                    PlayerBackendManager.Instance.GetBackend(buildBackend) is not PlayerBackend backend)
                {
                    Log.Error($"Missing build backend, Did you forget to specify it with `-backend name`?");

                    var backendList = new StringBuilder();

                    foreach (var name in PlayerBackendManager.BackendNames)
                    {
                        backendList.AppendLine($"\t{name}");
                    }

                    Log.Error($"List of available backends:\n{backendList}");

                    Environment.Exit(1);

                    return;
                }

                LoadProjectForBuilding(projectToLoad, (result) =>
                {
                    if (result == false)
                    {
                        Environment.Exit(1);
                    }

                    if (buildPlayerNativeAOT)
                    {
                        if (Platform.CurrentPlatform != backend.platform)
                        {
                            buildPlayerNativeAOT = false;
                        }
                    }

                    ProjectManager.Instance.BuildPlayer(backend, projectAppSettings, buildOutputDirectory,
                        buildPlayerDebug, buildPlayerNativeAOT, buildPlayerDebugRedists, false, buildPlayerSingleFile,
                        (percent, message) => Log.Info($"[{(int)(percent * 100)}%] {message}"),
                        (message) => Log.Info(message),
                        (platform, finish) => RefreshStaging(platform, finish));
                });

                break;
        }
    }

    private void NormalEditorLoop()
    {
        AssetDatabase.databaseObserver = this;

        playerSettings = PlayerSettings.Load(editorAppSettings);

        AudioSystem.AudioListenerImpl = typeof(OpenALAudioListener);
        AudioSystem.AudioSourceImpl = typeof(OpenALAudioSource);
        AudioSystem.AudioClipImpl = typeof(OpenALAudioClip);
        AudioSystem.AudioDeviceImpl = typeof(OpenALAudioDevice);

        SubsystemManager.instance.RegisterSubsystem(AudioSystem.Instance, AudioSystem.Priority);

        if (playerSettings.screenWidth <= 0 || playerSettings.screenHeight <= 0 || playerSettings.windowPosition.X < -1000 || playerSettings.windowPosition.Y < -1000)
        {
            playerSettings.screenWidth = editorAppSettings.defaultWindowWidth;
            playerSettings.screenHeight = editorAppSettings.defaultWindowHeight;

            playerSettings.windowPosition = Vector2Int.Zero;
        }

        window = RenderWindow.Create(playerSettings.screenWidth, playerSettings.screenHeight, true, WindowMode.Windowed,
            playerSettings.windowPosition != Vector2Int.Zero ? playerSettings.windowPosition : null,
            playerSettings.maximized, playerSettings.monitorIndex, RenderSystem.ResetFlags(playerSettings.videoFlags));

        if (window == null)
        {
            return;
        }

        World.AddChangeReceiver(renderQueue);

        window.OnInit = () =>
        {
            AssetDatabase.Reload(null, () =>
            {
                Time.fixedDeltaTime = 1000.0f / TargetFramerate / 1000.0f;

                projectBrowser.LoadEditorTextures();

                var iconPath = Path.Combine(EditorUtils.EditorPath.Value, "EditorResources", "Icon.png");

                ThumbnailCache.GetTexture(iconPath, force: true);

                if (ThumbnailCache.TryGetTextureData(iconPath, out var icon))
                {
                    window.SetIcon(icon);
                }

                window.Title = $"Staple Editor - {RenderWindow.CurrentRenderer}";

                if (ImGuiProxy.instance.Initialize() == false)
                {
                    ImGuiProxy.instance.Destroy();

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

                cameraTransform.Position = new Vector3(0, 0, 5);

                try
                {
                    var json = File.ReadAllText(Path.Combine(Storage.PersistentDataPath, "ProjectList.json"));

                    lastProjects = JsonConvert.DeserializeObject<LastProjectInfo>(json);
                }
                catch (Exception)
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

                lock(backgroundLock)
                {
                    initialized = true;
                }
            });
        };

        window.OnUpdate = () =>
        {
            lock (backgroundLock)
            {
                if (initialized == false)
                {
                    return;
                }
            }

            var io = ImGui.GetIO();

            bgfx.touch(ClearView);

            if (window.width == 0 || window.height == 0)
            {
                return;
            }

            io.DisplaySize = new Vector2(window.width, window.height);
            io.DisplayFramebufferScale = new Vector2(1, 1);

            ThumbnailCache.OnFrameStart();
            EditorGUI.OnFrameStart();
            ImGuiProxy.instance.BeginFrame();

            if(Cursor.LockState == CursorLockMode.Locked &&
                playMode != PlayMode.Stopped &&
                Input.GetKeyUp(KeyCode.Escape))
            {
                forceCursorVisible = true;
            }

            if (viewportType == ViewportType.Scene && Cursor.LockState == CursorLockMode.None)
            {
                if (io.WantTextInput == false)
                {
                    var axis = Vector3.Zero;

                    if (Input.GetKey(KeyCode.A))
                    {
                        axis += cameraTransform.Left;
                    }

                    if (Input.GetKey(KeyCode.D))
                    {
                        axis += cameraTransform.Right;
                    }

                    if (Input.GetKey(KeyCode.W))
                    {
                        axis += cameraTransform.Forward;
                    }

                    if (Input.GetKey(KeyCode.S))
                    {
                        axis += cameraTransform.Back;
                    }

                    cameraTransform.LocalPosition += axis * 5 * Time.unscaledDeltaTime * (Input.GetKey(KeyCode.LeftShift) ? 2 : Input.GetKey(KeyCode.LeftControl) ? 0.5f : 1);
                }

                if (io.WantTextInput == false && Input.GetMouseButton(MouseButton.Right))
                {
                    var rotation = cameraTransform.LocalRotation.ToEulerAngles();

                    rotation.X -= Input.MouseRelativePosition.Y;
                    rotation.Y -= Input.MouseRelativePosition.X;

                    cameraTransform.LocalRotation = Math.FromEulerAngles(rotation);
                }
            }

            if(io.WantTextInput)
            {
                projectBrowser.ClearSelection();
            }

            if(playMode == PlayMode.Playing)
            {
                SubsystemManager.instance.Update(SubsystemType.Update);

                Physics3D.Instance.Update();

                ExecuteGameViewHandler(() =>
                {
                    EntitySystemManager.Instance.Update();
                });
            }

            if (forceCursorVisible)
            {
                Cursor.Visible = true;
                Cursor.LockState = CursorLockMode.None;
            }

            if (resetSelection)
            {
                resetSelection = false;

                SetSelectedEntity(selectedEntity);
            }

            if(io.WantTextInput == false)
            {
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.Z))
                {
                    undoStack.Undo();
                }

                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyUp(KeyCode.Y))
                {
                    undoStack.Redo();
                }
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

                if (ImGui.BeginTable("##ProjectsList", 3))
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

            for (var i = 0; i < currentWindows.Count; i++)
            {
                var window = currentWindows[i];
                var shouldShow = false;

                var flags = ImGuiWindowFlags.None;

                if (window.windowFlags.HasFlag(EditorWindowFlags.Dockable) == false)
                {
                    flags |= ImGuiWindowFlags.NoDocking;
                }

                if (window.windowFlags.HasFlag(EditorWindowFlags.Resizable) == false)
                {
                    flags |= ImGuiWindowFlags.NoResize;
                }

                if (window.windowFlags.HasFlag(EditorWindowFlags.MenuBar))
                {
                    flags |= ImGuiWindowFlags.MenuBar;
                }

                var otherFlags = ImGuiWindowFlags.None;

                if (window.windowFlags.HasFlag(EditorWindowFlags.HorizontalScrollbar))
                {
                    otherFlags |= ImGuiWindowFlags.HorizontalScrollbar;
                }

                if (window.windowFlags.HasFlag(EditorWindowFlags.VerticalScrollbar))
                {
                    otherFlags |= ImGuiWindowFlags.AlwaysVerticalScrollbar;
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

                        if (window.windowType == EditorWindowType.Popup)
                        {
                            shouldShow = ImGui.BeginPopup($"{window.title}##Popup{window.GetType().Name}", otherFlags);
                        }
                        else
                        {
                            shouldShow = ImGui.BeginPopupModal($"{window.title}##Popup{window.GetType().Name}", otherFlags);
                        }

                        if (shouldShow == false)
                        {
                            ImGui.CloseCurrentPopup();

                            editorWindows.Remove(window);
                        }

                        break;

                    default:

                        ImGui.SetNextWindowSize(new Vector2(window.size.X, window.size.Y), ImGuiCond.FirstUseEver);

                        ImGui.SetNextWindowPos(new Vector2((io.DisplaySize.X - window.size.X) / 2, (io.DisplaySize.Y - window.size.Y) / 2), ImGuiCond.FirstUseEver);

                        shouldShow = ImGui.Begin($"{window.title}##{window.GetType().FullName}", ref isOpen, flags | otherFlags);

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

                    if (isOpen == false)
                    {
                        window.Close();
                    }

                    var size = ImGui.GetWindowSize();

                    window.size = new Vector2Int((int)size.X, (int)size.Y);
                }
            }

            lock (backgroundLock)
            {
                if (window.HasFocus && showingProgress == false && (backgroundHandles.Count == 0 || backgroundHandles.All(x => x.Completed)))
                {
                    if ((editorSettings.autoRecompile || forceGameRecompile) && needsGameRecompile)
                    {
                        needsGameRecompile = false;

                        UnloadGame();

                        RefreshStaging(currentPlatform, null, true, forceGameRecompile == false);

                        forceGameRecompile = false;
                    }
                    else if (needsRefreshStaging)
                    {
                        RefreshStaging(currentPlatform, null, false);
                    }
                }
            }

            EditorGUI.OnFrameEnd();

            ProgressPopup(io);
            MessageBoxPopup(io);

            ImGuiProxy.instance.EndFrame();

            if (World.Current != null &&
                Input.GetMouseButton(MouseButton.Left) &&
                mouseIsHoveringImGui == false &&
                ImGuizmo.IsUsingAny() == false &&
                viewportType == ViewportType.Scene)
            {
                var ray = Camera.ScreenPointToRay(Input.MousePosition, default, camera, cameraTransform);

                if (Physics3D.Instance.RayCast(ray, out var body, out _, new LayerMask(LayerMask.GetMask(Physics3D.PhysicsPickLayer)),
                    PhysicsTriggerQuery.Ignore, 1000))
                {
                    foreach(var pair in pickEntityBodies)
                    {
                        if(pair.Value.body == body)
                        {
                            SetSelectedEntity(pair.Key);

                            break;
                        }
                    }
                }
                else
                {
                    SetSelectedEntity(default);
                }
            }
        };

        window.OnFixedUpdate = () =>
        {
            if (playMode == PlayMode.Playing)
            {
                SubsystemManager.instance.Update(SubsystemType.FixedUpdate);

                ExecuteGameViewHandler(() =>
                {
                    EntitySystemManager.Instance.UpdateFixed();
                });
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

            if (hadFocus != hasFocus && hasFocus)
            {
                if (RefreshingAssets == false && ProjectManager.Instance.NeedsGameRecompile())
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
            for (; ; )
            {
                if (backgroundHandles.Count == 0 ||
                    backgroundHandles.All(x => x.Completed))
                {
                    break;
                }
            }

            ImGuiProxy.instance.Destroy();

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

        if(EditorUtils.CopyDirectory(Path.Combine(EditorUtils.EditorPath.Value, "EditorResources", "ProjectSettings"), Path.Combine(path, "Settings")) == false)
        {
            Log.Error($"Failed to create project: Failed to copy editor resources");

            return false;
        }

        PackageManager.InitializeProject(path);

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
        var path = Path.Combine(BasePath, "Cache", "LastSession.json");

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
            publishSingleFile = buildPlayerSingleFile,
        });
    }

    private void UpdateLastSession(LastSessionInfo info)
    {
        var path = Path.Combine(BasePath, "Cache", "LastSession.json");

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
        if(BasePath == null)
        {
            return path;
        }

        var directories = new List<string>(["Assets"]);

        try
        {
            var packageDirectories = Directory.GetDirectories(Path.Combine(BasePath, "Cache", "Packages"));

            directories.AddRange(packageDirectories.Select(x => Path.GetFileName(x)));
        }
        catch(Exception)
        {
        }

        foreach (var directory in directories)
        {
            var p = Path.Combine(BasePath, "Cache", "Staging", currentPlatform.ToString(), path);

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
        }

        {
            var p = Path.Combine(BasePath, "Assets", path);

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
        }

        return path;
    }

    public void ResetAssetPaths()
    {
        AssetDatabase.assetDirectories.Clear();
        ResourceManager.instance.resourcePaths.Clear();

        AssetDatabase.assetDirectories.Add(Path.Combine(BasePath, "Assets"));
        AssetDatabase.assetDirectories.Add(Path.Combine(BasePath, "Cache", "Packages"));

        ResourceManager.instance.resourcePaths.Add(Path.Combine(BasePath, "Cache", "Staging", currentPlatform.ToString()));

        try
        {
            var packageDirectories = Directory.GetDirectories(Path.Combine(BasePath, "Cache", "Packages"));

            foreach (var directory in packageDirectories)
            {
                ResourceManager.instance.resourcePaths.Add(Path.Combine(BasePath, "Cache", "Staging", currentPlatform.ToString(), "Packages", Path.GetFileName(directory)));
            }
        }
        catch (Exception)
        {
        }
    }

    public void ShowAssetPicker(Type type, string key, string[] ignoredGuids, Func<string, bool> filter)
    {
        var window = EditorWindow.GetWindow<AssetPickerWindow>();

        window.assetPickerKey = key;
        window.assetPickerSearch = "";
        window.assetPickerType = type;
        window.currentPlatform = currentPlatform;
        window.basePath = BasePath;
        window.projectBrowser = projectBrowser;
        window.ignoredGuids = ignoredGuids;
        window.filter = filter;
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

        projectBrowser.ClearSelection();

        foreach(var editor in cachedEditors)
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
            files = Directory.GetFiles(Path.Combine(EditorUtils.EditorPath.Value, "EditorResources", "AssetTemplates"));
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
}
