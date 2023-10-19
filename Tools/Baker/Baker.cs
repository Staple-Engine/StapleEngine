using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using static Staple.Internal.ResourcePak;

namespace Baker
{
    static partial class Program
    {
        private static string[] textureExtensions = new string[]
        {
            "bmp",
            "dds",
            "exr",
            "gif",
            "jpg",
            "jpeg",
            "hdr",
            "ktx",
            "png",
            "psd",
            "pvr",
            "tga"
        };

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

            try
            {
                lastToWrite = File.GetLastWriteTime(to);
            }
            catch (Exception)
            {
            }

            return lastFromWrite > lastToWrite || assemblyLastWrite > lastToWrite;
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:\n" +
                    "Baker\n" +
                    "\t-o [path]: set output directory\n" +
                    "\t-i [path]: set input directory\n" +
                    "\t-sd [define]: add a shader define\n" +
                    "\t-editor: enable editor mode, which uses different directories\n" +
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
            var inputPath = "";
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

                        inputPath = args[i + 1];

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

            ProcessShaders(platform, shadercPath, inputPath, outputPath, shaderDefines, renderers);
            ProcessTextures(platform, texturecPath, inputPath, outputPath);
            ProcessMaterials(platform, inputPath, outputPath);
            ProcessScenes(platform, inputPath, outputPath, editorMode);
            ProcessAssets(platform, inputPath, outputPath);
            ProcessAppSettings(platform, inputPath, outputPath, editorMode);
        }

        internal static string FindGuid<T>(string path, bool ignoreType = false)
        {
            var guid = Guid.NewGuid().ToString();
            var meta = path.EndsWith(".meta") ? path : $"{path}.meta";

            try
            {
                var json = File.ReadAllText(meta);
                var holder = JsonConvert.DeserializeObject<AssetHolder>(json);

                if (holder != null && (holder.guid?.Length ?? 0) > 0 && (ignoreType || holder.typeName == typeof(T).FullName))
                {
                    guid = holder.guid;

                    Console.WriteLine($"\t\tReusing guid {guid}");
                }
            }
            catch (Exception)
            {
                try
                {
                    var holder = new AssetHolder()
                    {
                        guid = guid,
                        typeName = typeof(T).FullName,
                    };

                    var json = JsonConvert.SerializeObject(holder, Formatting.Indented);

                    File.WriteAllText(meta, json);

                    Console.WriteLine($"\t\tRegenerating meta");
                }
                catch (Exception)
                {
                }
            }

            return guid;
        }
    }
}