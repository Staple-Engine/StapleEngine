using Microsoft.Build.Evaluation;
using Staple.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
            }
            catch (Exception)
            {
            }

            if(backend.platform == AppPlatform.MacOSX)
            {
                var appName = Path.GetFileName(outPath);

                var appPath = Path.Combine(outPath, $"{appName}.app");

                try
                {
                    Directory.CreateDirectory(appPath);
                }
                catch(Exception)
                {
                }

                try
                {
                    Directory.CreateDirectory(Path.Combine(appPath, "Contents"));
                }
                catch (Exception)
                {
                }

                try
                {
                    Directory.CreateDirectory(Path.Combine(appPath, "Contents", "MacOS"));
                }
                catch (Exception)
                {
                }

                try
                {
                    Directory.CreateDirectory(Path.Combine(appPath, "Contents", "MacOS", "Data"));
                }
                catch (Exception)
                {
                }

                var info = $$"""
<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>English</string>
    <key>CFBundleExecutable</key>
    <string>Run</string>
    <key>CFBundleGetInfoString</key>
    <string>Staple Engine - {{appName}} {{projectAppSettings.appDisplayVersion}}</string>
    <key>CFBundleIconFile</key>
    <string>Icon.icns</string>
    <key>CFBundleIdentifier</key>
    <string>{{projectAppSettings.appBundleID}}</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>{{projectAppSettings.appName}}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>{{projectAppSettings.appDisplayVersion}}</string>
    <key>CFBundleSupportedPlatforms</key>
    <array>
      <string>MacOSX</string>
    </array>
    <key>CFBundleVersion</key>
    <string>{{projectAppSettings.appVersion}}</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.games</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.13.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSAppTransportSecurity</key>
    <dict>
      <key>NSAllowsArbitraryLoads</key>
      <true />
    </dict>
  </dict>
</plist>
""";

                try
                {
                    File.WriteAllText(Path.Combine(appPath, "Contents", "Info.plist"), info);
                }
                catch(Exception e)
                {
                    Log.Debug(e.ToString());
                }

                var runScript = $$"""
#!/bin/sh

cd $(dirname $BASH_SOURCE)
./{{appName}}
""";

                try
                {
                    var p = Path.Combine(appPath, "Contents", "MacOS", "Run");

                    File.WriteAllText(p, runScript);

                    if(OperatingSystem.IsLinux() ||
                        OperatingSystem.IsMacOS() ||
                        OperatingSystem.IsFreeBSD())
                    {
                        File.SetUnixFileMode(p, UnixFileMode.GroupRead |
                            UnixFileMode.GroupWrite |
                            UnixFileMode.GroupExecute |
                            UnixFileMode.UserRead |
                            UnixFileMode.UserWrite |
                            UnixFileMode.UserExecute |
                            UnixFileMode.OtherRead |
                            UnixFileMode.OtherWrite);
                    }
                }
                catch (Exception e)
                {
                    Log.Debug(e.ToString());
                }

                try
                {
                    var dataFiles = Directory.GetFiles(targetResourcesPath);

                    foreach(var file in dataFiles)
                    {
                        File.Move(file, Path.Combine(appPath, "Contents", "MacOS", "Data", Path.GetFileName(file)), true);
                    }
                }
                catch(Exception e)
                {
                    Log.Debug(e.ToString());
                }

                try
                {
                    var frameworks = Directory.GetFiles(outPath, "*.dylib")
                        .Concat(Directory.GetFiles(outPath, "*.dll"));

                    foreach(var framework in frameworks)
                    {
                        File.Move(framework, Path.Combine(appPath, "Contents", "MacOS", Path.GetFileName(framework)), true);
                    }

                    File.Move(Path.Combine(outPath, appName), Path.Combine(appPath, "Contents", "MacOS", appName), true);
                }
                catch(Exception e)
                {
                    Log.Debug(e.ToString());
                }
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
