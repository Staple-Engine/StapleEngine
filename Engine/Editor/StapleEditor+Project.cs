using Newtonsoft.Json;
using NfdSharp;
using Staple.Internal;
using Staple.Jobs;
using Staple.PackageManagement;
using Staple.ProjectManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private static Regex stagingProgressRegex = StagingProgressRegex();

    private static readonly List<string> excludedStagingRefreshExtensions =
    [
        AssetSerialization.SceneExtension,
    ];

    private void ImGuiNewProject()
    {
        var result = Nfd.PickFolder("", out var projectPath);

        if (result == Nfd.NfdResult.NFD_OKAY)
        {
            CreateProject(projectPath);
            LoadProject(projectPath);
        }
    }

    private void ImGuiOpenProject()
    {
        var result = Nfd.PickFolder("", out var projectPath);

        if (result == Nfd.NfdResult.NFD_OKAY)
        {
            LoadProject(projectPath);
        }
    }

    private void RecordScene()
    {
        var scenePath = Path.Combine(BasePath, "Cache", $"LastScene.{AssetSerialization.SceneExtension}");

        try
        {
            File.Delete(scenePath);
        }
        catch (Exception)
        {
        }

        if (Scene.current != null)
        {
            try
            {
                var scene = Scene.current.Serialize();

                var json = JsonConvert.SerializeObject(scene.objects, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                File.WriteAllText(scenePath, json);
            }
            catch (Exception)
            {
            }
        }
    }

    private bool LoadRecordedScene()
    {
        var scenePath = Path.Combine(BasePath, "Cache", $"LastScene.{AssetSerialization.SceneExtension}");

        var scene = ResourceManager.instance.LoadRawSceneFromPath(scenePath);

        if(scene != null)
        {
            Scene.SetActiveScene(scene);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Loads a project at a specific path
    /// </summary>
    /// <param name="path">The patht to load from</param>
    public void LoadProject(string path)
    {
        UnloadGame();

        undoStack.Clear();

        try
        {
            var json = File.ReadAllText(Path.Combine(path, "ProjectInfo.json"));

            var projectInfo = JsonConvert.DeserializeObject<ProjectInfo>(json);

            if (projectInfo.stapleVersion != StapleVersion)
            {
                ShowMessageBox($"Project version is not compatible\nGot {projectInfo.stapleVersion}, expected: {StapleVersion}", "OK", null);

                return;
            }
        }
        catch (Exception)
        {
            return;
        }

        BasePath =
            ThumbnailCache.basePath =
            ProjectManager.Instance.basePath =
            projectBrowser.basePath =
            PackageManager.instance.basePath =
            Path.GetFullPath(path);

        PackageManager.instance.gitPath = editorSettings.gitExternalPath;

        PackageManager.instance.Refresh(ResetAssetPaths);

        ProjectManager.Instance.stapleBasePath = StapleBasePath;

        Log.Info($"Project Path: {BasePath}");

        projectBrowser.UpdateProjectBrowserNodes();

        try
        {
            Directory.CreateDirectory(Path.Combine(BasePath, "Cache"));
        }
        catch (Exception)
        {
        }

        try
        {
            Directory.CreateDirectory(Path.Combine(BasePath, "Cache", "Staging"));
        }
        catch (Exception)
        {
        }

        try
        {
            var json = File.ReadAllText(Path.Combine(BasePath, "Settings", "AppSettings.json"));

            projectAppSettings = JsonConvert.DeserializeObject<AppSettings>(json);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load project app settings: {e}");

            projectAppSettings = AppSettings.Default;
        }

        LayerMask.SetLayers(CollectionsMarshal.AsSpan(projectAppSettings.layers), CollectionsMarshal.AsSpan(editorAppSettings.sortingLayers));

        AddEditorLayers();

        Physics3D.Instance.Shutdown();

        Physics3D.Instance.Startup();

        AppSettings.Current.fixedTimeFrameRate = projectAppSettings.fixedTimeFrameRate;
        AppSettings.Current.ambientLight = projectAppSettings.ambientLight;
        AppSettings.Current.enableLighting = projectAppSettings.enableLighting;

        LightSystem.Enabled = projectAppSettings.enableLighting;

        foreach (var pair in projectAppSettings.renderers)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(BasePath, "Cache", "Staging", pair.Key.ToString()));
            }
            catch (Exception)
            {
            }
        }

        var lastSession = GetLastSession();

        if (lastSession != null)
        {
            currentPlatform = lastSession.currentPlatform;
            lastOpenScene = lastSession.lastOpenScene;
            lastPickedBuildDirectories = lastSession.lastPickedBuildDirectories;
            buildPlayerDebug = lastSession.debugBuild;
            buildPlayerNativeAOT = lastSession.nativeBuild;
            buildPlayerDebugRedists = lastSession.debugRedists;
            buildPlayerSingleFile = lastSession.publishSingleFile;
        }
        else
        {
            currentPlatform = Platform.CurrentPlatform.Value;
            lastOpenScene = null;
            lastPickedBuildDirectories.Clear();
            buildPlayerDebug = true;
            buildPlayerNativeAOT = false;
            buildPlayerDebugRedists = false;
            buildPlayerSingleFile = true;
        }

        projectBrowser.currentPlatform = currentPlatform;

        if (fileSystemWatcher != null)
        {
            fileSystemWatcher.Dispose();

            fileSystemWatcher = null;
        }

        fileSystemWatcher = new FileSystemWatcher(BasePath);

        void FileSystemHandler(object sender, FileSystemEventArgs e)
        {
            lock (backgroundLock)
            {
                if (RefreshingAssets == false)
                {
                    try
                    {
                        if ((e.FullPath.EndsWith(".cs") ||
                            (Directory.Exists(e.FullPath) && e.ChangeType == WatcherChangeTypes.Deleted)) &&
                            (e.FullPath.StartsWith(Path.Combine(BasePath, "Assets")) ||
                            e.FullPath.StartsWith(Path.Combine(BasePath, "Cache", "Packages"))))
                        {
                            needsGameRecompile = true;
                        }
                        else if (Directory.Exists(e.FullPath) == false &&
                            excludedStagingRefreshExtensions.Any(x => e.FullPath.EndsWith(x)) == false &&
                            e.FullPath.StartsWith(Path.Combine(BasePath, "Assets")))
                        {
                            needsRefreshStaging = true;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        void RenamedFileSystemHandler(object sender, RenamedEventArgs e)
        {
            lock (backgroundLock)
            {
                if (RefreshingAssets == false)
                {
                    if (e.FullPath.EndsWith(".cs") ||
                        (Directory.Exists(e.FullPath) && e.ChangeType == WatcherChangeTypes.Renamed &&
                        (e.FullPath.StartsWith(Path.Combine(BasePath, "Assets")) ||
                        e.FullPath.StartsWith(Path.Combine(BasePath, "Cache", "Packages")))))
                    {
                        needsGameRecompile = true;
                    }
                    else if (Directory.Exists(e.FullPath) == false &&
                        excludedStagingRefreshExtensions.Any(x => e.FullPath.EndsWith(x)) == false &&
                        e.FullPath.StartsWith(Path.Combine(BasePath, "Assets")))
                    {
                        needsRefreshStaging = true;
                    }
                }
            }
        }

        fileSystemWatcher.Changed += FileSystemHandler;
        fileSystemWatcher.Created += FileSystemHandler;
        fileSystemWatcher.Deleted += FileSystemHandler;
        fileSystemWatcher.Renamed += RenamedFileSystemHandler;

        fileSystemWatcher.IncludeSubdirectories = true;

        fileSystemWatcher.EnableRaisingEvents = true;

        RefreshStaging(currentPlatform, () =>
        {
            ThreadHelper.Dispatch(() =>
            {
                lastProjects.lastOpenProject = path;

                var target = lastProjects.items.FirstOrDefault(x => x.path == path);

                if (target != null)
                {
                    target.date = DateTime.Now;
                }
                else
                {
                    lastProjects.items.Add(new LastProjectItem()
                    {
                        name = Path.GetFileName(path),
                        path = path,
                        date = DateTime.Now,
                    });
                }

                SaveLastProjects();

                ProjectManager.Instance.CollectGameScriptModifyStates();

                UpdateWindowTitle();
            });
        },
        true);
    }

    private void LoadProjectForBuilding(string path, Action<bool> onFinish)
    {
        try
        {
            var json = File.ReadAllText(Path.Combine(path, "ProjectInfo.json"));

            var projectInfo = JsonConvert.DeserializeObject<ProjectInfo>(json);

            if (projectInfo.stapleVersion != StapleVersion)
            {
                Log.Error($"Project version is not compatible\nGot {projectInfo.stapleVersion}, expected: {StapleVersion}");

                onFinish(false);

                return;
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load: {e}");

            onFinish(false);

            return;
        }

        BasePath =
            ThumbnailCache.basePath =
            ProjectManager.Instance.basePath =
            projectBrowser.basePath =
            PackageManager.instance.basePath =
            Path.GetFullPath(path);

        PackageManager.instance.gitPath = editorSettings.gitExternalPath;

        PackageManager.instance.Refresh(ResetAssetPaths);

        ProjectManager.Instance.stapleBasePath = StapleBasePath;

        Log.Info($"Project Path: {BasePath}");

        projectBrowser.UpdateProjectBrowserNodes();

        try
        {
            Directory.CreateDirectory(Path.Combine(BasePath, "Cache"));
        }
        catch (Exception)
        {
        }

        try
        {
            Directory.CreateDirectory(Path.Combine(BasePath, "Cache", "Staging"));
        }
        catch (Exception)
        {
        }

        try
        {
            var json = File.ReadAllText(Path.Combine(BasePath, "Settings", "AppSettings.json"));

            projectAppSettings = JsonConvert.DeserializeObject<AppSettings>(json);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load project app settings: {e}");

            onFinish(false);

            return;
        }

        LayerMask.SetLayers(CollectionsMarshal.AsSpan(projectAppSettings.layers), CollectionsMarshal.AsSpan(editorAppSettings.sortingLayers));

        foreach (var pair in projectAppSettings.renderers)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(BasePath, "Cache", "Staging", pair.Key.ToString()));
            }
            catch (Exception)
            {
            }
        }

        projectBrowser.currentPlatform = currentPlatform;

        RefreshAssets(true, () => onFinish(true));
    }

    /// <summary>
    /// Updates the current window title with the project name and active scene
    /// </summary>
    private void UpdateWindowTitle()
    {
        if((lastOpenScene?.Length ?? 0) > 0)
        {
            window.Title = $"Staple Editor - {Path.GetFileName(BasePath)} - {Path.GetFileNameWithoutExtension(lastOpenScene)} - {RenderWindow.CurrentRenderer}";
        }
        else
        {
            window.Title = $"Staple Editor - {Path.GetFileName(BasePath)} - {RenderWindow.CurrentRenderer}";
        }
    }

    /// <summary>
    /// Refreshes the current assets and optionally updates the C# project
    /// </summary>
    /// <param name="onFinish">Callback when finished</param>
    /// <param name="updateProject">Whether to update the project</param>
    public void RefreshAssets(bool updateProject, Action onFinish)
    {
        RefreshStaging(currentPlatform, onFinish, updateProject);
    }

    /// <summary>
    /// Refreshes the assets cache and optionally updates the C# project
    /// </summary>
    /// <param name="platform">The current platform</param>
    /// <param name="onFinish">Callback when finished</param>
    /// <param name="updateProject">Whether to update the project</param>
    /// <param name="checkBuild">Check whether to build the project</param>
    public void RefreshStaging(AppPlatform platform, Action onFinish, bool updateProject = false, bool checkBuild = true)
    {
        if(editorMode == EditorMode.Normal)
        {
            if (gameLoadDisabled || RefreshingAssets)
            {
                return;
            }

            RecordScene();

            lock (backgroundLock)
            {
                RefreshingAssets = true;
                needsRefreshStaging = false;
            }
        }

        projectBrowser.UpdateProjectBrowserNodes();

        projectBrowser.CreateMissingMetaFiles(() =>
        {
            var bakerPath = Path.Combine(Storage.StapleBasePath, "Tools", "bin", "Baker");

            void Finish()
            {
                if (projectAppSettings == null)
                {
                    lock (backgroundLock)
                    {
                        RefreshingAssets = false;
                    }

                    return;
                }

                var progress = 0.0f;
                var message = "";

                var handle = JobScheduler.Schedule(new ActionJob(() =>
                {
                    try
                    {
                        if (projectAppSettings.renderers.TryGetValue(platform, out var renderers))
                        {
                            var rendererParameters = new HashSet<string>();

                            foreach (var item in renderers)
                            {
                                switch (item)
                                {
                                    case RendererType.Direct3D11:

                                        rendererParameters.Add("-r d3d11");

                                        break;

#if STAPLE_SUPPORTS_D3D12
                                case RendererType.Direct3D12:

                                    rendererParameters.Add("-r d3d12");

                                    break;
#endif

                                    case RendererType.Metal:

                                        rendererParameters.Add("-r metal");

                                        break;

                                    case RendererType.OpenGL:

                                        rendererParameters.Add("-r opengl");

                                        break;

                                    case RendererType.OpenGLES:

                                        rendererParameters.Add("-r opengles");

                                        break;

                                    case RendererType.Vulkan:

                                        rendererParameters.Add("-r spirv");

                                        break;
                                }
                            }

                            string[] packageDirectories = [];

                            try
                            {
                                packageDirectories = Directory.GetDirectories(Path.Combine(BasePath, "Cache", "Packages"));
                            }
                            catch (Exception)
                            {
                            }

                            var packageArgs = "";

                            foreach (var directory in packageDirectories)
                            {
                                packageArgs += $"-i \"{directory}\" ";
                            }

                            var args = $"-i \"{BasePath}/Assets\" {packageArgs} -o \"{BasePath}/Cache/Staging/{platform}\" -platform {platform} -editor {string.Join(" ", rendererParameters)}".Replace("\\", "/");

                            var processInfo = new ProcessStartInfo(bakerPath, args)
                            {
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                WorkingDirectory = Environment.CurrentDirectory
                            };

                            var process = new Process
                            {
                                StartInfo = processInfo
                            };

                            Staple.Tooling.Utilities.ExecuteAndCollectProcess(process,
                                (m) =>
                                {
                                    var match = stagingProgressRegex.Match(m);

                                    if (match.Success &&
                                        int.TryParse(match.Groups[1].Value, out var left) &&
                                        int.TryParse(match.Groups[2].Value, out var right))
                                    {
                                        progress = left / (float)right;

                                        message = match.Groups[3].Value;

                                        instance.SetBackgroundProgress(progress, message);
                                    }
                                });

                            lock (backgroundLock)
                            {
                                RefreshingAssets = false;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        lock (backgroundLock)
                        {
                            RefreshingAssets = false;
                        }
                    }

                    if (editorMode == EditorMode.Normal)
                    {
                        ThreadHelper.Dispatch(() =>
                        {
                            try
                            {
                                if (updateProject)
                                {
                                    UnloadGame();
                                    LoadGame();
                                }

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

                                ShowBackgroundProcess();

                                AssetDatabase.Reload(Path.Combine(BasePath, "Cache", "AssetDatabase"),
                                    () =>
                                    {
                                        HideBackgroundProcess();

                                        ThreadHelper.Dispatch(() =>
                                        {
                                            projectBrowser.UpdateProjectBrowserNodes();

                                            if (LoadRecordedScene() == false)
                                            {
                                                if ((lastOpenScene?.Length ?? 0) > 0)
                                                {
                                                    Scene scene = null;

                                                    if (lastOpenScene.EndsWith($".{AssetSerialization.PrefabExtension}"))
                                                    {
                                                        World.Current = new();
                                                        scene = new();

                                                        var prefab = ResourceManager.instance.LoadRawPrefabFromPath(lastOpenScene);

                                                        if (prefab != null)
                                                        {
                                                            SceneSerialization.InstantiatePrefab(default, prefab.data);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        scene = ResourceManager.instance.LoadRawSceneFromPath(lastOpenScene);
                                                    }

                                                    Scene.SetActiveScene(scene);
                                                }
                                                else
                                                {
                                                    Scene.SetActiveScene(null);
                                                }
                                            }

                                            ResetScenePhysics(false);

                                            onFinish?.Invoke();
                                        });
                                    });
                            }
                            catch (Exception e)
                            {
                                Log.Error(e.ToString());
                            }
                        });
                    }
                    else
                    {
                        onFinish?.Invoke();
                    }
                }));

                StartBackgroundTask(handle);
            }

            if (updateProject)
            {
                ShowBackgroundProcess();

                AssetDatabase.Reload(Path.Combine(BasePath, "Cache", "AssetDatabase"), 
                    () =>
                    {
                        HideBackgroundProcess();

                        UpdateCSProj(platform, checkBuild, Finish);
                    });
            }
            else
            {
                Finish();
            }
        });
    }

    /// <summary>
    /// Saves a Staple Asset
    /// </summary>
    /// <param name="assetPath">The asset path</param>
    /// <param name="assetInstance">The asset's instance</param>
    /// <returns>Whether it was saved</returns>
    public static bool SaveAsset(string assetPath, IStapleAsset assetInstance)
    {
        if(Path.IsPathRooted(assetPath) == false)
        {
            assetPath = Path.Combine(instance.BasePath, assetPath);
        }

        var existed = false;

        try
        {
            existed = File.Exists(assetPath);
        }
        catch(Exception)
        {
        }

        try
        {
            var guidField = assetInstance.GetType().GetProperty(nameof(IGuidAsset.Guid), BindingFlags.Public | BindingFlags.Instance);
            var guid = Guid.NewGuid().ToString();

            if (guidField != null && guidField.PropertyType == typeof(GuidHasher) && guidField.GetValue(assetInstance) != null)
            {
                guid = ((GuidHasher)guidField.GetValue(assetInstance)).Guid;
            }

            var serialized = AssetSerialization.Serialize(assetInstance, StapleSerializationMode.Text);

            if (serialized != null)
            {
                serialized.guid = guid;

                var json = JsonConvert.SerializeObject(serialized, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                File.WriteAllText(assetPath, json);

                var holder = new AssetHolder()
                {
                    guid = guid,
                    typeName = assetInstance.GetType().FullName,
                };

                json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                File.WriteAllText($"{assetPath}.meta", json);
            }
        }
        catch(Exception e)
        {
            Log.Debug($"Failed to save asset: {e}");

            if(existed == false)
            {
                try
                {
                    File.Delete(assetPath);
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        return true;
    }

    [GeneratedRegex("\\[(.*?)\\/(.*?)\\]((.|\\n)*)")]
    private static partial Regex StagingProgressRegex();
}