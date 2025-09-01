using Microsoft.Build.Evaluation;
using System;
using System.Diagnostics;
using System.IO;
using Staple.Internal;
using Staple.Jobs;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Staple.ProjectManagement;

public partial class ProjectManager
{
    /// <summary>
    /// Builds the game (Player)
    /// </summary>
    /// <param name="backend">The backend to use</param>
    /// <param name="projectAppSettings">The project's app settings</param>
    /// <param name="outPath">The output path</param>
    /// <param name="debug">Whether to make a debug build</param>
    /// <param name="nativeAOT">Whether to build natively</param>
    /// <param name="debugRedists">Whether to use debug redistributables</param>
    /// <param name="assetsOnly">Whether to just pack and copy assets</param>
    /// <param name="publishSingleFile">Whether to build into a single file</param>
    public void BuildPlayer(PlayerBackend backend, AppSettings projectAppSettings, string outPath, bool debug, bool nativeAOT,
        bool debugRedists, bool assetsOnly, bool publishSingleFile, Action<float, string> progress, Action<string> failure,
        Action<AppPlatform, Action> refreshStaging)
    {
        progress?.Invoke(0, "Building...");

        void ShowFailureMessage()
        {
            failure?.Invoke("Failed to build player, please check the build log");
        }

        var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", backend.platform.ToString());
        var assetsCacheDirectory = Path.Combine(basePath, "Cache", "Staging", backend.platform.ToString());
        var projectPath = Path.Combine(projectDirectory, "Player.sln");
        var configurationName = debug ? "Debug" : "Release";
        var redistConfigurationName = debugRedists ? "Debug" : "Release";
        var targetResourcesPath = Path.Combine(backend.dataDirIsOutput ? outPath : Path.Combine(projectDirectory, "Player"), backend.dataDir);

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

        var buildInfo = new BuildInfo(basePath, Path.Combine(projectDirectory, "Player"), outPath, assetsCacheDirectory,
            targetResourcesPath, Path.Combine(backend.basePath, "Resources"), backend.platform, projectAppSettings.Clone());

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

        GeneratePlayerCSProj(backend, projectAppSettings, debug, nativeAOT, debugRedists, publishSingleFile);

        refreshStaging?.Invoke(backend.platform, () =>
        {
            try
            {
                var data = File.ReadAllBytes(Path.Combine(basePath, "Settings", "Icon.png"));

                var rawData = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);

                if(rawData != null)
                {
                    if(rawData.width > 256 || rawData.height > 256)
                    {
                        var aspect = rawData.width / (float)rawData.height;

                        rawData.Resize((int)(256 * aspect), 256);
                    }

                    File.WriteAllBytes(Path.Combine(assetsCacheDirectory, "StapleAppIcon.png"), rawData.EncodePNG());
                }
            }
            catch (Exception)
            {
            }

            progress?.Invoke(0.1f, "Refreshing Staging...");

            try
            {
                Directory.CreateDirectory(targetResourcesPath);
            }
            catch (Exception)
            {
            }

            var baseResourcesPath = Path.Combine(stapleBasePath, "DefaultResources");

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
                if (CopyModuleRedists(Path.Combine(outPath, backend.redistOutput), projectAppSettings, backend.platform, backend.basePath,
                    redistConfigurationName) == false)
                {
                    Log.Error($"Failed to build player: Failed to copy redistributable files");

                    ShowFailureMessage();

                    return;
                }

                if (StorageUtils.CopyDirectory(Path.Combine(backend.basePath, "Redist", redistConfigurationName),
                    Path.Combine(outPath, backend.redistOutput)) == false)
                {
                    Log.Error($"Failed to build player: Failed to copy redistributable files");

                    ShowFailureMessage();

                    return;
                }
            }

            progress?.Invoke(0.6f, "Packing files...");

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

                Staple.Tooling.Utilities.ExecuteAndCollectProcess(process, null);

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

            progress?.Invoke(0.8f, "Compiling...");

            var args = $"restore \"{projectPath}\"";

            var processInfo = new ProcessStartInfo("dotnet", args)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = projectDirectory,
            };

            var process = new Process
            {
                StartInfo = processInfo
            };

            Log.Debug($"[Build] dotnet {args}");

            Staple.Tooling.Utilities.ExecuteAndCollectProcess(process, null);

            if ((process.HasExited && process.ExitCode == 0) == false)
            {
                Log.Error($"Failed to build player: Unable to complete build");

                ShowFailureMessage();

                return;
            }

            args = "";

            if (backend.publish)
            {
                args = $" publish -r {backend.platformRuntime} \"{projectPath}\" -c {configurationName} -o \"{outPath}\"";
            }
            else
            {
                args = $" build \"{projectPath}\" -c {configurationName} -o \"{outPath}\"";
            }

            processInfo = new ProcessStartInfo("dotnet", args)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Environment.CurrentDirectory,
            };

            process = new Process
            {
                StartInfo = processInfo
            };

            Log.Debug($"[Build] dotnet {args}");

            Staple.Tooling.Utilities.ExecuteAndCollectProcess(process, null);

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

            failure?.Invoke("Player built successfully!");
        });
    }

    /// <summary>
    /// Builds the game assembly
    /// </summary>
    public JobHandle BuildGame(Action onFinish, Action<float, string> progress)
    {
        return JobScheduler.Schedule(new ActionJob(() =>
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

            progress?.Invoke(0, "Building game...");

            try
            {
                using var collection = new ProjectCollection();

                var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", "Game");
                var projectPath = Path.Combine(projectDirectory, "Game.sln");
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

                Staple.Tooling.Utilities.ExecuteAndCollectProcess(process, null);

                Finish();
            }
            catch (Exception e)
            {
                Log.Error($"Error while building game: {e}");

                Finish();
            }
        }));
    }
}
