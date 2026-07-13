using Newtonsoft.Json;
using Staple.Internal;
using Staple.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private static Regex stagingProgressRegex = StagingProgressRegex();

    [GeneratedRegex("\\[(.*?)\\/(.*?)\\]((.|\\n)*)")]
    private static partial Regex StagingProgressRegex();

    private static readonly List<string> excludedStagingRefreshExtensions =
    [
        AssetSerialization.SceneExtension,
    ];

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

        if (scene != null)
        {
            Physics3D.Instance.DestroyAllBodies();

            Scene.SetActiveScene(scene);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Refreshes the current assets and optionally updates the C# project
    /// </summary>
    /// <param name="onFinish">Callback when finished</param>
    /// <param name="updateProject">Whether to update the project</param>
    public void RefreshAssets(bool updateProject, Action onFinish)
    {
        QueryChangedAssets(currentPlatform, (assets) =>
        {
            RefreshStaging(currentPlatform, () =>
            {
                RefreshChangedAssets(assets);

                onFinish?.Invoke();
            },
            updateProject ? StagingRefreshFlags.UpdateProject | StagingRefreshFlags.CheckBuild : StagingRefreshFlags.CheckBuild);
        });
    }

    /// <summary>
    /// Gets a list of all changed assets
    /// </summary>
    /// <param name="platform">The platform to check</param>
    /// <param name="onFinish">A callback with a list of changed assets</param>
    public void QueryChangedAssets(AppPlatform platform, Action<string[]> onFinish)
    {
        lock (backgroundLock)
        {
            RefreshingAssets = true;
        }

        projectBrowser.UpdateProjectBrowserNodes();

        projectBrowser.CreateMissingMetaFiles(() =>
        {
            var bakerPath = Path.Combine(Storage.StapleBasePath, "Tools", "bin", "Baker");

            if (projectAppSettings == null)
            {
                lock (backgroundLock)
                {
                    RefreshingAssets = false;
                }

                return;
            }

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
                                case RendererType.Direct3D12:

                                    rendererParameters.Add("-r d3d12");

                                    break;

                                case RendererType.Metal:

                                    rendererParameters.Add("-r metal");

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

                        var args = $"-i \"{BasePath}/Assets\" {packageArgs} -o \"{BasePath}/Cache/Staging/{platform}\" " +
                            $"-platform {platform} -editor {string.Join(" ", rendererParameters)} -report-changed".Replace("\\", "/");

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

                        var changes = new List<string>();

                        Staple.Tooling.Utilities.ExecuteAndCollectProcess(process,
                            (m) =>
                            {
                                changes.Add(m);
                            });

                        lock (backgroundLock)
                        {
                            RefreshingAssets = false;
                        }

                        ThreadHelper.Dispatch(() =>
                        {
                            onFinish?.Invoke(changes.ToArray());
                        });
                    }
                }
                catch (Exception)
                {
                    lock (backgroundLock)
                    {
                        RefreshingAssets = false;
                    }

                    ThreadHelper.Dispatch(() =>
                    {
                        onFinish?.Invoke([]);
                    });
                }
            },
            (e) =>
            {
                Log.Error(e);

                onFinish?.Invoke([]);
            }));

            StartBackgroundTask(handle);
        });
    }

    /// <summary>
    /// Attempts to refresh/reload any loaded assets that were just changed
    /// </summary>
    /// <param name="changedAssets">A list of changed assets</param>
    public void RefreshChangedAssets(Span<string> changedAssets)
    {
        foreach (var item in changedAssets)
        {
            if (item.Length <= BasePath.Length)
            {
                continue;
            }

            var assetPath = item.Replace('\\', '/').Substring(BasePath.Length + 1);

            var guid = AssetDatabase.GetAssetGuid(assetPath);

            if (guid == null)
            {
                continue;
            }

            var type = AssetDatabase.GetAssetType(guid);

            switch (type)
            {
                case string s when s == typeof(Material).FullName:

                    ResourceManager.instance.ReloadMaterial(guid);

                    break;

                case string s when s == typeof(Shader).FullName:

                    ResourceManager.instance.ReloadShader(guid);

                    break;

                case string s when s == typeof(ComputeShader).FullName:

                    ResourceManager.instance.ReloadComputeShader(guid);

                    break;

                case string s when s == typeof(Mesh).FullName:

                    ResourceManager.instance.ReloadMeshAsset(guid);

                    break;

                case string s when s == typeof(TextAsset).FullName:

                    ResourceManager.instance.ReloadTextAsset(guid);

                    break;

                case string s when s == typeof(AudioClip).FullName:

                    ResourceManager.instance.ReloadAudioClip(guid);

                    break;

                case string s when s == typeof(FontAsset).FullName:

                    ResourceManager.instance.ReloadFont(guid);

                    break;

                case string s when s == typeof(Texture).FullName:

                    ResourceManager.instance.ReloadTexture(guid);

                    break;
            }
        }
    }


    /// <summary>
    /// Refreshes the assets cache and optionally updates the C# project
    /// </summary>
    /// <param name="platform">The current platform</param>
    /// <param name="onFinish">Callback when finished</param>
    /// <param name="flags">Flags for how the refresh should behave</param>
    public void RefreshStaging(AppPlatform platform, Action onFinish, StagingRefreshFlags flags)
    {
        if (editorMode == EditorMode.Normal)
        {
            if (gameLoadDisabled || RefreshingAssets)
            {
                return;
            }

            if(flags.HasFlag(StagingRefreshFlags.ReloadScene))
            {
                RecordScene();
            }

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
                                    case RendererType.Direct3D12:

                                        rendererParameters.Add("-r d3d12");

                                        break;

                                    case RendererType.Metal:

                                        rendererParameters.Add("-r metal");

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
                                if (flags.HasFlag(StagingRefreshFlags.UpdateProject))
                                {
                                    UnloadGame();
                                    LoadGame();
                                }

                                if (flags.HasFlag(StagingRefreshFlags.ReloadScene))
                                {
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
                                }

                                ShowBackgroundProcess();

                                AssetDatabase.Reload(Path.Combine(BasePath, "Cache", "AssetDatabase"),
                                    () =>
                                    {
                                        HideBackgroundProcess();

                                        ThreadHelper.Dispatch(() =>
                                        {
                                            projectBrowser.UpdateProjectBrowserNodes();

                                            var loaded = false;

                                            if (flags.HasFlag(StagingRefreshFlags.ReloadScene))
                                            {
                                                loaded = LoadRecordedScene();
                                            }

                                            if(!loaded && flags.HasFlag(StagingRefreshFlags.LoadLastScene))
                                            {
                                                if ((lastOpenScene?.Length ?? 0) > 0)
                                                {
                                                    Scene scene = null;

                                                    if (sceneMode == SceneMode.Prefab)
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

            if (flags.HasFlag(StagingRefreshFlags.UpdateProject))
            {
                ShowBackgroundProcess();

                AssetDatabase.Reload(Path.Combine(BasePath, "Cache", "AssetDatabase"),
                    () =>
                    {
                        HideBackgroundProcess();

                        UpdateCSProj(platform, flags.HasFlag(StagingRefreshFlags.CheckBuild), false, Finish);
                    });
            }
            else
            {
                Finish();
            }
        });
    }
}
