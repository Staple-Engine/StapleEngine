using Microsoft.Build.Evaluation;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        public void CollectGameScriptModifyStates()
        {
            var assetsDirectory = Path.Combine(basePath, "Assets");

            fileModifyStates.Clear();

            void Recursive(string path)
            {
                try
                {
                    var files = Directory.GetFiles(path, "*.cs");

                    foreach (var file in files)
                    {
                        var filePath = Path.GetFullPath(file);

                        fileModifyStates.AddOrSetKey(filePath, File.GetLastWriteTime(filePath));
                    }

                    var directories = Directory.GetDirectories(path);

                    foreach (var directory in directories)
                    {
                        Recursive(directory);
                    }
                }
                catch (Exception)
                {
                }
            }

            Recursive(assetsDirectory);
        }

        public bool NeedsGameRecompile()
        {
            var assetsDirectory = Path.Combine(basePath, "Assets");

            bool Recursive(string path)
            {
                try
                {
                    var files = Directory.GetFiles(path, "*.cs");

                    foreach (var file in files)
                    {
                        var filePath = Path.GetFullPath(file);

                        if(fileModifyStates.ContainsKey(filePath) == false ||
                            fileModifyStates[filePath] < File.GetLastWriteTime(filePath))
                        {
                            CollectGameScriptModifyStates();

                            return true;
                        }
                    }

                    var directories = Directory.GetDirectories(path);

                    foreach (var directory in directories)
                    {
                        if(Recursive(directory))
                        {
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return false;
            }

            return Recursive(assetsDirectory);
        }

        public void OpenGameSolution()
        {
            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", "Sandbox");

            var startInfo = new ProcessStartInfo(Path.Combine(projectDirectory, "Sandbox.sln"))
            {
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        public void GenerateGameCSProj(AppPlatform platform, bool sandbox)
        {
            using var collection = new ProjectCollection();

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", (sandbox ? "Sandbox" : "Game"));
            var assetsDirectory = Path.Combine(basePath, "Assets");

            var projectProperties = new Dictionary<string, string>()
            {
                { "OutputType", "Library" },
                { "TargetFramework", "net7.0" },
                { "StripSymbols", "true" },
                { "PublishAOT", "true" },
                { "IsAOTCompatible", "true" },
                { "AppDesignerFolder", "Properties" },
                { "TieredCompilation", "false" },
                { "PublishReadyToRun", "false" },
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
            debugProperty.AddProperty("DebugType", "embedded");
            debugProperty.AddProperty("DebugSymbols", "true");
            debugProperty.AddProperty("Optimize", "false");
            debugProperty.AddProperty("DefineConstants", $"_DEBUG;STAPLE_EDITOR{platformDefinesString}");
            debugProperty.AddProperty("ErrorReport", "prompt");
            debugProperty.AddProperty("WarningLevel", "4");

            var releaseProperty = p.Xml.AddPropertyGroup();

            releaseProperty.Condition = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
            releaseProperty.AddProperty("PlatformTarget", "AnyCPU");
            releaseProperty.AddProperty("DebugType", "embedded");
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

            try
            {
                File.Delete(Path.Combine(projectDirectory, sandbox ? "Sandbox.sln" : "Game.sln"));
            }
            catch(Exception)
            {
            }

            var startInfo = new ProcessStartInfo("dotnet", "new sln")
            {
                WorkingDirectory = projectDirectory
            };

            var process = new Process
            {
                StartInfo = startInfo
            };

            if (process.Start())
            {
                while (process.HasExited == false) ;

                if(process.ExitCode != 0)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            if(process.ExitCode != 0)
            {
                return;
            }

            startInfo = new ProcessStartInfo("dotnet", "sln add Game.csproj")
            {
                WorkingDirectory = projectDirectory
            };

            process = new Process
            {
                StartInfo = startInfo
            };

            if (process.Start())
            {
                while (process.HasExited == false) ;

                if (process.ExitCode != 0)
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        public void GeneratePlayerCSProj(PlayerBackend backend, AppSettings projectAppSettings, bool debug, bool nativeAOT)
        {
            using var collection = new ProjectCollection();

            var p = new Project(collection);

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

            void FindScripts(string path)
            {
                try
                {
                    var files = Directory.GetFiles(path, "*.cs");

                    foreach (var file in files)
                    {
                        if (file.Replace(Path.DirectorySeparatorChar, '/').Contains($"/Editor/"))
                        {
                            continue;
                        }

                        p.AddItem("Compile", Path.GetFullPath(file));
                    }

                    var directories = Directory.GetDirectories(path);

                    foreach (var directory in directories)
                    {
                        FindScripts(directory);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed generating csproj: {e}");
                }
            }

            void CopyDirectory(string from, string to)
            {
                MakeDirectory(to);

                try
                {
                    var files = Directory.GetFiles(from, "*");

                    foreach(var file in files)
                    {
                        try
                        {
                            File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
                        }
                        catch(Exception)
                        {
                        }
                    }

                    var directories = Directory.GetDirectories(from);

                    foreach(var directory in directories)
                    {
                        CopyDirectory(directory, Path.Combine(to, Path.GetFileName(directory)));
                    }
                }
                catch(Exception)
                {
                }
            }

            var platform = backend.platform;

            MakeDirectory(Path.Combine(basePath, "Cache", "Assembly", platform.ToString()));

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", platform.ToString());
            var assetsDirectory = Path.Combine(basePath, "Assets");
            var configurationName = debug ? "Debug" : "Release";

            CopyDirectory(Path.Combine(backend.basePath, "Resources"), projectDirectory);

            if(backend.dataDirIsOutput == false)
            {
                CopyDirectory(Path.Combine(backend.basePath, "Redist", configurationName), Path.Combine(projectDirectory, backend.redistOutput));
            }

            var targetFramework = platformFramework[platform];

            var exeType = platform switch
            {
                AppPlatform.Windows or AppPlatform.Linux or AppPlatform.MacOSX => debug ? "Exe" : "WinExe",
                _ => "Exe",
            };

            var projectProperties = new Dictionary<string, string>()
            {
                { "OutputType", exeType },
                { "TargetFramework", targetFramework },
                { "StripSymbols", debug ? "false" : "true" },
                { "AppDesignerFolder", "Properties" },
                { "OptimizationPreference", "Speed" },
                { "Nullable", "enable" },
                { "AllowUnsafeBlocks", "true" },
                { "TieredCompilation", "false" },
                { "PublishReadyToRun", "false" },
            };

            var platformDefinesString = "";

            if (platformDefines.TryGetValue(platform, out var defines) && defines.Length > 0)
            {
                platformDefinesString = $";{string.Join(";", defines)}";
            }

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

            if(platform == AppPlatform.Windows)
            {
                try
                {
                    var iconData = File.ReadAllBytes(Path.Combine(basePath, "Settings", "Icon.png"));

                    var textureData = Texture.LoadStandard(iconData, StandardTextureColorComponents.RGBA);

                    if(textureData != null)
                    {
                        var width = 256;
                        var height = (int)(textureData.height / (float)textureData.width * 256);

                        if(textureData.Resize(width, height))
                        {
                            var pngData = textureData.EncodePNG();

                            var stream = new MemoryStream();

                            //reserved
                            stream.WriteByte(0);
                            stream.WriteByte(0);

                            //image type (1 = icon, 2 = cursor)
                            var bytes = BitConverter.GetBytes((short)1);

                            stream.Write(bytes);

                            //number of images, same as type since we only got one
                            stream.Write(bytes);

                            //image width
                            stream.WriteByte((byte)(width % 256));

                            //image height
                            stream.WriteByte((byte)(height % 256));

                            //number of colors
                            stream.WriteByte(0);

                            //reserved
                            stream.WriteByte(0);

                            //4-5 color planes
                            stream.WriteByte(0);
                            stream.WriteByte(0);

                            //bits per pixel
                            bytes = BitConverter.GetBytes((short)32);

                            stream.Write(bytes);

                            //size of image data
                            bytes = BitConverter.GetBytes(pngData.Length);

                            stream.Write(bytes);

                            //offset of image data
                            bytes = BitConverter.GetBytes(22);

                            stream.Write(bytes);

                            stream.Write(pngData);

                            var final = stream.ToArray();

                            File.WriteAllBytes(Path.Combine(projectDirectory, "Icon.ico"), final);

                            p.SetProperty("ApplicationIcon", $"Icon.ico");

                            p.Xml.AddItemGroup().AddItem("Content", $"Icon.ico");
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            else if(platform == AppPlatform.Android)
            {
                try
                {
                    var iconData = File.ReadAllBytes(Path.Combine(basePath, "Settings", "Icon.png"));
                    var backgroundData = File.ReadAllBytes(Path.Combine(basePath, "Settings", "Icon Background.png"));
                    var foregroundData = File.ReadAllBytes(Path.Combine(basePath, "Settings", "Icon Foreground.png"));

                    var sizes = new Dictionary<string, List<int>>
                    {
                        { "mipmap-mdpi", new() { 48, 108 } },
                        { "mipmap-hdpi", new() { 72, 162 } },
                        { "mipmap-xhdpi", new() { 96, 216 } },
                        { "mipmap-xxhdpi", new() { 144, 324 } },
                        { "mipmap-xxxhdpi", new() { 192, 432 } },
                    };

                    foreach(var pair in sizes)
                    {
                        var iconTexture = Texture.LoadStandard(iconData, StandardTextureColorComponents.RGBA);
                        var backgroundTexture = Texture.LoadStandard(backgroundData, StandardTextureColorComponents.RGBA);
                        var foregroundTexture = Texture.LoadStandard(foregroundData, StandardTextureColorComponents.RGBA);

                        if (iconTexture == null || backgroundTexture == null || foregroundTexture == null)
                        {
                            break;
                        }

                        if(iconTexture.Resize(pair.Value.FirstOrDefault(), pair.Value.FirstOrDefault()) == false ||
                            backgroundTexture.Resize(pair.Value.LastOrDefault(), pair.Value.LastOrDefault()) == false ||
                            foregroundTexture.Resize(pair.Value.LastOrDefault(), pair.Value.LastOrDefault()) == false)
                        {
                            break;
                        }

                        try
                        {
                            File.WriteAllBytes(Path.Combine(projectDirectory, "Resources", pair.Key, "appicon.png"), iconTexture.EncodePNG());
                            File.WriteAllBytes(Path.Combine(projectDirectory, "Resources", pair.Key, "appicon_background.png"), backgroundTexture.EncodePNG());
                            File.WriteAllBytes(Path.Combine(projectDirectory, "Resources", pair.Key, "appicon_foreground.png"), foregroundTexture.EncodePNG());
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            switch (platform)
            {
                case AppPlatform.Windows:
                case AppPlatform.Linux:
                case AppPlatform.MacOSX:

                    if(nativeAOT)
                    {
                        p.SetProperty("PublishAOT", "true");
                        p.SetProperty("IsAOTCompatible", "true");
                    }
                    else
                    {
                        p.SetProperty("PublishTrimmed", "true");
                        p.SetProperty("PublishSingleFile", "true");
                        p.SetProperty("IsTrimmable", "true");
                        p.SetProperty("EnableTrimAnalyzer", "true");
                        p.SetProperty("EnableSingleFileAnalyzer", "true");
                        p.SetProperty("EnableAotAnalyzer", "true");
                    }

                    break;

                case AppPlatform.Android:

                    p.SetProperty("SupportedOSPlatformVersion", projectAppSettings.androidMinSDK.ToString());
                    p.SetProperty("ApplicationId", projectAppSettings.appBundleID);
                    p.SetProperty("ApplicationVersion", projectAppSettings.appVersion.ToString());
                    p.SetProperty("ApplicationDisplayVersion", projectAppSettings.appDisplayVersion);
                    p.SetProperty("EnableLLVM", "true");
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

            p.AddItem("Reference", "SharpFont", new KeyValuePair<string, string>[] {
                new("HintPath", Path.Combine(stapleBasePath, "Engine", "Core", "bin", configurationName, targetFramework, "SharpFont.dll"))
            });

            p.AddItem("Reference", "NAudio", new KeyValuePair<string, string>[] {
                new("HintPath", Path.Combine(stapleBasePath, "Engine", "Core", "bin", configurationName, targetFramework, "NAudio.dll"))
            });

            p.AddItem("Reference", "NVorbis", new KeyValuePair<string, string>[] {
                new("HintPath", Path.Combine(stapleBasePath, "Engine", "Core", "bin", configurationName, targetFramework, "NVorbis.dll"))
            });

            if (platform == AppPlatform.Windows || platform == AppPlatform.Linux || platform == AppPlatform.MacOSX)
            {
                p.AddItem("Reference", "glfwnet", new KeyValuePair<string, string>[] {
                    new("HintPath", Path.Combine(stapleBasePath, "Engine", "Core", "bin", configurationName, targetFramework, "glfwnet.dll"))
                });
            }

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
                        var activityPath = Path.Combine(projectDirectory, "PlayerActivity.cs");

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

            FindScripts(assetsDirectory);

            p.Save(Path.Combine(projectDirectory, "Player.csproj"));

            if (platform == AppPlatform.Android)
            {
                bool SaveResource(string path, string data)
                {
                    MakeDirectory(Path.GetDirectoryName(path));

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

                var strings = $$"""
<resources>
    <string name="app_name">{{projectAppSettings.appName}}</string>
</resources>
""";

                if (SaveResource(Path.Combine(projectDirectory, "Resources", "values", "strings.xml"), strings) == false)
                {
                    return;
                }

                var orientationType = "Unspecified";

                if(projectAppSettings.portraitOrientation == false || projectAppSettings.landscapeOrientation == false)
                {
                    if(projectAppSettings.portraitOrientation)
                    {
                        orientationType = "Portrait";
                    }
                    else if(projectAppSettings.landscapeOrientation)
                    {
                        orientationType = "Landscape";
                    }
                }

                var activity = $$"""
using Android.App;
using Android.Content.PM;
using Android.OS;
using Staple;

[Activity(Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.{{orientationType}})]
public class PlayerActivity : StapleActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        TypeCacheRegistration.RegisterAll();

        base.OnCreate(savedInstanceState);
    }
}
""";

                if (SaveResource(Path.Combine(projectDirectory, "PlayerActivity.cs"), activity) == false)
                {
                    return;
                }
            }
        }
    }
}
