using Microsoft.Build.Evaluation;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void BuildPlayer(AppPlatform platform, string outPath)
        {
            lock(backgroundLock)
            {
                progressFraction = 0;
            }

            using var collection = new ProjectCollection();

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly");
            var assetsCacheDirectory = Path.Combine(basePath, "Cache", "Staging", platform.ToString());
            var projectPath = Path.Combine(projectDirectory, "Player.csproj");

            var platformRuntime = "";

            GeneratePlayerCSProj(platform);

            lock (backgroundLock)
            {
                progressFraction = 0.1f;
            }

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

            var args = $" publish -r {platformRuntime} \"{projectPath}\" -c Release -o \"{outPath}\" --self-contained";

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

            if ((process.HasExited && process.ExitCode == 0) == false)
            {
                Log.Error($"Failed to build player: Unable to complete native build");

                return;
            }

            lock (backgroundLock)
            {
                progressFraction = 0.5f;
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(outPath, "Data"));
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

            var assetsDirectory = Path.Combine(basePath, "Assets");

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
                    File.Copy(defaultResourcesPath, Path.Combine(outPath, "Data", "DefaultResources.pak"), true);
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

            args = $"-p -i \"{assetsCacheDirectory}\" -o {Path.Combine(outPath, "Data", "Resources.pak")}";

            processInfo = new ProcessStartInfo(Path.Combine(Storage.StapleBasePath, "Tools", "bin", "Packer"), args)
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
                Log.Error($"Failed to build player: Unable to pack resources");

                return;
            }

            var redistPath = Path.Combine(StapleBasePath, "Dependencies", "Redist", buildPlatform.ToString());

            var dependencies = new string[]
            {
                Directory.GetFiles(redistPath, "*bgfx.*").FirstOrDefault(),
                Directory.GetFiles(redistPath, "*glfw.*").FirstOrDefault(),
                Directory.GetFiles(redistPath, "*joltc.*").FirstOrDefault(),
            };

            foreach (var file in dependencies)
            {
                if (file == null)
                {
                    continue;
                }

                try
                {
                    File.Copy(file, Path.Combine(outPath, file.Replace($"{redistPath}{Path.DirectorySeparatorChar}", "")), true);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to build player: {e}");

                    return;
                }
            }

            lock (backgroundLock)
            {
                progressFraction = 1;
            }
        }

        public void BuildGame()
        {
            using var collection = new ProjectCollection();

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly");
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
