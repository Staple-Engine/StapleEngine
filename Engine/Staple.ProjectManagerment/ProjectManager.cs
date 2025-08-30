using Microsoft.Build.Evaluation;
using Newtonsoft.Json;
using Staple.Internal;
using Staple.PackageManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Staple.ProjectManagement;

/// <summary>
/// Handles generating C# Projects
/// </summary>
public partial class ProjectManager
{
    internal static readonly string MessagePackVersion = "3.1.4";

    internal static readonly string SourceGeneratorAssemblyVersion = "4.12.0";

    internal static readonly string StapleCoreFileName = "Staple.Core.dll";

    internal static readonly string StapleEditorFileName = "Staple.Editor.dll";

    public class ProjectInfo
    {
        public AssemblyDefinition asmDef;
        public Package package;
        public Project project;
        public int counter;
    }

    [Flags]
    public enum ProjectGenerationFlags
    {
        None = 0,
        ReferenceEditor = (1 << 1),
        AllowMultiProject = (1 << 2),
        RecordFileModifyStates = (1 << 3),
        IsSandbox = (1 << 4),
        IsPlayer = (1 << 5),
        Debug = (1 << 6),
        NativeAOT = (1 << 7),
        PublishSingleFile = (1 << 8),
    }

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

    public static readonly ProjectManager Instance = new();

    private static string[] GetScriptModifiableFiles(string path)
    {
        return Directory.GetFiles(path, "*.cs")
            .Concat(Directory.GetFiles(path, "*.asmdef"))
            .Concat(AssetSerialization.PluginExtensions.SelectMany(x => Directory.GetFiles(path, $"*{x}.meta")))
            .ToArray();
    }

    /// <summary>
    /// Gets the modify states of each game script, to know whether we need a full recompile.
    /// </summary>
    public void CollectGameScriptModifyStates()
    {
        var assetsDirectory = Path.Combine(basePath, "Assets");
        var packagesDirectory = Path.Combine(basePath, "Cache", "Packages");

        fileModifyStates.Clear();

        void Recursive(string path)
        {
            try
            {
                var files = GetScriptModifiableFiles(path);

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
        Recursive(packagesDirectory);
    }

    /// <summary>
    /// Checks whether we need to recompile the game
    /// </summary>
    /// <returns>Whether to recompile</returns>
    public bool NeedsGameRecompile()
    {
        var assetsDirectory = Path.Combine(basePath, "Assets");
        var packagesDirectory = Path.Combine(basePath, "Cache", "Packages");

        bool Recursive(string path)
        {
            try
            {
                var files = GetScriptModifiableFiles(path);

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

        return Recursive(assetsDirectory) || Recursive(packagesDirectory);
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
    /// <param name="platform">The current platform to build for</param>
    /// <param name="backendBasePath">The base path of the current backend</param>
    /// <param name="configurationName">The configuration name</param>
    /// <returns>Whether we copied successfully</returns>
    public static bool CopyModuleRedists(string targetPath, AppSettings appSettings, AppPlatform platform, string backendBasePath, string configurationName)
    {
        StorageUtils.CreateDirectory(targetPath);

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

        var pluginFiles = AssetDatabase.FindAssetsByType(typeof(PluginAsset).FullName);

        foreach(var file in pluginFiles)
        {
            var path = AssetDatabase.ResolveAssetFullPath(file);

            try
            {
                var text = File.ReadAllText($"{path}.meta");

                var plugin = JsonConvert.DeserializeObject<PluginAsset>(text, Tooling.Utilities.JsonSettings);

                if(plugin.autoReferenced || (plugin.anyPlatform == false && plugin.platforms.Contains(platform) == false))
                {
                    continue;
                }

                if (File.Exists(path))
                {
                    File.Copy(path, Path.Combine(targetPath, Path.GetFileName(path)));
                }
                else if(Directory.Exists(path))
                {
                    if(StorageUtils.CopyDirectory(path, Path.Combine(targetPath, Path.GetFileName(path))) == false)
                    {
                        return false;
                    }
                }
            }
            catch(Exception)
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
            var files = Directory.GetFiles(path, "*.asmdef");

            if (files.Length > 0)
            {
                return files[0];
            }

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

    private static Project MakeCodeGeneratorProject(ProjectCollection collection)
    {
        var p = new Project(collection);

        p.Xml.Sdk = "Microsoft.NET.Sdk";

        p.SetProperty("TargetFramework", "netstandard2.0");
        p.SetProperty("LangVersion", "latest");
        p.SetProperty("EnforceExtendedAnalyzerRules", "true");
        p.SetProperty("EmitCompilerGeneratedFiles", "true");
        p.SetProperty("CompilerGeneratedFilesOutputPath", "Generated");

        var itemGroup = p.Xml.AddItemGroup();

        itemGroup.AddItem("PackageReference", "Microsoft.CodeAnalysis.Analyzers",
            [
                new("Version", SourceGeneratorAssemblyVersion),
                new("PrivateAssets", "all"),
                new("IncludeAssets", "runtime; build; native; contentfiles; analyzers; buildtransitive")
            ]);

        itemGroup.AddItem("PackageReference", "Microsoft.CodeAnalysis.CSharp",
            [
                new("Version", SourceGeneratorAssemblyVersion),
                new("PrivateAssets", "all"),
            ]);

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

    public static DateTime GetLastFileChange(string assetsDirectory)
    {
        var highest = DateTime.MinValue;

        void Recursive(string path, string basePath)
        {
            try
            {
                var files = GetScriptModifiableFiles(path);

                foreach (var file in files)
                {
                    var change = File.GetLastWriteTime(file);

                    if(change > highest)
                    {
                        highest = change;
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
                Log.Error($"Failed getting last file change: {e}");
            }
        }

        void HandlePackages()
        {
            try
            {
                foreach (var pair in PackageManager.instance.projectPackages)
                {
                    Recursive(pair.Value.Item1, pair.Value.Item1);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed generating csproj: {e}");
            }
        }

        Recursive(assetsDirectory, assetsDirectory);

        HandlePackages();

        return highest;
    }

    public void GenerateProject(string projectDirectory, string assetsDirectory, PlayerBackend backend, Dictionary<string, string> projectProperties, 
        Dictionary<string, string> asmDefProperties, AppSettings projectAppSettings, AppPlatform platform, ProjectGenerationFlags flags)
    {
        using var collection = new ProjectCollection();

        try
        {
            var csprojFiles = Directory.GetFiles(projectDirectory, "*.csproj");

            foreach (var file in csprojFiles)
            {
                try
                {
                    var directoryName = Path.GetDirectoryName(file);

                    if (directoryName == projectDirectory.Replace('/', Path.DirectorySeparatorChar))
                    {
                        File.Delete(file);
                    }
                    else
                    {
                        Directory.Delete(Path.GetDirectoryName(file), true);
                    }
                }
                catch(Exception)
                {
                }
            }
        }
        catch (Exception)
        {
        }

        if (projectAppSettings.allowUnsafeCode)
        {
            projectProperties.AddOrSetKey("AllowUnsafeBlocks", "true");
            asmDefProperties.AddOrSetKey("AllowUnsafeBlocks", "true");
        }

        var platformDefinesString = "";

        if (platformDefines.TryGetValue(platform, out var defines) && defines.Length > 0)
        {
            platformDefinesString = $";{string.Join(";", defines)}";
        }

        var projectDefines = flags.HasFlag(ProjectGenerationFlags.ReferenceEditor) ? $"STAPLE_EDITOR{platformDefinesString}" :
            platformDefinesString;

        var configurationName = flags.HasFlag(ProjectGenerationFlags.Debug) ? "Debug" : "Release";

        var backendStapleCorePath = Path.Combine(backend.basePath, "Runtime", configurationName, StapleCoreFileName);

        var p = MakeProject(collection, projectDefines, projectProperties);

        if (flags.HasFlag(ProjectGenerationFlags.IsPlayer) == false)
        {
            p.AddItem("Reference", "StapleCore", [new("HintPath", Path.Combine(AppContext.BaseDirectory, StapleCoreFileName))]);

            if(flags.HasFlag(ProjectGenerationFlags.ReferenceEditor))
            {
                p.AddItem("Reference", "StapleEditor", [new("HintPath", Path.Combine(AppContext.BaseDirectory, StapleEditorFileName))]);
            }
        }

        var projects = new Dictionary<string, ProjectInfo>();
        var excludedAsmDefs = new HashSet<string>();

        void Recursive(string path, string basePath, Project target)
        {
            try
            {
                var files = Directory.GetFiles(path, "*.cs");

                foreach (var file in files)
                {
                    if (flags.HasFlag(ProjectGenerationFlags.ReferenceEditor) == false &&
                        file.Replace(Path.DirectorySeparatorChar, '/').Contains($"/Editor/"))
                    {
                        continue;
                    }

                    var parentAsmDef = FindAsmDef(Path.GetDirectoryName(file), basePath);

                    //Force a fake directory to get the right level of relative path
                    var filePath = Path.GetRelativePath(Path.Combine(projectDirectory, "TEMP"), file);

                    if(flags.HasFlag(ProjectGenerationFlags.RecordFileModifyStates))
                    {
                        fileModifyStates.AddOrSetKey(filePath, File.GetLastWriteTime(filePath));
                    }

                    if (parentAsmDef != null && excludedAsmDefs.Contains(parentAsmDef) == false)
                    {
                        var projectName = Path.GetFileNameWithoutExtension(parentAsmDef);

                        if (projects.TryGetValue(parentAsmDef, out var pair) == false)
                        {
                            AssemblyDefinition def = null;
                            Project asmProj = null;

                            try
                            {
                                def = JsonConvert.DeserializeObject<AssemblyDefinition>(File.ReadAllText(parentAsmDef), Tooling.Utilities.JsonSettings);

                                var meta = File.ReadAllText($"{parentAsmDef}.meta");

                                var holder = JsonConvert.DeserializeObject<AssetHolder>(meta, Tooling.Utilities.JsonSettings);

                                def.guid = holder.guid;
                            }
                            catch (Exception)
                            {
                                excludedAsmDefs.Add(parentAsmDef);

                                continue;
                            }

                            if ((def.anyPlatform && def.excludedPlatforms.Contains(platform)) ||
                                (def.anyPlatform == false && def.platforms.Contains(platform) == false))
                            {
                                excludedAsmDefs.Add(parentAsmDef);

                                continue;
                            }

                            switch(def.type)
                            {
                                case AssemblyDefinition.AssemblyType.Normal:

                                    if (flags.HasFlag(ProjectGenerationFlags.AllowMultiProject))
                                    {
                                        asmProj = MakeProject(collection, projectDefines, asmDefProperties);

                                        if (def.allowUnsafeCode && asmDefProperties.ContainsKey("AllowUnsafeBlocks") == false)
                                        {
                                            asmProj.SetProperty("AllowUnsafeBlocks", "true");
                                        }

                                        if (flags.HasFlag(ProjectGenerationFlags.IsPlayer))
                                        {
                                            asmProj.AddItem("Reference", "StapleCore", [new("HintPath", backendStapleCorePath)]);
                                        }
                                        else
                                        {
                                            asmProj.AddItem("Reference", "StapleCore",
                                                [
                                                    new("HintPath", Path.Combine(AppContext.BaseDirectory, StapleCoreFileName))
                                                ]);

                                            if (flags.HasFlag(ProjectGenerationFlags.ReferenceEditor))
                                            {
                                                asmProj.AddItem("Reference", "StapleEditor",
                                                    [
                                                        new("HintPath", Path.Combine(AppContext.BaseDirectory, StapleEditorFileName))
                                                    ]);
                                            }
                                        }
                                    }

                                    break;

                                case AssemblyDefinition.AssemblyType.CodeGenerator:

                                    {
                                        asmProj = MakeCodeGeneratorProject(collection);

                                        if (def.allowUnsafeCode && asmDefProperties.ContainsKey("AllowUnsafeBlocks") == false)
                                        {
                                            asmProj.SetProperty("AllowUnsafeBlocks", "true");
                                        }
                                    }

                                    break;
                            }

                            var counter = 0;

                            foreach (var projectPair in projects)
                            {
                                if (Path.GetFileNameWithoutExtension(projectPair.Key).Equals(projectName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    counter++;
                                }
                            }

                            pair = new()
                            {
                                asmDef = def,
                                project = asmProj,
                                counter = counter,
                            };

                            projects.Add(parentAsmDef, pair);
                        }

                        var targetProject = pair.project ?? target ?? p;

                        targetProject.AddItem("Compile", filePath);
                    }
                    else
                    {
                        (target ?? p).AddItem("Compile", filePath);
                    }
                }

                var directories = Directory.GetDirectories(path);

                foreach (var directory in directories)
                {
                    Recursive(directory, basePath, target);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed generating csproj: {e}");
            }
        }

        void HandlePackages()
        {
            try
            {
                foreach (var pair in PackageManager.instance.projectPackages)
                {
                    if(flags.HasFlag(ProjectGenerationFlags.AllowMultiProject))
                    {
                        var project = MakeProject(collection, projectDefines, asmDefProperties);

                        if (flags.HasFlag(ProjectGenerationFlags.IsPlayer))
                        {
                            project.AddItem("Reference", "StapleCore", [new("HintPath", backendStapleCorePath)]);
                        }
                        else
                        {
                            project.AddItem("Reference", "StapleCore",
                                [
                                    new("HintPath", Path.Combine(AppContext.BaseDirectory, StapleCoreFileName))
                                ]);

                            if (flags.HasFlag(ProjectGenerationFlags.ReferenceEditor))
                            {
                                project.AddItem("Reference", "StapleEditor",
                                    [
                                        new("HintPath", Path.Combine(AppContext.BaseDirectory, StapleEditorFileName))
                                    ]);
                            }
                        }

                        var counter = 0;

                        var projectName = pair.Value.Item1;

                        if (projectName.Contains('@'))
                        {
                            projectName = projectName.Substring(0, projectName.IndexOf('@'));
                        }

                        foreach (var projectPair in projects)
                        {
                            if (Path.GetFileName(projectPair.Key).Equals(projectName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                counter++;
                            }
                        }

                        Recursive(pair.Value.Item1, pair.Value.Item1, project);

                        if(project.Items.Any(x => x.ItemType == "Compile") == false)
                        {
                            continue;
                        }

                        projects.Add(projectName, new()
                        {
                            project = project,
                            counter = counter,
                            package = pair.Value.Item2,
                        });
                    }
                    else
                    {
                        Recursive(pair.Value.Item1, pair.Value.Item1, p);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed generating csproj: {e}");
            }
        }

        Recursive(assetsDirectory, assetsDirectory, p);

        HandlePackages();

        var assemblies = new List<string>();

        foreach (var directory in AssetDatabase.assetDirectories)
        {
            try
            {
                assemblies.AddRange(Directory.GetFiles(directory, "*.dll.meta", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
                continue;
            }
        }

        foreach (var assemblyPath in assemblies)
        {
            try
            {
                var text = File.ReadAllText(assemblyPath);

                var plugin = JsonConvert.DeserializeObject<PluginAsset>(text, Tooling.Utilities.JsonSettings);

                if (plugin.typeName != typeof(PluginAsset).FullName)
                {
                    continue;
                }

                if (plugin.autoReferenced &&
                    (plugin.anyPlatform || plugin.platforms.Contains(platform)))
                {
                    var targetPath = assemblyPath[..^".meta".Length];

                    foreach (var pair in projects)
                    {
                        pair.Value.project?.AddItem("Reference", Path.GetFileName(targetPath),
                            [new("HintPath", targetPath)]);
                    }

                    p.AddItem("Reference", Path.GetFileName(targetPath), [new("HintPath", targetPath)]);
                }
            }
            catch (Exception)
            {
            }
        }

        if (flags.HasFlag(ProjectGenerationFlags.IsSandbox) == false)
        {
            var typeRegistrationPath = Path.Combine(backend.basePath, "Runtime", "TypeRegistration", "TypeRegistration.csproj");

            p.AddItem("ProjectReference", typeRegistrationPath,
                [
                    new("OutputItemType", "Analyzer"),
                    new("ReferenceOutputAssembly", "false"),
                ]);

            var registration = Path.Combine(projectDirectory, "Player", "GameRegistration.cs");

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

                            Staple.Internal.TypeCache.Freeze();
                        }
                    }
                    """);
            }
            catch (Exception)
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

        var asmDefNames = new List<string>();

        foreach (var pair in projects)
        {
            var counter = pair.Value.counter == 0 ? "" : pair.Value.counter.ToString();

            var projectName = pair.Value.asmDef is null ? Path.GetFileName(pair.Key) : Path.GetFileNameWithoutExtension(pair.Key);

            var name = $"{projectName}{counter}";

            if (pair.Value.project != null)
            {
                asmDefNames.Add(name);

                if ((pair.Value.asmDef?.autoReferenced ?? true))
                {
                    switch((pair.Value.asmDef?.type ?? AssemblyDefinition.AssemblyType.Normal))
                    {
                        case AssemblyDefinition.AssemblyType.Normal:

                            p.AddItem("ProjectReference", Path.Combine("..", name, $"{name}.csproj"));

                            break;

                        case AssemblyDefinition.AssemblyType.CodeGenerator:

                            p.AddItem("ProjectReference", Path.Combine("..", name, $"{name}.csproj"),
                                [
                                    new("OutputItemType", "Analyzer"),
                                    new("ReferenceOutputAssembly", "false"),
                                ]);

                            break;
                    }

                }
            }

            if (pair.Value.asmDef != null)
            {
                if (pair.Value.project != null)
                {
                    foreach (var assembly in pair.Value.asmDef.referencedAssemblies)
                    {
                        var targetAssembly = projects.FirstOrDefault(x => x.Value.asmDef.guid != null && x.Value.asmDef.guid == assembly);

                        if (targetAssembly.Value.asmDef != null && targetAssembly.Value.project != null)
                        {
                            counter = targetAssembly.Value.counter == 0 ? "" : targetAssembly.Value.counter.ToString();

                            var targetAssemblyName = $"{Path.GetFileNameWithoutExtension(targetAssembly.Key)}{counter}";

                            switch(targetAssembly.Value.asmDef.type)
                            {
                                case AssemblyDefinition.AssemblyType.Normal:

                                    pair.Value.project.AddItem("ProjectReference",
                                        Path.Combine("..", targetAssemblyName, $"{targetAssemblyName}.csproj"));

                                    break;

                                case AssemblyDefinition.AssemblyType.CodeGenerator:

                                    pair.Value.project.AddItem("ProjectReference",
                                        Path.Combine("..", targetAssemblyName, $"{targetAssemblyName}.csproj"),
                                        [
                                            new("OutputItemType", "Analyzer"),
                                            new("ReferenceOutputAssembly", "false"),
                                        ]);

                                    break;
                            }
                        }
                    }
                }

                if(pair.Value.asmDef.overrideReferences)
                {
                    foreach(var reference in pair.Value.asmDef.referencedPlugins)
                    {
                        try
                        {
                            var path = AssetDatabase.GetAssetPath(reference);

                            if(path != null)
                            {
                                path = StorageUtils.GetRootPath(basePath, path);
                            }

                            if (PluginAsset.IsAssembly(path))
                            {
                                if (pair.Value.project != null)
                                {
                                    pair.Value.project.AddItem("Reference", path);
                                }
                                else
                                {
                                    p.AddItem("Reference", path);
                                }
                            }
                        }
                        catch(Exception)
                        {
                        }
                    }
                }
            }
            else if (pair.Value.package != null)
            {
                foreach (var dependency in pair.Value.package.dependencies)
                {
                    var targetName = dependency.name;

                    foreach (var projectPair in projects)
                    {
                        if (Path.GetFileName(projectPair.Key) == targetName && projectPair.Value.project != null)
                        {
                            if (pair.Value.project != null)
                            {
                                if(projectPair.Value.asmDef != null)
                                {
                                    switch (projectPair.Value.asmDef.type)
                                    {
                                        case AssemblyDefinition.AssemblyType.Normal:

                                            pair.Value.project.AddItem("ProjectReference",
                                                Path.Combine("..", targetName, $"{targetName}.csproj"));

                                            break;

                                        case AssemblyDefinition.AssemblyType.CodeGenerator:

                                            pair.Value.project.AddItem("ProjectReference",
                                                Path.Combine("..", targetName, $"{targetName}.csproj"),
                                                [
                                                    new("OutputItemType", "Analyzer"),
                                                    new("ReferenceOutputAssembly", "false"),
                                                ]);

                                            break;
                                    }
                                }
                                else
                                {
                                    pair.Value.project.AddItem("ProjectReference",
                                        Path.Combine("..", targetName, $"{targetName}.csproj"));
                                }
                            }
                            else
                            {
                                p.AddItem("ProjectReference",
                                    Path.Combine("..", targetName, $"{targetName}.csproj"));
                            }

                            break;
                        }
                    }
                }
            }

            StorageUtils.CreateDirectory(Path.Combine(projectDirectory, name));

            pair.Value.project?.Save(Path.Combine(projectDirectory, name, $"{name}.csproj"));
        }

        if(flags.HasFlag(ProjectGenerationFlags.IsPlayer))
        {
            if (platform == AppPlatform.Windows)
            {
                p.SetProperty("ApplicationIcon", $"Icon.ico");

                p.Xml.AddItemGroup().AddItem("Content", $"Icon.ico");
            }

            if(flags.HasFlag(ProjectGenerationFlags.NativeAOT) ||
                flags.HasFlag(ProjectGenerationFlags.PublishSingleFile))
            {
                p.SetProperty("SelfContained", "true");
            }

            switch (platform)
            {
                case AppPlatform.Windows:
                case AppPlatform.Linux:
                case AppPlatform.MacOSX:

                    if (flags.HasFlag(ProjectGenerationFlags.NativeAOT))
                    {
                        p.SetProperty("PublishAOT", "true");
                        p.SetProperty("EnableTrimAnalyzer", "true");
                        p.SetProperty("EnableSingleFileAnalyzer", "true");
                        p.SetProperty("EnableAotAnalyzer", "true");
                    }
                    else
                    {
                        p.SetProperty("PublishSingleFile", flags.HasFlag(ProjectGenerationFlags.PublishSingleFile).ToString());
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
                    p.SetProperty("EnableTrimAnalyzer", "true");
                    p.SetProperty("EnableSingleFileAnalyzer", "true");
                    p.SetProperty("EnableAotAnalyzer", "true");
                    p.SetProperty("AndroidEnableMarshalMethods", "false");

                    break;

                case AppPlatform.iOS:

                    p.SetProperty("SupportedOSPlatformVersion", $"{projectAppSettings.iOSDeploymentTarget}.0");
                    p.SetProperty("ApplicationId", projectAppSettings.appBundleID);
                    p.SetProperty("ApplicationVersion", projectAppSettings.appVersion.ToString());
                    p.SetProperty("ApplicationDisplayVersion", projectAppSettings.appDisplayVersion);
                    p.SetProperty("RuntimeIdentifiers", "ios-arm64");
                    p.SetProperty("UseInterpreter", "false");
                    p.SetProperty("EnableTrimAnalyzer", "true");
                    p.SetProperty("EnableSingleFileAnalyzer", "true");
                    p.SetProperty("EnableAotAnalyzer", "true");

                    break;
            }

            p.AddItem("Reference", "StapleCore",
                [
                    new("HintPath", Path.Combine(backend.basePath, "Runtime", configurationName, StapleCoreFileName))
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

            switch (platform)
            {
                case AppPlatform.Android:

                    {
                        var activityPath = Path.Combine(projectDirectory, "Player", "PlayerActivity.cs");

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
                            var redistFiles = Directory.EnumerateFileSystemEntries(Path.Combine(backend.basePath, "Redist", configurationName));

                            foreach (var file in redistFiles)
                            {
                                if (File.GetAttributes(file).HasFlag(FileAttributes.Directory))
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
        }

        var mainProjectName = flags.HasFlag(ProjectGenerationFlags.IsSandbox) ? "Sandbox" :
            flags.HasFlag(ProjectGenerationFlags.IsPlayer) ? "Player" : "Game";

        StorageUtils.CreateDirectory(Path.Combine(projectDirectory, mainProjectName));

        p.Save(Path.Combine(projectDirectory, mainProjectName, $"{mainProjectName}.csproj"));

        var fileName = $"{mainProjectName}.sln";

        var target = Path.Combine(projectDirectory, fileName);

        if (File.Exists(target))
        {
            File.Delete(target);
        }

        var startInfo = new ProcessStartInfo("dotnet", $"new sln -n {mainProjectName}")
        {
            WorkingDirectory = projectDirectory
        };

        var process = new Process
        {
            StartInfo = startInfo
        };

        if (process.Start())
        {
            process.WaitForExit();

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

        var projectFiles = new string[] { mainProjectName }.Concat(asmDefNames);

        foreach (var file in projectFiles)
        {
            startInfo = new ProcessStartInfo("dotnet", $"sln add \"{file}/{file}.csproj\"")
            {
                WorkingDirectory = projectDirectory
            };

            process = new Process
            {
                StartInfo = startInfo
            };

            if (process.Start())
            {
                process.WaitForExit();

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
    /// Generates the game project file
    /// </summary>
    /// <param name="backend">The current backend</param>
    /// <param name="projectAppSettings">The project app settings</param>
    /// <param name="platform">The current platform</param>
    /// <param name="sandbox">Whether we want the project to be separate for the developer to customize</param>
    public void GenerateGameCSProj(PlayerBackend backend, AppSettings projectAppSettings, AppPlatform platform, bool sandbox)
    {
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
            { "IlcOptimizationPreference", "Speed" },
        };

        var flags = ProjectGenerationFlags.RecordFileModifyStates |
            ProjectGenerationFlags.AllowMultiProject |
            ProjectGenerationFlags.ReferenceEditor;

        if(sandbox)
        {
            flags |= ProjectGenerationFlags.IsSandbox;
        }

        GenerateProject(projectDirectory, assetsDirectory, backend, projectProperties, projectProperties, projectAppSettings, platform, flags);
    }

    /// <summary>
    /// Generates the player project
    /// </summary>
    /// <param name="backend">The player backend</param>
    /// <param name="projectAppSettings">The project app settings</param>
    /// <param name="debug">Whether it's a debug build</param>
    /// <param name="nativeAOT">Whether to build natively</param>
    /// <param name="debugRedists">Whether to use debug dependencies</param>
    /// <param name="publishSingleFile">Whether to build to a single file</param>
    public void GeneratePlayerCSProj(PlayerBackend backend, AppSettings projectAppSettings, bool debug, bool nativeAOT, bool debugRedists, bool publishSingleFile)
    {
        var platform = backend.platform;

        StorageUtils.CreateDirectory(Path.Combine(basePath, "Cache", "Assembly", platform.ToString()));

        var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", platform.ToString());
        var assetsDirectory = Path.Combine(basePath, "Assets");
        var redistConfigurationName = debugRedists ? "Debug" : "Release";

        StorageUtils.CopyDirectory(Path.Combine(backend.basePath, "Resources"), Path.Combine(projectDirectory, "Player"));

        if(backend.dataDirIsOutput == false)
        {
            CopyModuleRedists(Path.Combine(projectDirectory, "Player", backend.redistOutput), projectAppSettings,
                platform, backend.basePath, redistConfigurationName);

            StorageUtils.CopyDirectory(Path.Combine(backend.basePath, "Redist", redistConfigurationName),
                Path.Combine(projectDirectory, "Player", backend.redistOutput));
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
            { "TieredCompilation", "false" },
            { "PublishReadyToRun", "false" },
            { "OptimizationPreference", "Speed" },
            { "IlcOptimizationPreference", "Speed" },
            { "MetadataUpdateSupport", "false" },
            { "DebuggerSupport", debug ? "true" : "false" },
            { "EventSourceSupport", debug ? "true" : "false" },
            { "StackTraceSupport", debug ? "true" : "false" },
        };

        var asmDefProjectProperties = new Dictionary<string, string>()
        {
            { "OutputType", "Library" },
            { "TargetFramework", targetFramework },
            { "StripSymbols", debug ? "false" : "true" },
            { "AppDesignerFolder", "Properties" },
            { "TieredCompilation", "false" },
            { "PublishReadyToRun", "false" },
            { "OptimizationPreference", "Speed" },
            { "IlcOptimizationPreference", "Speed" },
            { "MetadataUpdateSupport", "false" },
            { "DebuggerSupport", debug ? "true" : "false" },
            { "EventSourceSupport", debug ? "true" : "false" },
            { "StackTraceSupport", debug ? "true" : "false" },
            { "IsAOTCompatible", "true" },
        };

        var platformUsesSeparateProjects = platform switch
        {
            AppPlatform.Android or AppPlatform.iOS => false,
            _ => true,
        };

        var flags = ProjectGenerationFlags.IsPlayer;

        if(debug)
        {
            flags |= ProjectGenerationFlags.Debug;
        }

        if(nativeAOT)
        {
            flags |= ProjectGenerationFlags.NativeAOT;
        }
        else if (platformUsesSeparateProjects)
        {
            flags |= ProjectGenerationFlags.AllowMultiProject;
        }

        if(publishSingleFile)
        {
            flags |= ProjectGenerationFlags.PublishSingleFile;
        }

        GenerateProject(projectDirectory, assetsDirectory, backend, projectProperties, asmDefProjectProperties, projectAppSettings,
            platform, flags);
    }
}
