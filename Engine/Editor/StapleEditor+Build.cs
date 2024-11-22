using Microsoft.Build.Evaluation;
using System;
using System.Diagnostics;
using System.IO;
using Staple.Internal;
using System.Collections.Generic;
using Newtonsoft.Json;
using Staple.Jobs;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private class GameBuildJob(string basePath, Action onFinish) : IJob
    {
        public string basePath = basePath;
        public Action onFinish = onFinish;

        public void Execute()
        {
            void Finish()
            {
                ThreadHelper.Dispatch(() =>
                {
                    try
                    {
                        onFinish?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                });
            }

            try
            {
                using var collection = new ProjectCollection();

                var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", "Game");
                var projectPath = Path.Combine(projectDirectory, "Game.csproj");
                var outPath = Path.Combine(projectDirectory, "bin");

                try
                {
                    Directory.Delete(outPath, true);
                }
                catch (Exception)
                {
                }

                var args = $" build \"{projectPath}\" -c Debug -o \"{outPath}\"";

                var processInfo = new ProcessStartInfo("dotnet", args)
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Environment.CurrentDirectory
                };

                var process = new Process
                {
                    StartInfo = processInfo
                };

                Staple.Tooling.Utilities.ExecuteAndCollectProcess(process);

                Finish();
            }
            catch(Exception e)
            {
                Log.Error($"Error while building game: {e}");

                Finish();
            }
        }
    }

    /// <summary>
    /// Builds the game (Player)
    /// </summary>
    /// <param name="backend">The backend to use</param>
    /// <param name="outPath">The output path</param>
    /// <param name="debug">Whether to make a debug build</param>
    /// <param name="nativeAOT">Whether to build natively</param>
    /// <param name="assetsOnly">Whether to just pack and copy assets</param>
    public void BuildPlayer(PlayerBackend backend, string outPath, bool debug, bool nativeAOT, bool debugRedists, bool assetsOnly)
    {
        lock (backgroundLock)
        {
            progressFraction = 0;
        }

        void ShowFailureMessage()
        {
            ShowMessageBox("Failed to build player, please check the build log", "OK", null);
        }

        var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", backend.platform.ToString());
        var assetsCacheDirectory = Path.Combine(basePath, "Cache", "Staging", backend.platform.ToString());
        var projectPath = Path.Combine(projectDirectory, "Player.csproj");
        var configurationName = debug ? "Debug" : "Release";
        var redistConfigurationName = debugRedists ? "Debug" : "Release";
        var targetResourcesPath = Path.Combine(backend.dataDirIsOutput ? outPath : projectDirectory, backend.dataDir);

        try
        {
            Directory.CreateDirectory(projectDirectory);
        }
        catch(Exception)
        {
        }

        try
        {
            Directory.CreateDirectory(assetsCacheDirectory);
        }
        catch (Exception)
        {
        }

        try
        {
            Directory.CreateDirectory(targetResourcesPath);
        }
        catch (Exception)
        {
        }

        var buildInfo = new BuildInfo(basePath, projectDirectory, outPath, assetsCacheDirectory, targetResourcesPath,
            Path.Combine(backend.basePath, "Resources"), backend.platform, projectAppSettings.Clone());

        var preprocessors = TypeCache.AllTypesSubclassingOrImplementing<IBuildPreprocessor>();
        var postprocessors = TypeCache.AllTypesSubclassingOrImplementing<IBuildPostprocessor>();

        foreach(var processor in preprocessors)
        {
            try
            {
                var instance = (IBuildPreprocessor)Activator.CreateInstance(processor);

                if(instance.OnPreprocessBuild(buildInfo) == BuildProcessorResult.Failed)
                {
                    throw new Exception("Build Preprocessor failed. Please check your logs for details.");
                }
            }
            catch(Exception e)
            {
                Log.Error($"Failed to execute Build Preprocessor {processor.FullName}: {e}");

                ShowFailureMessage();

                return;
            }
        }

        csProjManager.GeneratePlayerCSProj(backend, projectAppSettings, debug, nativeAOT, debugRedists);

        RefreshStaging(backend.platform, () =>
        {
            try
            {
                File.Copy(Path.Combine(basePath, "Settings", "Icon.png"), Path.Combine(assetsCacheDirectory, "StapleAppIcon.png"), true);
            }
            catch (Exception)
            {
            }

            lock (backgroundLock)
            {
                progressFraction = 0.1f;
            }

            try
            {
                Directory.CreateDirectory(targetResourcesPath);
            }
            catch (Exception)
            {
            }

            var baseResourcesPath = Path.Combine(StapleBasePath, "DefaultResources");

            try
            {
                var defaultResourcesPath = Path.Combine(baseResourcesPath, $"DefaultResources-{backend.platform}.pak");

                if (File.Exists(defaultResourcesPath) == false)
                {
                    Log.Error($"Failed to build player: Missing DefaultResources-{backend.platform} pak file");

                    ShowFailureMessage();

                    return;
                }

                try
                {
                    File.Copy(defaultResourcesPath, Path.Combine(targetResourcesPath, "DefaultResources.pak"), true);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to build player: {e}");

                    ShowFailureMessage();

                    return;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to build player: {e}");

                ShowFailureMessage();

                return;
            }

            if (backend.dataDirIsOutput)
            {
                if (CSProjManager.CopyModuleRedists(Path.Combine(outPath, backend.redistOutput), projectAppSettings, backend.basePath,
                    redistConfigurationName) == false)
                {
                    Log.Error($"Failed to build player: Failed to copy redistributable files");

                    ShowFailureMessage();

                    return;
                }

                if (EditorUtils.CopyDirectory(Path.Combine(backend.basePath, "Redist", redistConfigurationName),
                    Path.Combine(outPath, backend.redistOutput)) == false)
                {
                    Log.Error($"Failed to build player: Failed to copy redistributable files");

                    ShowFailureMessage();

                    return;
                }
            }

            lock (backgroundLock)
            {
                progressFraction = 0.6f;
            }

            List<string> CollectPakNames()
            {
                var outValue = new List<string>();

                void Recursive(string path)
                {
                    try
                    {
                        var directories = Directory.GetDirectories(path);

                        foreach(var directory in directories)
                        {
                            try
                            {
                                if (File.Exists($"{directory}.meta"))
                                {
                                    var json = File.ReadAllText($"{directory}.meta");

                                    var folderAsset = JsonConvert.DeserializeObject<FolderAsset>(json);

                                    if ((folderAsset?.pakName?.Length ?? 0) > 0)
                                    {
                                        outValue.Add(folderAsset.pakName);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }

                            Recursive(directory);
                        }
                    }
                    catch(Exception)
                    {
                    }
                }

                Recursive(Path.Combine(basePath, "Assets"));

                return outValue;
            }

            bool PreparePak(string name)
            {
                var args = $"-p -r -i \"{assetsCacheDirectory}\" -o \"{Path.Combine(targetResourcesPath, $"{name}.pak")}\"";

                var processInfo = new ProcessStartInfo(Path.Combine(Storage.StapleBasePath, "Tools", "bin", "Packer"), args)
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Environment.CurrentDirectory
                };

                var process = new Process
                {
                    StartInfo = processInfo
                };

                Staple.Tooling.Utilities.ExecuteAndCollectProcess(process);

                if ((process.HasExited && process.ExitCode == 0) == false)
                {
                    return false;
                }

                return true;
            }

            var pakNames = CollectPakNames();

            foreach (var name in pakNames)
            {
                if (PreparePak(name) == false)
                {
                    Log.Error($"Failed to build player: Unable to pack resources");

                    ShowFailureMessage();

                    return;
                }
            }

            if (PreparePak("Resources") == false)
            {
                Log.Error($"Failed to build player: Unable to pack resources");

                ShowFailureMessage();

                return;
            }

            if (assetsOnly)
            {
                return;
            }

            string args = "";

            if (backend.publish)
            {
                args = $" publish -r {backend.platformRuntime} \"{projectPath}\" -c {configurationName} -o \"{outPath}\" --self-contained -p:UseAppHost=true";
            }
            else
            {
                args = $" build \"{projectPath}\" -c {configurationName} -o \"{outPath}\" -p:TargetFramework={backend.framework}";
            }

            var processInfo = new ProcessStartInfo("dotnet", args)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Environment.CurrentDirectory
            };

            var process = new Process
            {
                StartInfo = processInfo
            };

            Log.Debug($"[Build] dotnet {args}");

            Staple.Tooling.Utilities.ExecuteAndCollectProcess(process);

            if ((process.HasExited && process.ExitCode == 0) == false)
            {
                Log.Error($"Failed to build player: Unable to complete build");

                ShowFailureMessage();

                return;
            }

            try
            {
                var projectExtension = backend.platform switch
                {
                    AppPlatform.Windows => ".exe",
                    _ => "",
                };

                File.Move(Path.Combine(outPath, $"Player{projectExtension}"), Path.Combine(outPath, $"{Path.GetFileName(outPath)}{projectExtension}"), true);

                if (debug)
                {
                    File.Move(Path.Combine(outPath, $"Player.pdb"), Path.Combine(outPath, $"{Path.GetFileName(outPath)}.pdb"), true);
                }
                else
                {
                    var pdbs = Directory.GetFiles(outPath, "*.pdb");

                    foreach (var file in pdbs)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception)
            {
            }

            foreach (var processor in postprocessors)
            {
                try
                {
                    var instance = (IBuildPostprocessor)Activator.CreateInstance(processor);

                    if (instance.OnPostprocessBuild(buildInfo) == BuildProcessorResult.Failed)
                    {
                        throw new Exception($"Build Postprocessor failed. Please check your logs for details");
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to execute Build Postprocessor {processor.FullName}: {e}");

                    ShowFailureMessage();
                }
            }

            lock (backgroundLock)
            {
                progressFraction = 1;
            }

            ShowMessageBox("Player built successfully!", "OK", null);
        },
        false);
    }

    /// <summary>
    /// Builds the game assembly
    /// </summary>
    public void BuildGame(Action onFinish)
    {
        if (gameLoadDisabled)
        {
            onFinish();

            return;
        }

        buildingGame = true;

        var finished = false;

        var handle = JobScheduler.Schedule(new GameBuildJob(basePath, () =>
        {
            finished = true;
        }));

        IEnumerator<(bool, string, float)> Handle()
        {
            yield return (false, "Building game...", 0);

            while(finished == false)
            {
                yield return (false, "Building game...", 0);
            }

            ThreadHelper.Dispatch(() =>
            {
                try
                {
                    onFinish?.Invoke();
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }

                buildingGame = false;
            });

            yield return (true, "", 1);
        }

        StartBackgroundTask(Handle());
    }
}
