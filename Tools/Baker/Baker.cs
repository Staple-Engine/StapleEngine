using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

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

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:\n" +
                    "Baker\n" +
                    "\t-o [path]: set output directory\n" +
                    "\t-i [path]: set input directory\n" +
                    "\t-sd [define]: add a shader define\n" +
                    "\t-r [name]: set the renderer to compile for\n" +
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
            Renderer renderer = Renderer.opengl;
            bool setRenderer = false;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
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

                        if (!Enum.TryParse<Renderer>(args[i + 1], out renderer))
                        {
                            Console.WriteLine($"Invalid argument `-r`: invalid renderer name `{args[i + 1]}`");

                            Environment.Exit(1);

                            return;
                        }

                        setRenderer = true;

                        i++;

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

            ProcessShaders(shadercPath, inputPath, outputPath, shaderDefines, renderer);
            ProcessTextures(texturecPath, inputPath, outputPath, renderer);
            ProcessMaterials(inputPath, outputPath);
            ProcessScenes(inputPath, outputPath);
            ProcessAppSettings(inputPath, outputPath);
        }
    }
}