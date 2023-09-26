using Microsoft.Build.Evaluation;
using System;
using System.Diagnostics;
using System.IO;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void BuildPlayer(AppPlatform platform, string outPath, bool debug)
        {
            lock(backgroundLock)
            {
                progressFraction = 0;
            }

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", platform.ToString());
            var assetsCacheDirectory = Path.Combine(basePath, "Cache", "Staging", platform.ToString());
            var projectPath = Path.Combine(projectDirectory, "Player.csproj");
            var configurationName = debug ? "Debug" : "Release";

            csProjManager.GeneratePlayerCSProj(platform, projectAppSettings, debug);

            RefreshStaging(platform, false);

            lock (backgroundLock)
            {
                progressFraction = 0.1f;
            }

            string targetResourcesPath;

            switch (platform)
            {
                case AppPlatform.Android:

                    targetResourcesPath = Path.Combine(projectDirectory, "Assets");

                    break;

                case AppPlatform.Windows:
                case AppPlatform.Linux:
                case AppPlatform.MacOSX:

                    targetResourcesPath = Path.Combine(outPath, "Data");

                    break;

                default:

                    targetResourcesPath = Path.Combine(outPath, "Data");

                    break;
            }

            try
            {
                Directory.CreateDirectory(targetResourcesPath);
            }
            catch (Exception)
            {
            }

            void CopyDirectory(string sourceDir, string destinationDir)
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

            var baseResourcesPath = Path.Combine(StapleBasePath, "DefaultResources");

            try
            {
                var defaultResourcesPath = Path.Combine(baseResourcesPath, $"DefaultResources-{platform}.pak");

                if (File.Exists(defaultResourcesPath) == false)
                {
                    Log.Error($"Failed to build player: Missing DefaultResources-{platform} pak file");

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

            var redistPath = Path.Combine(StapleBasePath, "Dependencies", "Redist", configurationName, buildPlatform.ToString());

            if(Directory.Exists(redistPath))
            {
                var dependencies = Directory.GetFiles(redistPath);

                foreach (var file in dependencies)
                {
                    if (file == null)
                    {
                        continue;
                    }

                    try
                    {
                        switch(platform)
                        {
                            case AppPlatform.Android:

                                File.Copy(file, Path.Combine(projectDirectory, "lib", "arm64-v8a", file.Replace($"{redistPath}{Path.DirectorySeparatorChar}", "")), true);

                                break;

                            default:

                                File.Copy(file, Path.Combine(outPath, file.Replace($"{redistPath}{Path.DirectorySeparatorChar}", "")), true);

                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to build player: {e}");

                        return;
                    }
                }
            }

            var platformRuntime = "";

            switch (platform)
            {
                case AppPlatform.Windows:

                    platformRuntime = "win-x64";

                    break;

                case AppPlatform.Linux:

                    platformRuntime = "linux-x64";

                    break;

                case AppPlatform.MacOSX:

                    platformRuntime = "osx-x64";

                    break;

                case AppPlatform.Android:

                    platformRuntime = "android-arm64";

                    break;

                case AppPlatform.iOS:

                    platformRuntime = "ios-arm64";

                    break;
            }

            switch(platform)
            {
                case AppPlatform.Windows:
                case AppPlatform.Linux:
                case AppPlatform.MacOSX:

                    args = $" publish -r {platformRuntime} \"{projectPath}\" -c {configurationName} -o \"{outPath}\" --self-contained";

                    break;

                case AppPlatform.Android:

                    args = $" build \"{projectPath}\" -c {configurationName} -o \"{outPath}\" -p:TargetFramework=net7.0-android";

                    break;

                case AppPlatform.iOS:

                    args = $" build \"{projectPath}\" -c {configurationName} -o \"{outPath}\" -p:TargetFramework=net7.0-ios";

                    break;

                default:

                    args = $" publish -r {platformRuntime} \"{projectPath}\" -c {configurationName} -o \"{outPath}\" --self-contained";

                    break;
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

            lock (backgroundLock)
            {
                progressFraction = 1;
            }
        }

        public void BuildGame()
        {
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
