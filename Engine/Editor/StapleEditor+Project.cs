using Newtonsoft.Json;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Staple.Internal;
using System.Reflection;
using System.Linq;
using NfdSharp;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Staple.Jobs;

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
        var scenePath = Path.Combine(basePath, "Cache", $"LastScene.{AssetSerialization.SceneExtension}");

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
        var scenePath = Path.Combine(basePath, "Cache", $"LastScene.{AssetSerialization.SceneExtension}");

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

        basePath =
            ThumbnailCache.basePath =
            csProjManager.basePath =
            projectBrowser.basePath =
            Path.GetFullPath(path);

        AssetDatabase.assetDirectories.Clear();
        AssetDatabase.assetDirectories.Add(Path.Combine(basePath, "Assets"));

        ResourceManager.instance.resourcePaths.Clear();
        ResourceManager.instance.resourcePaths.Add(Path.Combine(basePath, "Cache", "Staging", currentPlatform.ToString()));

        csProjManager.stapleBasePath = StapleBasePath;

        Log.Info($"Project Path: {basePath}");

        projectBrowser.UpdateProjectBrowserNodes();

        projectBrowser.CreateMissingMetaFiles();

        try
        {
            Directory.CreateDirectory(Path.Combine(basePath, "Cache"));
        }
        catch (Exception)
        {
        }

        try
        {
            Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Staging"));
        }
        catch (Exception)
        {
        }

        try
        {
            var json = File.ReadAllText(Path.Combine(basePath, "Settings", "AppSettings.json"));

            projectAppSettings = JsonConvert.DeserializeObject<AppSettings>(json);
        }
        catch(Exception e)
        {
            Log.Error($"Failed to load project app settings: {e}");

            projectAppSettings = AppSettings.Default;
        }

        LayerMask.SetLayers(CollectionsMarshal.AsSpan(projectAppSettings.layers), CollectionsMarshal.AsSpan(editorSettings.sortingLayers));

        AddEditorLayers();

        Physics3D.Instance.Shutdown();

        Physics3D.Instance.Startup();

        AppSettings.Current.fixedTimeFrameRate = projectAppSettings.fixedTimeFrameRate;
        AppSettings.Current.ambientLight = projectAppSettings.ambientLight;
        AppSettings.Current.enableLighting = projectAppSettings.enableLighting;

        LightSystem.Enabled = projectAppSettings.enableLighting;

        foreach(var pair in projectAppSettings.renderers)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Staging", pair.Key.ToString()));
            }
            catch(Exception)
            {
            }
        }

        var lastSession = GetLastSession();

        if(lastSession != null)
        {
            currentPlatform = lastSession.currentPlatform;
            lastOpenScene = lastSession.lastOpenScene;
            lastPickedBuildDirectories = lastSession.lastPickedBuildDirectories;
            buildPlayerDebug = lastSession.debugBuild;
            buildPlayerNativeAOT = lastSession.nativeBuild;
            buildPlayerDebugRedists = lastSession.debugRedists;
        }
        else
        {
            currentPlatform = Platform.CurrentPlatform.Value;
            lastOpenScene = null;
            lastPickedBuildDirectories.Clear();
            buildPlayerDebug = true;
            buildPlayerNativeAOT = false;
            buildPlayerDebugRedists = false;
        }

        projectBrowser.currentPlatform = currentPlatform;

        if (fileSystemWatcher != null)
        {
            fileSystemWatcher.Dispose();

            fileSystemWatcher = null;
        }

        fileSystemWatcher = new FileSystemWatcher(Path.Combine(basePath, "Assets"));

        void FileSystemHandler(object sender, FileSystemEventArgs e)
        {
            lock(backgroundLock)
            {
                if (refreshingAssets == false)
                {
                    try
                    {
                        if (e.FullPath.EndsWith(".cs") ||
                            (Directory.Exists(e.FullPath) && e.ChangeType == WatcherChangeTypes.Deleted))
                        {
                            needsGameRecompile = true;
                        }
                        else if (Directory.Exists(e.FullPath) == false &&
                            excludedStagingRefreshExtensions.Any(x => e.FullPath.EndsWith(x)) == false)
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
                if (refreshingAssets == false)
                {
                    if (e.FullPath.EndsWith(".cs") ||
                        (Directory.Exists(e.FullPath) && e.ChangeType == WatcherChangeTypes.Renamed))
                    {
                        needsGameRecompile = true;
                    }
                    else if (Directory.Exists(e.FullPath) == false &&
                        excludedStagingRefreshExtensions.Any(x => e.FullPath.EndsWith(x)) == false)
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
                if ((lastOpenScene?.Length ?? 0) > 0)
                {
                    var scene = ResourceManager.instance.LoadRawSceneFromPath(lastOpenScene);

                    Scene.SetActiveScene(scene);

                    ResetScenePhysics(false);
                }
                else
                {
                    Scene.SetActiveScene(null);

                    ResetScenePhysics(false);
                }

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

                csProjManager.CollectGameScriptModifyStates();

                UpdateWindowTitle();
            });
        });
    }

    /// <summary>
    /// Updates the current window title with the project name and active scene
    /// </summary>
    private void UpdateWindowTitle()
    {
        if((lastOpenScene?.Length ?? 0) > 0)
        {
            window.Title = $"Staple Editor - {Path.GetFileName(basePath)} - {Path.GetFileNameWithoutExtension(lastOpenScene)}";
        }
        else
        {
            window.Title = $"Staple Editor - {Path.GetFileName(basePath)}";
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

        projectBrowser.UpdateProjectBrowserNodes();
    }

    /// <summary>
    /// Refreshes the assets cache and optionally updates the C# project
    /// </summary>
    /// <param name="platform">The current platform</param>
    /// <param name="onFinish">Callback when finished</param>
    /// <param name="updateProject">Whether to update the project</param>
    public void RefreshStaging(AppPlatform platform, Action onFinish, bool updateProject = true)
    {
        if(gameLoadDisabled || refreshingAssets)
        {
            return;
        }

        RecordScene();

        lock (backgroundLock)
        {
            refreshingAssets = true;
            needsRefreshStaging = false;
        }

        projectBrowser.UpdateProjectBrowserNodes();

        projectBrowser.CreateMissingMetaFiles();

        var bakerPath = Path.Combine(Storage.StapleBasePath, "Tools", "bin", "Baker");

        void Finish()
        {
            if (projectAppSettings == null)
            {
                lock (backgroundLock)
                {
                    refreshingAssets = false;
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

                                case RendererType.Direct3D12:

                                    rendererParameters.Add("-r d3d12");

                                    break;

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

                        var args = $"-i \"{basePath}/Assets\" -o \"{basePath}/Cache/Staging/{platform}\" -platform {platform} -editor {string.Join(" ", rendererParameters)}".Replace("\\", "/");

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

                        ThreadHelper.Dispatch(() =>
                        {
                            ResourceManager.instance.Clear();

                            AssetDatabase.Reload();
                            projectBrowser.UpdateProjectBrowserNodes();

                            if(LoadRecordedScene() == false)
                            {
                                if ((lastOpenScene?.Length ?? 0) > 0)
                                {
                                    var scene = ResourceManager.instance.LoadRawSceneFromPath(lastOpenScene);

                                    Scene.SetActiveScene(scene);
                                }
                                else
                                {
                                    Scene.SetActiveScene(null);
                                }
                            }

                            ResetScenePhysics(false);
                        });

                        lock (backgroundLock)
                        {
                            refreshingAssets = false;
                        }
                    }
                }
                catch (Exception)
                {
                    lock (backgroundLock)
                    {
                        refreshingAssets = false;
                    }
                }

                ThreadHelper.Dispatch(() =>
                {
                    try
                    {
                        if(updateProject)
                        {
                            UnloadGame();
                            LoadGame();
                        }

                        onFinish?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                });
            }));

            StartBackgroundTask(handle);
        }

        if (updateProject)
        {
            UpdateCSProj(platform, Finish);
        }
        else
        {
            Finish();
        }
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
            assetPath = Path.Combine(instance.basePath, "Assets", assetPath);
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
            var guidField = assetInstance.GetType().GetProperty("Guid");
            var guid = Guid.NewGuid().ToString();

            if (guidField != null && guidField.PropertyType == typeof(string) && guidField.GetValue(assetInstance) != null)
            {
                guid = (string)guidField.GetValue(assetInstance);
            }

            var serialized = AssetSerialization.Serialize(assetInstance, true);

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