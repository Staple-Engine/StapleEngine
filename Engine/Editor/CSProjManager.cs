using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;

namespace Staple.Editor
{
    internal class CSProjManager
    {
        private readonly Dictionary<string, DateTime> fileModifyStates = new();

        private readonly Dictionary<AppPlatform, string[]> platformDefines = new()
        {
            { AppPlatform.Windows, new string[]{ "STAPLE_ENGINE", "STAPLE_WINDOWS" } },
            { AppPlatform.Linux, new string[]{ "STAPLE_ENGINE", "STAPLE_LINUX" } },
            { AppPlatform.MacOSX, new string[]{ "STAPLE_ENGINE", "STAPLE_MACOSX" } },
            { AppPlatform.Android, new string[]{ "STAPLE_ENGINE", "STAPLE_ANDROID" } },
            { AppPlatform.iOS, new string[]{ "STAPLE_ENGINE", "STAPLE_IOS" } },
        };

        private readonly Dictionary<AppPlatform, string> platformFramework = new()
        {
            { AppPlatform.Windows, "net7.0" },
            { AppPlatform.Linux, "net7.0" },
            { AppPlatform.MacOSX, "net7.0" },
            { AppPlatform.Android, "net7.0-android" },
            { AppPlatform.iOS, "net7.0-ios" },
        };

        public string basePath;
        public string stapleBasePath;

        public void GenerateGameCSProj(AppPlatform platform)
        {
            using var collection = new ProjectCollection();

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", "Game");
            var assetsDirectory = Path.Combine(basePath, "Assets");

            var projectProperties = new Dictionary<string, string>()
            {
                { "OutputType", "Library" },
                { "TargetFramework", "net7.0" },
                { "StripSymbols", "true" },
                { "PublishAOT", "true" },
                { "IsAOTCompatible", "true" },
                { "AppDesignerFolder", "Properties" },
            };

            var platformDefinesString = "";

            if (platformDefines.TryGetValue(platform, out var defines) && defines.Length > 0)
            {
                platformDefinesString = $";{string.Join(";", defines)}";
            }

            var p = new Project(collection);

            p.Xml.Sdk = "Microsoft.NET.Sdk";

            var debugProperty = p.Xml.AddPropertyGroup();

            debugProperty.Condition = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
            debugProperty.AddProperty("PlatformTarget", "AnyCPU");
            debugProperty.AddProperty("DebugType", "pdbonly");
            debugProperty.AddProperty("DebugSymbols", "true");
            debugProperty.AddProperty("Optimize", "false");
            debugProperty.AddProperty("DefineConstants", $"_DEBUG;STAPLE_EDITOR{platformDefinesString}");
            debugProperty.AddProperty("ErrorReport", "prompt");
            debugProperty.AddProperty("WarningLevel", "4");

            var releaseProperty = p.Xml.AddPropertyGroup();

            releaseProperty.Condition = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
            releaseProperty.AddProperty("PlatformTarget", "AnyCPU");
            releaseProperty.AddProperty("DebugType", "portable");
            releaseProperty.AddProperty("DebugSymbols", "true");
            releaseProperty.AddProperty("Optimize", "true");
            releaseProperty.AddProperty("DefineConstants", $"NDEBUG;STAPLE_EDITOR{platformDefinesString}");
            releaseProperty.AddProperty("ErrorReport", "prompt");
            releaseProperty.AddProperty("WarningLevel", "4");

            foreach (var pair in projectProperties)
            {
                p.SetProperty(pair.Key, pair.Value);
            }

            p.AddItem("Reference", "StapleCore", new KeyValuePair<string, string>[] { new("HintPath", Path.Combine(AppContext.BaseDirectory, "StapleCore.dll")) });
            p.AddItem("Reference", "StapleEditor", new KeyValuePair<string, string>[] { new("HintPath", Path.Combine(AppContext.BaseDirectory, "StapleEditor.dll")) });

            void Recursive(string path)
            {
                try
                {
                    var files = Directory.GetFiles(path, "*.cs");

                    foreach (var file in files)
                    {
                        var filePath = Path.GetFullPath(file);

                        fileModifyStates.AddOrSetKey(filePath, File.GetLastWriteTime(filePath));

                        p.AddItem("Compile", filePath);
                    }

                    var directories = Directory.GetDirectories(path);

                    foreach (var directory in directories)
                    {
                        Recursive(directory);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed generating csproj: {e}");
                }
            }

            Recursive(assetsDirectory);

            try
            {
                Directory.CreateDirectory(projectDirectory);
            }
            catch (Exception)
            {
            }

            p.Save(Path.Combine(projectDirectory, "Game.csproj"));
        }

        public void GeneratePlayerCSProj(AppPlatform platform, AppSettings projectAppSettings, bool debug)
        {
            using var collection = new ProjectCollection();

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Assembly", platform.ToString()));
            }
            catch (Exception)
            {
            }

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", platform.ToString());
            var assetsDirectory = Path.Combine(basePath, "Assets");
            var targetFramework = platformFramework[platform];
            var configurationName = debug ? "Debug" : "Release";

            var projectProperties = new Dictionary<string, string>()
            {
                { "OutputType", debug ? "Exe" : "WinExe" },
                { "TargetFramework", targetFramework },
                { "StripSymbols", "true" },
                { "AppDesignerFolder", "Properties" },
                { "OptimizationPreference", "Speed" },
                { "Nullable", "enable" },
                { "AllowUnsafeBlocks", "true" },
            };

            var platformDefinesString = "";

            if (platformDefines.TryGetValue(platform, out var defines) && defines.Length > 0)
            {
                platformDefinesString = $";{string.Join(";", defines)}";
            }

            var p = new Project(collection);

            p.Xml.Sdk = "Microsoft.NET.Sdk";

            var debugProperty = p.Xml.AddPropertyGroup();

            debugProperty.Condition = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
            debugProperty.AddProperty("PlatformTarget", "AnyCPU");
            debugProperty.AddProperty("DebugType", "pdbonly");
            debugProperty.AddProperty("DebugSymbols", "true");
            debugProperty.AddProperty("Optimize", "false");
            debugProperty.AddProperty("DefineConstants", $"_DEBUG{platformDefinesString}");
            debugProperty.AddProperty("ErrorReport", "prompt");
            debugProperty.AddProperty("WarningLevel", "4");

            var releaseProperty = p.Xml.AddPropertyGroup();

            releaseProperty.Condition = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
            releaseProperty.AddProperty("PlatformTarget", "AnyCPU");
            releaseProperty.AddProperty("DebugType", "portable");
            releaseProperty.AddProperty("DebugSymbols", "true");
            releaseProperty.AddProperty("Optimize", "true");
            releaseProperty.AddProperty("DefineConstants", $"NDEBUG{platformDefinesString}");
            releaseProperty.AddProperty("ErrorReport", "prompt");
            releaseProperty.AddProperty("WarningLevel", "4");

            foreach (var pair in projectProperties)
            {
                p.SetProperty(pair.Key, pair.Value);
            }

            switch (platform)
            {
                case AppPlatform.Windows:
                case AppPlatform.Linux:
                case AppPlatform.MacOSX:

                    p.SetProperty("PublishAOT", "true");
                    p.SetProperty("IsAOTCompatible", "true");

                    break;

                case AppPlatform.Android:

                    p.SetProperty("SupportedOSPlatformVersion", projectAppSettings.androidMinSDK.ToString());
                    p.SetProperty("ApplicationId", projectAppSettings.appBundleID);
                    p.SetProperty("ApplicationVersion", projectAppSettings.appVersion.ToString());
                    p.SetProperty("ApplicationDisplayVersion", projectAppSettings.appDisplayVersion);
                    p.SetProperty("EnableLLVM", "True");
                    p.SetProperty("RuntimeIdentifiers", "android-arm64");

                    break;

                case AppPlatform.iOS:

                    p.SetProperty("SupportedOSPlatformVersion", projectAppSettings.iOSDeploymentTarget.ToString());
                    p.SetProperty("ApplicationId", projectAppSettings.appBundleID);
                    p.SetProperty("ApplicationVersion", projectAppSettings.appVersion.ToString());
                    p.SetProperty("ApplicationDisplayVersion", projectAppSettings.appDisplayVersion);

                    break;
            }

            var typeRegistrationPath = Path.Combine(stapleBasePath, "Engine", "TypeRegistration", "TypeRegistration.csproj");

            p.AddItem("Reference", "StapleCore", new KeyValuePair<string, string>[]
            {
                new("HintPath", Path.Combine(stapleBasePath, "Engine", "Core", "bin", configurationName, targetFramework, "StapleCore.dll"))
            });

            p.AddItem("Reference", "MessagePack", new KeyValuePair<string, string>[]
            {
                new("HintPath", Path.Combine(stapleBasePath, "Engine", "Core", "bin", configurationName, targetFramework, "MessagePack.dll"))
            });

            p.AddItem("Reference", "JoltPhysicsSharp", new KeyValuePair<string, string>[]
            {
                new("HintPath", Path.Combine(stapleBasePath, "Engine", "Core", "bin", configurationName, targetFramework, "JoltPhysicsSharp.dll"))
            });

            if (platform == AppPlatform.Windows || platform == AppPlatform.Linux || platform == AppPlatform.MacOSX)
            {
                p.AddItem("Reference", "glfwnet", new KeyValuePair<string, string>[] {
                    new("HintPath", Path.Combine(stapleBasePath, "Engine", "Core", "bin", configurationName, targetFramework, "glfwnet.dll"))
                });
            }

            p.AddItem("Reference", "SharpFont", new KeyValuePair<string, string>[] {
                new("HintPath", Path.Combine(stapleBasePath, "Engine", "Core", "bin", configurationName, targetFramework, "SharpFont.dll"))
            });

            p.AddItem("ProjectReference", typeRegistrationPath,
                new KeyValuePair<string, string>[] {
                    new("OutputItemType", "Analyzer"),
                    new("ReferenceOutputAssembly", "false")
                });

            var trimmerRootAssemblies = p.Xml.AddItemGroup();

            trimmerRootAssemblies.AddItem("TrimmerRootAssembly", "Player");
            trimmerRootAssemblies.AddItem("TrimmerRootAssembly", "StapleCore");
            trimmerRootAssemblies.AddItem("TrimmerRootAssembly", "SharpFont");

            switch (platform)
            {
                case AppPlatform.Android:

                    {
                        var activityPath = Path.Combine(stapleBasePath, "Engine", "Player", "Android", "StapleActivity.cs");

                        p.AddItem("Compile", Path.GetFullPath(activityPath));
                    }

                    break;

                default:

                    {
                        var programPath = Path.Combine(stapleBasePath, "Engine", "Player", "Program.cs");

                        p.AddItem("Compile", Path.GetFullPath(programPath));
                    }

                    break;
            }
            void Recursive(string path)
            {
                try
                {
                    var files = Directory.GetFiles(path, "*.cs");

                    foreach (var file in files)
                    {
                        if(file.Replace(Path.DirectorySeparatorChar, '/').Contains($"/Editor/"))
                        {
                            continue;
                        }

                        p.AddItem("Compile", Path.GetFullPath(file));
                    }

                    var directories = Directory.GetDirectories(path);

                    foreach (var directory in directories)
                    {
                        Recursive(directory);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed generating csproj: {e}");
                }
            }

            Recursive(assetsDirectory);

            p.Save(Path.Combine(projectDirectory, "Player.csproj"));

            if (platform == AppPlatform.Android)
            {
                void MakeDirectory(string path)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch (Exception)
                    {
                    }
                }

                bool SaveResource(string path, string data)
                {
                    try
                    {
                        File.WriteAllText(path, data);
                    }
                    catch (Exception)
                    {
                        Log.Error($"Failed generating csproj: Failed to save a resource");

                        return false;
                    }

                    return true;
                }

                MakeDirectory(Path.Combine(projectDirectory, "lib"));
                MakeDirectory(Path.Combine(projectDirectory, "lib", "arm64-v8a"));

                MakeDirectory(Path.Combine(projectDirectory, "Resources"));
                MakeDirectory(Path.Combine(projectDirectory, "Resources", "values"));

                var manifest = $$"""
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
  <application android:allowBackup="true" android:icon="@mipmap/appicon" android:label="@string/app_name" android:roundIcon="@mipmap/appicon_round" android:supportsRtl="true">
  </application>
  <uses-permission android:name="android.permission.INTERNET" />
</manifest>
""";

                if (SaveResource(Path.Combine(projectDirectory, "AndroidManifest.xml"), manifest) == false)
                {
                    return;
                }

                var strings = $$"""
<resources>
    <string name="app_name">{{projectAppSettings.appName}}</string>
</resources>
""";

                if (SaveResource(Path.Combine(projectDirectory, "Resources", "values", "strings.xml"), strings) == false)
                {
                    return;
                }
            }
        }

    }
}
