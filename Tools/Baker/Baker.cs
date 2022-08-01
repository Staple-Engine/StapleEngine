using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Baker
{
    static class Program
    {
        enum Renderer
        {
            d3d9,
            d3d11,
            opengl,
            gles,
            metal,
            pssl,
            spirv
        }

        enum ShaderType
        {
            vertex,
            fragment,
            compute
        }

        static string shadercBinName
        {
            get
            {
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "shadercRelease.exe";
                }

                return "shaderc";
            }
        }
        static string texturecBinName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "texturecRelease.exe";
                }

                return "texturec";
            }
        }

        public static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Usage:\n" + 
                    "Baker\n" +
                    "\t-o [path]: set output directory\n" +
                    "\t-i [path]: set input directory\n" +
                    "\t-sd [define]: add a shader define\n" +
                    "\t-r [name]: set the renderer to compile for\n" +
                    "\t\tValid values are:\n" +
                    "\t\t\td3d9\n" +
                    "\t\t\td3d11\n" +
                    "\t\t\tgles\n" +
                    "\t\t\tmetal\n" +
                    "\t\t\tpssl\n" +
                    "\t\t\tspirv\n");

                Environment.Exit(1);

                return;
            }

            string ValidateTool(string name, string executable)
            {
                var toolPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, executable));
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
            Renderer renderer = Renderer.d3d9;
            bool setRenderer = false;

            for (var i = 0; i < args.Length; i++)
            {
                switch(args[i])
                {
                    case "-o":

                        if(i + 1 >= args.Length)
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
                            .Replace("\\", Path.PathSeparator.ToString())
                            .Replace("/", Path.PathSeparator.ToString());

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
                        catch(Exception)
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

                        if(!Enum.TryParse<Renderer>(args[i + 1], out renderer))
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

            if(!setRenderer)
            {
                Console.WriteLine("Missing renderer (-r) parameter");

                Environment.Exit(1);

                return;
            }

            ProcessShaders(shadercPath, inputPath, outputPath, shaderDefines, renderer);
        }

        static void ProcessShaders(string shadercPath, string inputPath, string outputPath, List<string> shaderDefines, Renderer renderer)
        {
            var bgfxShaderInclude = $"-i \"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "Dependencies", "bgfx", "src"))}\"";

            var shaderFiles = new List<string>();

            try
            {
                shaderFiles.AddRange(Directory.GetFiles(inputPath, "*.sc", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }

            //Ensure we don't try to compile the varying.def file
            for (var i = shaderFiles.Count - 1; i >= 0; i--)
            {
                if (Path.GetFileName(shaderFiles[i]) == "varying.def.sc")
                {
                    shaderFiles.RemoveAt(i);
                }
            }

            Console.WriteLine($"Processing {shaderFiles.Count} shaders...");

            var shaderDefineString = string.Join(",", shaderDefines);

            if (shaderDefineString.Length > 0)
            {
                shaderDefineString = $"--define {shaderDefineString}";
            }

            for (var i = 0; i < shaderFiles.Count; i++)
            {
                Console.WriteLine($"\t{shaderFiles[i]}");

                var directory = Path.GetDirectoryName(shaderFiles[i]);
                var file = Path.GetFileName(shaderFiles[i]);
                var fileWithoutExt = Path.GetFileNameWithoutExtension(file);
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }
                catch(System.Exception)
                {

                }

                ShaderType shaderType;

                if (fileWithoutExt.EndsWith("_vs"))
                {
                    shaderType = ShaderType.vertex;
                }
                else if (fileWithoutExt.EndsWith("_fs"))
                {
                    shaderType = ShaderType.fragment;
                }
                else if (fileWithoutExt.EndsWith("_cs"))
                {
                    shaderType = ShaderType.compute;
                }
                else
                {
                    Console.WriteLine("\t\tError: Unknown shader type");

                    continue;
                }

                var shaderPlatform = "";

                switch (renderer)
                {
                    case Renderer.d3d9:

                        shaderPlatform = "--platform windows -O 3 ";

                        switch (shaderType)
                        {
                            case ShaderType.vertex:

                                shaderPlatform += "-p vs_3_0";

                                break;

                            case ShaderType.fragment:

                                shaderPlatform += "-p ps_3_0";

                                break;

                            case ShaderType.compute:

                                Console.WriteLine("\t\tError: Compute shaders not supported for d3d9");

                                continue;
                        }

                        break;

                    case Renderer.d3d11:

                        shaderPlatform = "--platform windows -O 3 ";

                        switch (shaderType)
                        {
                            case ShaderType.vertex:

                                shaderPlatform += "-p vs_5_0";

                                break;

                            case ShaderType.fragment:

                                shaderPlatform += "-p ps_5_0";

                                break;

                            case ShaderType.compute:

                                shaderPlatform += "-p cs_5_0";

                                break;
                        }

                        break;

                    case Renderer.gles:

                        shaderPlatform = "--platform android";

                        break;

                    case Renderer.metal:

                        shaderPlatform = "--platform osx -p metal";

                        break;

                    case Renderer.opengl:

                        shaderPlatform = "--platform linux";

                        switch (shaderType)
                        {
                            case ShaderType.vertex:

                                shaderPlatform += "-p 120";

                                break;

                            case ShaderType.fragment:

                                shaderPlatform += "-p 120";

                                break;

                            case ShaderType.compute:

                                shaderPlatform += "-p 430";

                                break;
                        }

                        break;

                    case Renderer.pssl:

                        shaderPlatform = "--platform orbis -p pssl";

                        break;

                    case Renderer.spirv:

                        shaderPlatform = "--platform linux -p spirv";

                        break;
                }

                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception)
                {
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shadercPath,
                        Arguments = $"-f \"{shaderFiles[i]}\" -o \"{outputFile}\" {shaderDefineString} --type {shaderType} {bgfxShaderInclude} {shaderPlatform}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    }
                };

                process.Start();

                var result = "";

                while (!process.StandardOutput.EndOfStream)
                {
                    result += $"{process.StandardOutput.ReadLine()}\n";
                }

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"\t\tError:\n\t{result}\n");

                    Environment.Exit(1);

                    return;
                }
            }
        }
    }
}