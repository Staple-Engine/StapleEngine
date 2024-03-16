using Microsoft.Build.Evaluation;
using System;
using System.Diagnostics;
using System.IO;

namespace Staple.Editor;

internal partial class StapleEditor
{
    /// <summary>
    /// Builds the game (Player)
    /// </summary>
    /// <param name="backend">The backend to use</param>
    /// <param name="outPath">The output path</param>
    /// <param name="debug">Whether to make a debug build</param>
    /// <param name="nativeAOT">Whether to build natively</param>
    /// <param name="assetsOnly">Whether to just pack and copy assets</param>
    public void BuildPlayer(PlayerBackend backend, string outPath, bool debug, bool nativeAOT, bool assetsOnly)
    {
        lock (backgroundLock)
        {
            progressFraction = 0;
        }

        var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", backend.platform.ToString());
        var assetsCacheDirectory = Path.Combine(basePath, "Cache", "Staging", backend.platform.ToString());
        var projectPath = Path.Combine(projectDirectory, "Player.csproj");
        var configurationName = debug ? "Debug" : "Release";
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

                return;
            }
        }

        csProjManager.GeneratePlayerCSProj(backend, projectAppSettings, debug, nativeAOT);

        RefreshStaging(backend.platform, false);

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

                return;
            }

            try
            {
                File.Copy(defaultResourcesPath, Path.Combine(targetResourcesPath, "DefaultResources.pak"), true);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to build player: {e}");

                return;
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to build player: {e}");

            return;
        }

        if (backend.dataDirIsOutput)
        {
            if(EditorUtils.CopyDirectory(Path.Combine(backend.basePath, "Redist", configurationName), Path.Combine(outPath, backend.redistOutput)) == false)
            {
                Log.Error($"Failed to build player: Failed to copy redistributable files");

                return;
            }
        }

        lock (backgroundLock)
        {
            progressFraction = 0.6f;
        }

        var args = $"-p -i \"{assetsCacheDirectory}\" -o \"{Path.Combine(targetResourcesPath, "Resources.pak")}\"";

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

        if (process.Start())
        {
            while (process.HasExited == false)
            {
                var line = process.StandardOutput.ReadLine();

                if (line != null)
                {
                    Log.Info(line);
                }
            }
        }

        if ((process.HasExited && process.ExitCode == 0) == false)
        {
            Log.Error($"Failed to build player: Unable to pack resources");

            return;
        }

        if(assetsOnly)
        {
            return;
        }

        if(backend.publish)
        {
            args = $" publish -r {backend.platformRuntime} \"{projectPath}\" -c {configurationName} -o \"{outPath}\" --self-contained -p:UseAppHost=true";
        }
        else
        {
            args = $" build \"{projectPath}\" -c {configurationName} -o \"{outPath}\" -p:TargetFramework={backend.framework}";
        }

        processInfo = new ProcessStartInfo("dotnet", args)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Environment.CurrentDirectory
        };

        process = new Process
        {
            StartInfo = processInfo
        };

        Log.Debug($"[Build] dotnet {args}");

        if (process.Start())
        {
            while (process.HasExited == false)
            {
                var line = process.StandardOutput.ReadLine();

                if (line != null)
                {
                    Log.Info(line);
                }
            }
        }

        if ((process.HasExited && process.ExitCode == 0) == false)
        {
            Log.Error($"Failed to build player: Unable to complete build");

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

            if(debug)
            {
                File.Move(Path.Combine(outPath, $"Player.pdb"), Path.Combine(outPath, $"{Path.GetFileName(outPath)}.pdb"), true);
            }
            else
            {
                var pdbs = Directory.GetFiles(outPath, "*.pdb");

                foreach(var file in pdbs)
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

                if(instance.OnPostprocessBuild(buildInfo) == BuildProcessorResult.Failed)
                {
                    throw new Exception($"Build Postprocessor failed. Please check your logs for details");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to execute Build Postprocessor {processor.FullName}: {e}");
            }
        }

        lock (backgroundLock)
        {
            progressFraction = 1;
        }
    }

    /// <summary>
    /// Builds the game assembly
    /// </summary>
    public void BuildGame()
    {
        if (gameLoadDisabled)
        {
            return;
        }

        using var collection = new ProjectCollection();

        var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", "Game");
        var projectPath = Path.Combine(projectDirectory, "Game.csproj");
        var outPath = Path.Combine(projectDirectory, "bin");

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

        if (process.Start())
        {
            while (process.HasExited == false)
            {
                var line = process.StandardOutput.ReadLine();

                if (line != null)
                {
                    Log.Info(line);
                }
            }
        }
    }
}
