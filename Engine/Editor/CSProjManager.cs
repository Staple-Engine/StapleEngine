using Microsoft.Build.Evaluation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Staple.Editor;

/// <summary>
/// Handles generating C# Projects
/// </summary>
internal class CSProjManager
{
    private readonly Dictionary<string, DateTime> fileModifyStates = [];

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
        { AppPlatform.Windows, "net9.0" },
        { AppPlatform.Linux, "net9.0" },
        { AppPlatform.MacOSX, "net9.0" },
        { AppPlatform.Android, "net9.0-android" },
        { AppPlatform.iOS, "net9.0-ios" },
    };

    public string basePath;
    public string stapleBasePath;

    /// <summary>
    /// Gets the modify states of each game script, to know whether we need a full recompile.
    /// </summary>
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

    /// <summary>
    /// Checks whether we need to recompile the game
    /// </summary>
    /// <returns>Whether to recompile</returns>
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

    /// <summary>
    /// Opens the game's solution file
    /// </summary>
    public void OpenGameSolution()
    {
        var projectDirectory = basePath;

        var startInfo = new ProcessStartInfo(Path.Combine(projectDirectory, "Sandbox.sln"))
        {
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    /// <summary>
    /// Copies the redistributable files from a module to a target path
    /// </summary>
    /// <param name="targetPath">The path to copy to</param>
    /// <param name="appSettings">The project appsettings</param>
    /// <param name="backendBasePath">The base path of the current backend</param>
    /// <param name="configurationName">The configuration name</param>
    /// <returns>Whether we copied successfully</returns>
    public static bool CopyModuleRedists(string targetPath, AppSettings appSettings, string backendBasePath, string configurationName)
    {
        void DeleteAll(string extension)
        {
            try
            {
                var files = Directory.GetFiles(targetPath, $"*.{extension}");

                foreach(var file in files)
                {
                    File.Delete(file);
                }
            }
            catch(Exception)
            {
            }
        }

        DeleteAll("pdb");
        DeleteAll("dll");
        DeleteAll("dylib*");
        DeleteAll("so*");

        foreach (var module in appSettings.usedModules)
        {
            if ((module?.Length ?? 0) == 0)
            {
                continue;
            }

            if (EditorUtils.CopyDirectory(Path.Combine(backendBasePath, "Modules", module, "Redist", configurationName), targetPath) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static string FindAsmDef(string path, string assetsDirectory)
    {
        if (path == assetsDirectory)
        {
            return null;
        }

        try
        {
            var files = Directory.GetFiles(path, "*.asmdef");

            if (files.Length > 0)
            {
                return files[0];
            }

            var parent = Path.GetDirectoryName(path);

            return FindAsmDef(parent, assetsDirectory);
        }
        catch (Exception)
        {
        }

        return null;
    }

    private static Project MakeProject(ProjectCollection collection, string defines, Dictionary<string, string> projectProperties)
    {
        var p = new Project(collection);

        p.Xml.Sdk = "Microsoft.NET.Sdk";

        var debugProperty = p.Xml.AddPropertyGroup();

        debugProperty.Condition = " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ";
        debugProperty.AddProperty("PlatformTarget", "AnyCPU");
        debugProperty.AddProperty("DebugType", "embedded");
        debugProperty.AddProperty("DebugSymbols", "true");
        debugProperty.AddProperty("Optimize", "false");
        debugProperty.AddProperty("DefineConstants", $"_DEBUG;{defines}");
        debugProperty.AddProperty("ErrorReport", "prompt");
        debugProperty.AddProperty("WarningLevel", "4");

        var releaseProperty = p.Xml.AddPropertyGroup();

        releaseProperty.Condition = " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ";
        releaseProperty.AddProperty("PlatformTarget", "AnyCPU");
        releaseProperty.AddProperty("DebugType", "embedded");
        releaseProperty.AddProperty("DebugSymbols", "true");
        releaseProperty.AddProperty("Optimize", "true");
        releaseProperty.AddProperty("DefineConstants", $"NDEBUG;{defines}");
        releaseProperty.AddProperty("ErrorReport", "prompt");
        releaseProperty.AddProperty("WarningLevel", "4");

        foreach (var pair in projectProperties)
        {
            p.SetProperty(pair.Key, pair.Value);
        }

        string[] elements =
        [
            "Compile",
            "Content",
            "None"
        ];

        var removed = p.Xml.AddItemGroup();

        foreach (var element in elements)
        {
            var temp = removed.AddItem(element, " ");

            temp.Include = null;
            temp.Remove = "**";
        }

        return p;
    }

    /// <summary>
    /// Generates the game project file
    /// </summary>
    /// <param name="backend">The current backend</param>
    /// <param name="projectAppSettings">The project app settings</param>
    /// <param name="platform">The current platform</param>
    /// <param name="sandbox">Whether we want the project to be separate for the developer to customize</param>
    public void GenerateGameCSProj(PlayerBackend backend, AppSettings projectAppSettings, AppPlatform platform, bool sandbox)
    {
        using var collection = new ProjectCollection();

        var projectDirectory = sandbox ? basePath : Path.Combine(basePath, "Cache", "Assembly", "Game");
        var assetsDirectory = Path.Combine(basePath, "Assets");

        var projectProperties = new Dictionary<string, string>()
        {
            { "OutputType", "Library" },
            { "TargetFramework", "net9.0" },
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

        var projectDefines = $"STAPLE_EDITOR{platformDefinesString}";

        var p = MakeProject(collection, projectDefines, projectProperties);

        p.AddItem("Reference", "StapleCore", [new("HintPath", Path.Combine(AppContext.BaseDirectory, "StapleCore.dll"))]);
        p.AddItem("Reference", "StapleEditor", [new("HintPath", Path.Combine(AppContext.BaseDirectory, "StapleEditor.dll"))]);

        foreach (var pair in projectAppSettings.usedModules)
        {
            p.AddItem("Reference", pair,
                [
                    new("HintPath", Path.Combine(backend.basePath, "Modules", pair, "Assembly", "Debug", $"{pair}.dll"))
                ]);
        }

        var projects = new Dictionary<string, (AssemblyDefinition, Project)>();
        var excludedAsmDefs = new HashSet<string>();

        void Recursive(string path, string basePath)
        {
            try
            {
                var files = Directory.GetFiles(path, "*.cs");

                foreach (var file in files)
                {
                    var parentAsmDef = FindAsmDef(Path.GetDirectoryName(file), basePath);

                    var filePath = Path.GetRelativePath(projectDirectory, file);

                    fileModifyStates.AddOrSetKey(filePath, File.GetLastWriteTime(filePath));

                    if (parentAsmDef != null && excludedAsmDefs.Contains(parentAsmDef) == false)
                    {
                        var projectName = Path.GetFileNameWithoutExtension(parentAsmDef);

                        if(projects.TryGetValue(projectName, out var pair) == false)
                        {
                            AssemblyDefinition def = null;

                            try
                            {
                                def = JsonConvert.DeserializeObject<AssemblyDefinition>(File.ReadAllText(parentAsmDef));
                            }
                            catch (Exception)
                            {
                                excludedAsmDefs.Add(parentAsmDef);

                                continue;
                            }

                            if ((def.includedPlatforms.Count != 0 && def.includedPlatforms.Contains(platform) == false) ||
                                def.excludedPlatforms.Contains(platform))
                            {
                                excludedAsmDefs.Add(parentAsmDef);

                                continue;
                            }

                            var asmProj = MakeProject(collection, projectDefines, projectProperties);

                            asmProj.AddItem("Reference", "StapleCore", [new("HintPath", Path.Combine(AppContext.BaseDirectory, "StapleCore.dll"))]);
                            asmProj.AddItem("Reference", "StapleEditor", [new("HintPath", Path.Combine(AppContext.BaseDirectory, "StapleEditor.dll"))]);

                            pair = (def, asmProj);

                            projects.Add(projectName, pair);

                            if(def.autoReferenced)
                            {
                                p.AddItem("ProjectReference", $"{projectName}.csproj");
                            }
                        }

                        var (asmDef, project) = pair;

                        project.AddItem("Compile", filePath);
                    }
                    else
                    {
                        p.AddItem("Compile", filePath);
                    }
                }

                var directories = Directory.GetDirectories(path);

                foreach (var directory in directories)
                {
                    Recursive(directory, basePath);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed generating csproj: {e}");
            }
        }

        Recursive(assetsDirectory, assetsDirectory);

        if(sandbox == false)
        {
            var typeRegistrationPath = Path.Combine(backend.basePath, "Runtime", "TypeRegistration", "TypeRegistration.csproj");

            p.AddItem("ProjectReference", typeRegistrationPath,
                [
                    new("OutputItemType", "Analyzer"),
                    new("ReferenceOutputAssembly", "false"),
                ]);

            var registration = Path.Combine(projectDirectory, "GameRegistration.cs");

            p.AddItem("Compile", registration);

            try
            {
                File.WriteAllText(registration, $$"""
                    namespace Staple.Internal;

                    public sealed class GameRegistration
                    {
                        public void RegisterAll()
                        {
                            StapleCodeGeneration.TypeCacheRegistration.RegisterAll();
                        }
                    }
                    """);
            }
            catch(Exception)
            {
            }
        }

        try
        {
            Directory.CreateDirectory(projectDirectory);
        }
        catch (Exception)
        {
        }

        p.Save(Path.Combine(projectDirectory, "Game.csproj"));

        foreach(var pair in projects)
        {
            pair.Value.Item2.Save(Path.Combine(projectDirectory, $"{pair.Key}.csproj"));
        }

        var fileName = sandbox ? "Sandbox.sln" : "Game.sln";

        var target = Path.Combine(projectDirectory, fileName);

        if (File.Exists(target))
        {
            File.Delete(target);
        }

        var startInfo = new ProcessStartInfo("dotnet", $"new sln -n {Path.GetFileNameWithoutExtension(fileName)}")
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

            if (process.ExitCode != 0)
            {
                return;
            }
        }
        else
        {
            return;
        }

        if (process.ExitCode != 0)
        {
            return;
        }

        var projectFiles = new string[] { "Game" }.Concat(projects.Keys);

        foreach (var file in projectFiles)
        {
            startInfo = new ProcessStartInfo("dotnet", $"sln add \"{file}.csproj\"")
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
    }

    /// <summary>
    /// Generates the player project
    /// </summary>
    /// <param name="backend">The player backend</param>
    /// <param name="projectAppSettings">The project app settings</param>
    /// <param name="debug">Whether it's a debug build</param>
    /// <param name="nativeAOT">Whether to build natively</param>
    /// <param name="debugRedists">Whether to use debug dependencies</param>
    public void GeneratePlayerCSProj(PlayerBackend backend, AppSettings projectAppSettings, bool debug, bool nativeAOT, bool debugRedists)
    {
        using var collection = new ProjectCollection();

        var platform = backend.platform;

        EditorUtils.CreateDirectory(Path.Combine(basePath, "Cache", "Assembly", platform.ToString()));

        var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", platform.ToString());
        var assetsDirectory = Path.Combine(basePath, "Assets");
        var configurationName = debug ? "Debug" : "Release";
        var redistConfigurationName = debugRedists ? "Debug" : "Release";

        try
        {
            var csprojFiles = Directory.GetFiles(projectDirectory, "*.csproj");

            foreach (var file in csprojFiles)
            {
                File.Delete(file);
            }
        }
        catch (Exception)
        {
        }

        EditorUtils.CopyDirectory(Path.Combine(backend.basePath, "Resources"), projectDirectory);

        if(backend.dataDirIsOutput == false)
        {
            CopyModuleRedists(Path.Combine(Path.Combine(projectDirectory, backend.redistOutput), backend.redistOutput),
                projectAppSettings, backend.basePath, redistConfigurationName);

            EditorUtils.CopyDirectory(Path.Combine(backend.basePath, "Redist", redistConfigurationName), Path.Combine(projectDirectory, backend.redistOutput));
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

        var projectDefines = platformDefinesString;

        var p = MakeProject(collection, projectDefines, projectProperties);

        var projects = new Dictionary<string, (AssemblyDefinition, Project)>();
        var excludedAsmDefs = new HashSet<string>();

        var asmDefProjectProperties = new Dictionary<string, string>()
        {
            { "OutputType", "Library" },
            { "TargetFramework", targetFramework },
            { "StripSymbols", "true" },
            { "AppDesignerFolder", "Properties" },
            { "TieredCompilation", "false" },
            { "PublishReadyToRun", "false" },
        };

        var platformUsesSeparateProjects = platform switch
        {
            AppPlatform.Android or AppPlatform.iOS  => false,
            _ => true,
        };

        void FindScripts(string path, string basePath)
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

                    var parentAsmDef = FindAsmDef(Path.GetDirectoryName(file), basePath);

                    if (parentAsmDef != null && excludedAsmDefs.Contains(parentAsmDef) == false)
                    {
                        var projectName = Path.GetFileNameWithoutExtension(parentAsmDef);

                        if (projects.TryGetValue(projectName, out var pair) == false)
                        {
                            AssemblyDefinition def = null;

                            try
                            {
                                def = JsonConvert.DeserializeObject<AssemblyDefinition>(File.ReadAllText(parentAsmDef));
                            }
                            catch (Exception)
                            {
                                excludedAsmDefs.Add(parentAsmDef);

                                continue;
                            }

                            if ((def.includedPlatforms.Count != 0 && def.includedPlatforms.Contains(platform) == false) ||
                                def.excludedPlatforms.Contains(platform))
                            {
                                excludedAsmDefs.Add(parentAsmDef);

                                continue;
                            }

                            Project asmProj = null;

                            if(platformUsesSeparateProjects)
                            {
                                asmProj = MakeProject(collection, projectDefines, asmDefProjectProperties);

                                asmProj.AddItem("Reference", "StapleCore", [new("HintPath", Path.Combine(backend.basePath, "Runtime", configurationName, "StapleCore.dll"))]);

                                switch (platform)
                                {
                                    case AppPlatform.Android:

                                        asmProj.SetProperty("SupportedOSPlatformVersion", projectAppSettings.androidMinSDK.ToString());
                                        asmProj.SetProperty("RuntimeIdentifiers", "android-arm64");
                                        asmProj.SetProperty("UseInterpreter", "false");

                                        break;

                                    case AppPlatform.iOS:

                                        asmProj.SetProperty("SupportedOSPlatformVersion", $"{projectAppSettings.iOSDeploymentTarget}.0");
                                        asmProj.SetProperty("RuntimeIdentifiers", "ios-arm64");
                                        asmProj.SetProperty("UseInterpreter", "false");

                                        break;
                                }
                            }

                            pair = (def, asmProj);

                            projects.Add(projectName, pair);

                            if (def.autoReferenced && asmProj != null)
                            {
                                p.AddItem("ProjectReference", $"{projectName}.csproj");
                            }
                        }

                        var (asmDef, project) = pair;

                        project ??= p;

                        project?.AddItem("Compile", Path.GetFullPath(file));
                    }
                    else
                    {
                        p.AddItem("Compile", Path.GetFullPath(file));
                    }
                }

                var directories = Directory.GetDirectories(path);

                foreach (var directory in directories)
                {
                    FindScripts(directory, basePath);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed generating csproj: {e}");
            }
        }

        if(platform == AppPlatform.Windows)
        {
            p.SetProperty("ApplicationIcon", $"Icon.ico");

            p.Xml.AddItemGroup().AddItem("Content", $"Icon.ico");
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
                p.SetProperty("RuntimeIdentifiers", "android-arm64");
                p.SetProperty("UseInterpreter", "false");

                break;

            case AppPlatform.iOS:

                p.SetProperty("SupportedOSPlatformVersion", $"{projectAppSettings.iOSDeploymentTarget}.0");
                p.SetProperty("ApplicationId", projectAppSettings.appBundleID);
                p.SetProperty("ApplicationVersion", projectAppSettings.appVersion.ToString());
                p.SetProperty("ApplicationDisplayVersion", projectAppSettings.appDisplayVersion);
                p.SetProperty("RuntimeIdentifiers", "ios-arm64");
                p.SetProperty("UseInterpreter", "false");

                break;
        }

        p.AddItem("Reference", "StapleCore",
            [
                new("HintPath", Path.Combine(backend.basePath, "Runtime", configurationName, "StapleCore.dll"))
            ]);

        p.AddItem("Reference", "MessagePack",
            [
                new("HintPath", Path.Combine(backend.basePath, "Runtime", configurationName, "MessagePack.dll"))
            ]);

        p.AddItem("Reference", "NAudio",
            [
                new("HintPath", Path.Combine(backend.basePath, "Runtime", configurationName, "NAudio.dll"))
            ]);

        p.AddItem("Reference", "NVorbis",
            [
                new("HintPath", Path.Combine(backend.basePath, "Runtime", configurationName, "NVorbis.dll"))
            ]);

        if (platform == AppPlatform.Windows || platform == AppPlatform.Linux || platform == AppPlatform.MacOSX)
        {
            p.AddItem("Reference", "SDL2-CS",
                [
                    new("HintPath", Path.Combine(backend.basePath, "Runtime", configurationName, "SDL2-CS.dll"))
                ]);
        }

        foreach(var pair in projectAppSettings.usedModules)
        {
            p.AddItem("Reference", pair,
                [
                    new("HintPath", Path.Combine(backend.basePath, "Modules", pair, "Assembly", configurationName, $"{pair}.dll"))
                ]);
        }

        var typeRegistrationPath = Path.Combine(backend.basePath, "Runtime", "TypeRegistration", "TypeRegistration.csproj");

        p.AddItem("ProjectReference", typeRegistrationPath,
            [
                new("OutputItemType", "Analyzer"),
                new("ReferenceOutputAssembly", "false")
            ]);

        //TODO: Consider re-enabling this at some point if necessary
        /*
        var trimmerRootAssemblies = p.Xml.AddItemGroup();

        trimmerRootAssemblies.AddItem("TrimmerRootAssembly", "Player")
            .AddMetadata("RootMode", "library");
        trimmerRootAssemblies.AddItem("TrimmerRootAssembly", "StapleCore")
            .AddMetadata("RootMode", "library");
        */

        switch (platform)
        {
            case AppPlatform.Android:

                {
                    var activityPath = Path.Combine(projectDirectory, "PlayerActivity.cs");

                    p.AddItem("Compile", Path.GetFullPath(activityPath));
                }

                break;

            case AppPlatform.iOS:

                {
                    var itemGroup = p.Xml.AddItemGroup();

                    itemGroup.AddItem("BundleResource", "DefaultResources.pak");
                    itemGroup.AddItem("BundleResource", "Resources.pak");

                    try
                    {
                        var redistFiles = Directory.EnumerateFileSystemEntries(Path.Combine(backend.basePath, "Redist", debug ? "Debug" : "Release"));

                        foreach(var file in redistFiles)
                        {
                            if(File.GetAttributes(file).HasFlag(FileAttributes.Directory))
                            {
                                var item = itemGroup.AddItem("NativeReference", file);

                                item.AddMetadata("Kind", "Framework");
                            }
                            else
                            {
                                itemGroup.AddItem("BundleResource", Path.GetFileName(file));

                                File.Copy(file, Path.Combine(projectDirectory, Path.GetFileName(file)), true);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }

                    var appDelegatePath = Path.Combine(projectDirectory, "AppDelegate.cs");

                    p.AddItem("Compile", Path.GetFullPath(appDelegatePath));

                    var mainPath = Path.Combine(projectDirectory, "Main.cs");

                    p.AddItem("Compile", Path.GetFullPath(mainPath));
                }

                break;

            default:

                {
                    var programPath = Path.Combine(projectDirectory, "Program.cs");

                    p.AddItem("Compile", Path.GetFullPath(programPath));
                }

                break;
        }

        FindScripts(assetsDirectory, assetsDirectory);

        p.Save(Path.Combine(projectDirectory, "Player.csproj"));

        foreach (var pair in projects)
        {
            pair.Value.Item2?.Save(Path.Combine(projectDirectory, $"{pair.Key}.csproj"));
        }

        var fileName = "Player.sln";

        var target = Path.Combine(projectDirectory, fileName);

        if (File.Exists(target))
        {
            File.Delete(target);
        }

        var startInfo = new ProcessStartInfo("dotnet", $"new sln -n {Path.GetFileNameWithoutExtension(fileName)}")
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

            if (process.ExitCode != 0)
            {
                return;
            }
        }
        else
        {
            return;
        }

        if (process.ExitCode != 0)
        {
            return;
        }

        var projectFiles = new string[] { "Player" }.Concat(projects.Keys);

        foreach (var file in projectFiles)
        {
            startInfo = new ProcessStartInfo("dotnet", $"sln add \"{file}.csproj\"")
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
    }
}
