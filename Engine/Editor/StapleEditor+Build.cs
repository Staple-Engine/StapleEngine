using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Staple.Editor
{
    partial class StapleEditor
    {
        public void BuildPlayer()
        {
            using var collection = new ProjectCollection();

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly");
            var assetsCacheDirectory = Path.Combine(basePath, "Cache", "Staging");
            var projectPath = Path.Combine(projectDirectory, "Player.csproj");
            var outPath = Path.Combine(projectDirectory, "publish");

            var args = $" publish -r win-x64 \"{projectPath}\" -c Release -o \"{outPath}\" --self-contained";

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

            var higherDir = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).ToList();

            while (higherDir.Count > 0 && higherDir.LastOrDefault() != "StapleEngine")
            {
                higherDir.RemoveAt(higherDir.Count - 1);
            }

            if ((process.HasExited && process.ExitCode == 0) == false)
            {
                Log.Error($"Failed to build player: Unable to complete native build");

                return;
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

            var baseResourcesPath = Path.Combine(string.Join(Path.DirectorySeparatorChar, higherDir), "DefaultResources");

            try
            {
                var directories = Directory.GetDirectories(baseResourcesPath);

                foreach (var directory in directories)
                {
                    CopyDirectory(directory, Path.Combine(outPath, "Data", Path.GetFileName(directory)));
                }

                var files = Directory.GetFiles(baseResourcesPath);

                foreach (var file in files)
                {
                    try
                    {
                        File.Copy(file, Path.Combine(outPath, "Data", Path.GetFileName(file)), true);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to build player: {e}");

                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to build player: {e}");

                return;
            }

            try
            {
                var directories = Directory.GetDirectories(assetsCacheDirectory);

                foreach (var directory in directories)
                {
                    CopyDirectory(directory, Path.Combine(outPath, "Data", Path.GetFileName(directory)));
                }

                var files = Directory.GetFiles(assetsCacheDirectory);

                foreach (var file in files)
                {
                    try
                    {
                        File.Copy(file, Path.Combine(outPath, "Data", Path.GetFileName(file)), true);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to build player: {e}");

                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to build player: {e}");

                return;
            }

            var baseStagingPath = Path.Combine(string.Join(Path.DirectorySeparatorChar, higherDir), "Staging");

            var dependencies = new string[]
            {
                Directory.GetFiles(baseStagingPath, "bgfx.*").FirstOrDefault(),
                Directory.GetFiles(baseStagingPath, "glfw.*").FirstOrDefault(),
                Directory.GetFiles(baseStagingPath, "joltc.*").FirstOrDefault(),
            };

            foreach (var file in dependencies)
            {
                if (file == null)
                {
                    continue;
                }

                try
                {
                    File.Copy(file, Path.Combine(outPath, file.Replace($"{baseStagingPath}{Path.DirectorySeparatorChar}", "")), true);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to build player: {e}");

                    return;
                }
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
