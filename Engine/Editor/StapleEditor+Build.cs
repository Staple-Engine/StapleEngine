using Microsoft.Build.Evaluation;
using Staple.Internal;
using System;
using System.Diagnostics;
using System.IO;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();

            try
            {
                Directory.CreateDirectory(destinationDir);
            }
            catch (Exception)
            {
            }

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);

                try
                {
                    file.CopyTo(targetFilePath, true);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to build player: {e}");

                    return;
                }
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);

                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        public void BuildPlayer(PlayerBackend backend, string outPath, bool debug, bool nativeAOT)
        {
            lock (backgroundLock)
            {
                progressFraction = 0;
            }

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", backend.platform.ToString());
            var assetsCacheDirectory = Path.Combine(basePath, "Cache", "Staging", backend.platform.ToString());
            var projectPath = Path.Combine(projectDirectory, "Player.csproj");
            var configurationName = debug ? "Debug" : "Release";

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

            string targetResourcesPath = Path.Combine(backend.dataDirIsOutput ? outPath : projectDirectory, backend.dataDir);

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
                CopyDirectory(Path.Combine(backend.basePath, "Redist", configurationName), Path.Combine(outPath, backend.redistOutput));
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

            if(backend.publish)
            {
                args = $" publish -r {backend.platformRuntime} \"{projectPath}\" -c {configurationName} -o \"{outPath}\" --self-contained";
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
            }
            catch (Exception)
            {
            }

            lock (backgroundLock)
            {
                progressFraction = 1;
            }
        }

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
}
