using Newtonsoft.Json;
using NfdSharp;
using Staple.Internal;
using Staple.PackageManagement;
using Staple.ProjectManagement;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Staple.Editor;

internal partial class StapleEditor
{
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
                if (!RefreshingAssets)
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
                        else if (!Directory.Exists(e.FullPath) &&
                            !excludedStagingRefreshExtensions.Any(x => e.FullPath.EndsWith(x)) &&
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
                if (!RefreshingAssets)
                {
                    if (e.FullPath.EndsWith(".cs") ||
                        (Directory.Exists(e.FullPath) && e.ChangeType == WatcherChangeTypes.Renamed &&
                        (e.FullPath.StartsWith(Path.Combine(BasePath, "Assets")) ||
                        e.FullPath.StartsWith(Path.Combine(BasePath, "Cache", "Packages")))))
                    {
                        needsGameRecompile = true;
                    }
                    else if (!Directory.Exists(e.FullPath) &&
                        !excludedStagingRefreshExtensions.Any(x => e.FullPath.EndsWith(x)) &&
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
            StagingRefreshFlags.UpdateProject |
            StagingRefreshFlags.CheckBuild |
            StagingRefreshFlags.LoadLastScene);
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
    /// Saves a Staple Asset
    /// </summary>
    /// <param name="assetPath">The asset path</param>
    /// <param name="assetInstance">The asset's instance</param>
    /// <returns>Whether it was saved</returns>
    public static bool SaveAsset(string assetPath, IStapleAsset assetInstance)
    {
        if(!Path.IsPathRooted(assetPath))
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

            if(!existed)
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
}
