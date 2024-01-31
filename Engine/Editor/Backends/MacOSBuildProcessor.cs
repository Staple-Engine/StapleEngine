using System.IO;
using System;
using System.Linq;

namespace Staple.Editor;

internal class MacOSBuildProcessor : IBuildPostprocessor
{
    public void OnPostprocessBuild(BuildInfo buildInfo)
    {
        if (buildInfo.platform != AppPlatform.MacOSX)
        {
            return;
        }

        var outPath = buildInfo.outPath;

        var appName = Path.GetFileName(outPath);

        var appPath = Path.Combine(outPath, $"{appName}.app");

        try
        {
            Directory.CreateDirectory(appPath);
        }
        catch (Exception)
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
    <string>Staple Engine - {{appName}} {{buildInfo.projectAppSettings.appDisplayVersion}}</string>
    <key>CFBundleIconFile</key>
    <string>Icon.icns</string>
    <key>CFBundleIdentifier</key>
    <string>{{buildInfo.projectAppSettings.appBundleID}}</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>{{buildInfo.projectAppSettings.appName}}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>{{buildInfo.projectAppSettings.appDisplayVersion}}</string>
    <key>CFBundleSupportedPlatforms</key>
    <array>
      <string>MacOSX</string>
    </array>
    <key>CFBundleVersion</key>
    <string>{{buildInfo.projectAppSettings.appVersion}}</string>
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
        catch (Exception e)
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

            if (OperatingSystem.IsLinux() ||
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
            var dataFiles = Directory.GetFiles(buildInfo.targetResourcesPath);

            foreach (var file in dataFiles)
            {
                File.Move(file, Path.Combine(appPath, "Contents", "MacOS", "Data", Path.GetFileName(file)), true);
            }
        }
        catch (Exception e)
        {
            Log.Debug(e.ToString());
        }

        try
        {
            var frameworks = Directory.GetFiles(outPath, "*.dylib")
                .Concat(Directory.GetFiles(outPath, "*.dll"))
                .Concat(Directory.GetFiles(outPath, "*.pdb"));

            foreach (var framework in frameworks)
            {
                File.Move(framework, Path.Combine(appPath, "Contents", "MacOS", Path.GetFileName(framework)), true);
            }

            File.Move(Path.Combine(outPath, appName), Path.Combine(appPath, "Contents", "MacOS", appName), true);
        }
        catch (Exception e)
        {
            Log.Debug(e.ToString());
        }
    }
}
