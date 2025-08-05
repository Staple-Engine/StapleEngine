using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Baker;

static partial class Program
{
    private static string shadercBinName
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "shadercRelease.exe";
            }

            return "shadercRelease";
        }
    }
    
    private static string texturecBinName
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "texturecRelease.exe";
            }

            return "texturecRelease";
        }
    }

    public static bool ShouldProcessFile(string from, string to)
    {
        var lastFromWrite = DateTime.MinValue;
        var lastToWrite = DateTime.MinValue;
        var assemblyLastWrite = File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);

        try
        {
            lastFromWrite = File.GetLastWriteTime(from);
        }
        catch (Exception)
        {
        }

        if(from.EndsWith($".{AssetSerialization.ShaderExtension}") || from.EndsWith($".{AssetSerialization.ComputeShaderExtension}"))
        {
            try
            {
                var pieces = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).ToList();

                while (pieces.Count > 0 && pieces.LastOrDefault() != "StapleEngine")
                {
                    pieces.RemoveAt(pieces.Count - 1);
                }

                var files = Directory.GetFiles(Path.GetFullPath(Path.Combine(string.Join(Path.DirectorySeparatorChar, pieces), "Tools", "ShaderIncludes")),
                    "*.sh", SearchOption.AllDirectories);

                foreach(var file in files)
                {
                    var writeTime = File.GetLastWriteTime(file);

                    if(lastFromWrite < writeTime)
                    {
                        lastFromWrite = writeTime;
                    }
                }
            }
            catch(Exception)
            {
            }
        }

        try
        {
            lastToWrite = File.GetLastWriteTime(to);
        }
        catch (Exception)
        {
        }

        return lastFromWrite > lastToWrite || (checkAssemblyBuildTime && assemblyLastWrite > lastToWrite);
    }

    private static readonly Dictionary<string, string> processedTextures = [];

    private static readonly Dictionary<string, string> processedShaders = [];

    internal static bool checkAssemblyBuildTime = true;

    internal static string StapleBasePath
    {
        get
        {
            var pieces = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).ToList();

            while (pieces.Count > 0 && pieces.LastOrDefault() != "StapleEngine")
            {
                pieces.RemoveAt(pieces.Count - 1);
            }

            return Path.Combine(string.Join(Path.DirectorySeparatorChar, pieces));
        }
    }

    public static void Main(string[] args)
    {
        Log.SetLog(new ConsoleLog());

        if (args.Length == 0)
        {
            Console.WriteLine("Usage:\n" +
                "Baker\n" +
                "\t-o [path]: set output directory\n" +
                "\t-i [path]: set input directory\n" +
                "\t-sd [define]: add a shader define\n" +
                "\t-editor: enable editor mode, which uses different directories\n" +
                "\t-no-self-check: don't check this tool's last build time when checking whether to reimport a file\n" +
                $"\t-platform [platform]: specify the platform to build for ({string.Join(", ", Enum.GetValues<AppPlatform>().Select(x => x.ToString()))}\n" +
                "\t-r [name]: set the renderer to compile for (can be repeated for multiple exports)\n" +
                "\t\tValid values are:\n" +
                "\t\t\td3d11\n" +
                "\t\t\tmetal\n" +
                "\t\t\topengl\n" +
                "\t\t\topengles\n" +
                "\t\t\tpssl\n" +
                "\t\t\tspirv\n");

            Environment.Exit(1);

            return;
        }

        MessagePackInit.Initialize();

        string ValidateTool(string name, string executable)
        {
#if _DEBUG
            string baseDir = Environment.CurrentDirectory;
#else
            string baseDir = AppContext.BaseDirectory;
#endif

            var toolPath = Path.GetFullPath(Path.Combine(baseDir, executable));
            var toolValid = false;

            try
            {
                toolValid = File.Exists(toolPath);
            }
            catch (Exception)
            {
            }

            if (toolValid == false)
            {
                Console.WriteLine($"ERROR: {name} tool not found at {toolPath}");

                Environment.Exit(1);

                return null;
            }

            return toolPath;
        }

        var shadercPath = ValidateTool("shaderc", shadercBinName);
        var texturecPath = ValidateTool("texturec", texturecBinName);

        var outputPath = "out";
        var inputPaths = new List<string>();
        var shaderDefines = new List<string>();
        var renderers = new List<Renderer>();
        bool setRenderer = false;
        bool editorMode = false;
        AppPlatform platform = AppPlatform.Windows;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-platform":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Invalid argument `-platform`: missing platform ID");

                        Environment.Exit(1);

                        return;
                    }

                    if (Enum.TryParse(args[i + 1], true, out platform) == false)
                    {
                        Console.WriteLine("Invalid argument `-platform`: invalid platform ID");

                        Environment.Exit(1);

                        return;
                    }

                    i++;

                    break;

                case "-o":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Invalid argument `-o`: missing path");

                        Environment.Exit(1);

                        return;
                    }

                    outputPath = args[i + 1];

                    i++;

                    break;

                case "-i":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Invalid argument `-i`: missing path");

                        Environment.Exit(1);

                        return;
                    }

                    var inputPath = args[i + 1];

                    inputPath = inputPath
                        .Replace("\\", Path.DirectorySeparatorChar.ToString())
                        .Replace("/", Path.DirectorySeparatorChar.ToString());

                    i++;

                    try
                    {
                        if (!Directory.Exists(inputPath))
                        {
                            Console.WriteLine($"Input path `{inputPath}` doesn't exist");

                            Environment.Exit(1);

                            return;
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Input path `{inputPath}` doesn't exist");

                        Environment.Exit(1);

                        return;
                    }

                    inputPaths.Add(inputPath);

                    break;

                case "-sd":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Invalid argument `-sd`: missing define");

                        Environment.Exit(1);

                        return;
                    }

                    shaderDefines.Add(args[i + 1]);

                    i++;

                    break;

                case "-r":

                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Invalid argument `-r`: missing renderer name");

                        Environment.Exit(1);

                        return;
                    }

                    if (!Enum.TryParse<Renderer>(args[i + 1], out var renderer))
                    {
                        Console.WriteLine($"Invalid argument `-r`: invalid renderer name `{args[i + 1]}`");

                        Environment.Exit(1);

                        return;
                    }

                    renderers.Add(renderer);

                    setRenderer = true;

                    i++;

                    break;

                case "-editor":

                    editorMode = true;

                    break;

                case "-no-self-check":

                    checkAssemblyBuildTime = false;

                    break;

                default:

                    Console.WriteLine($"Unknown argument `{args[i]}`");

                    Environment.Exit(1);

                    return;
            }
        }

        if (!setRenderer)
        {
            Console.WriteLine("Missing renderer (-r) parameter");

            Environment.Exit(1);

            return;
        }

        if(inputPaths.Count == 0)
        {
            Console.WriteLine("Missing input (-i) parameter");

            Environment.Exit(1);

            return;
        }

        var finished = false;
        var l = new Lock();

        var resourcesFileName = Platform.CurrentPlatform switch
        {
            AppPlatform.Android => "DefaultResources-Android.pak",
            AppPlatform.iOS => "DefaultResources-iOS.pak",
            AppPlatform.Windows => "DefaultResources-Windows.pak",
            AppPlatform.MacOSX => "DefaultResources-MacOSX.pak",
            AppPlatform.Linux => "DefaultResources-Linux.pak",
            _ => null,
        };

        var resourcesPath = Path.Combine(StapleBasePath, "DefaultResources", resourcesFileName);

        ResourceManager.instance.LoadPak(resourcesPath);

        AssetDatabase.Reload(null, () =>
        {
            foreach (var path in inputPaths)
            {
                var outPath = Path.Combine(outputPath, Path.GetFileName(path));

                ProcessShaders(platform, shadercPath, path, outPath, shaderDefines, renderers);
            }

            lock(l)
            {
                finished = true;
            }
        });

        for(; ; )
        {
            lock(l)
            {
                if (finished)
                {
                    break;
                }
            }

            Thread.Sleep(25);
        }

        WorkScheduler.WaitForTasks();

        //First phase of processing textures
        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessTextures(platform, texturecPath, path, outPath);
        }

        WorkScheduler.WaitForTasks();

        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessMeshes(platform, path, outPath);
        }

        WorkScheduler.WaitForTasks();

        //Second phase of processing textures because some textures might be generated
        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessTextures(platform, texturecPath, path, outPath);
        }

        WorkScheduler.WaitForTasks();

        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessAudio(platform, path, outPath);
        }

        WorkScheduler.WaitForTasks();

        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessMaterials(platform, path, outPath);
        }

        WorkScheduler.WaitForTasks();

        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessAssets(platform, path, outPath);
        }

        WorkScheduler.WaitForTasks();

        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessPrefabs(platform, path, outPath);
        }

        WorkScheduler.WaitForTasks();

        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessFonts(platform, path, outPath);
        }

        WorkScheduler.WaitForTasks();

        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessTextAssets(platform, path, outPath);
        }

        WorkScheduler.WaitForTasks();

        Console.WriteLine($"Cleaning up moved and deleted files in the output folder");

        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            CleanupUnusedFiles(platform, path, outPath);
        }

        foreach (var path in inputPaths)
        {
            var outPath = Path.Combine(outputPath, Path.GetFileName(path));

            ProcessScenes(platform, path, outPath, outputPath, editorMode);
            ProcessFolders(platform, path, outPath);
        }

        ProcessAppSettings(platform, inputPaths[0], outputPath, editorMode);

        WorkScheduler.WaitForTasks();

        //Cleanup extra folders only if we're in editor mode
        if (editorMode)
        {
            try
            {
                var directories = Directory.GetDirectories(outputPath);

                foreach(var directory in directories)
                {
                    var fileName = Path.GetFileName(directory);

                    if(inputPaths.Any(x => Path.GetFileName(x) == fileName))
                    {
                        continue;
                    }

                    Directory.Delete(directory, true);
                }
            }
            catch (Exception)
            {
            }
        }
    }

    internal static string GenerateGuid()
    {
        Thread.Sleep(25);

        return Guid.NewGuid().ToString();
    }

    internal static string FindGuid<T>(string path, bool ignoreType = false)
    {
        var meta = path.EndsWith(".meta") ? path : $"{path}.meta";

        try
        {
            var json = File.ReadAllText(meta);
            var holder = JsonConvert.DeserializeObject<AssetHolder>(json);

            if (holder != null && (holder.guid?.Length ?? 0) > 0 && (ignoreType || holder.typeName == typeof(T).FullName))
            {
                //Console.WriteLine($"\t\tReusing guid {holder.guid}");

                return holder.guid;
            }
        }
        catch (Exception)
        {
        }

        //Guid collision fix
        Thread.Sleep(25);

        var guid = Guid.NewGuid().ToString();

        try
        {
            var holder = new AssetHolder()
            {
                guid = guid,
                typeName = typeof(T).FullName,
            };

            var json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

            File.WriteAllText(meta, json);

            //Console.WriteLine($"\t\tRegenerating meta");
        }
        catch (Exception)
        {
        }

        return guid;
    }
}