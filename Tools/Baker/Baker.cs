using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Baker
{
    static class Program
    {
        public static string shadercBinName
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

        public static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Usage:\n" + 
                    "Baker\n" +
                    "\t-o [path]: set output directory\n" +
                    "\t-i [path]: set input directory\n" +
                    "\t-sd [define]: add a shader define\n" +
                    "\t-r [name]: add a renderer to compile for\n");

                Environment.Exit(1);

                return;
            }

            var shadercPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, shadercBinName));
            var shadercValid = false;

            try
            {
                shadercValid = File.Exists(shadercPath);
            }
            catch(Exception)
            {
            }

            if (shadercValid == false)
            {
                Console.WriteLine($"ERROR: Shaderc tool not found at {shadercPath}");

                Environment.Exit(1);

                return;
            }

            var bgfxShaderInclude = $"-i \"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "Dependencies", "bgfx", "src"))}\"";

            var outputPath = "out";
            var inputPath = "";
            var shaderDefines = new List<string>();
            var renderers = new List<string>();

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

                        renderers.Add(args[i + 1]);

                        i++;

                        break;

                    default:

                        Console.WriteLine($"Unknown argument `{args[i]}`");

                        Environment.Exit(1);

                        return;
                }
            }

            //Find shaders to compile
            var shaderFiles = new List<string>();

            try
            {
                shaderFiles.AddRange(Directory.GetFiles(inputPath, "*.sc", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
            }

            //Ensure we don't try to compile the varying.def file
            for(var i = shaderFiles.Count - 1; i >= 0; i--)
            {
                if (Path.GetFileName(shaderFiles[i]) == "varying.def.sc")
                {
                    shaderFiles.RemoveAt(i);
                }
            }

            Console.WriteLine($"Processing {shaderFiles.Count} shaders...");

            var shaderDefineString = string.Join(",", shaderDefines);

            if(shaderDefineString.Length > 0)
            {
                shaderDefineString = $"--define {shaderDefineString}";
            }

            for(var i = 0; i < shaderFiles.Count; i++)
            {
                Console.WriteLine($"\t{shaderFiles[i]}");

                var directory = Path.GetDirectoryName(shaderFiles[i]);
                var file = Path.GetFileNameWithoutExtension(shaderFiles[i]);
                var outputFile = Path.Combine(Path.GetFullPath(outputPath), directory, $"{file}.shader");

                string shaderType;

                if(file.EndsWith("_vs"))
                {
                    shaderType = "vertex";
                }
                else if(file.EndsWith("_fs"))
                {
                    shaderType = "fragment";
                }
                else if(file.EndsWith("_cs"))
                {
                    shaderType = "compute";
                }
                else
                {
                    Console.WriteLine("\t\tError: Unknown shader type");

                    continue;
                }

                try
                {
                    Directory.CreateDirectory(Path.Combine(outputPath, directory));
                }
                catch(Exception)
                {
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shadercPath,
                        Arguments = $"-f \"{shaderFiles[i]}\" -o \"{outputFile}\" {shaderDefineString} --type {shaderType} {bgfxShaderInclude}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    }
                };

                process.Start();

                var result = "";

                while(!process.StandardOutput.EndOfStream)
                {
                    result += $"{process.StandardOutput.ReadLine()}\n";
                }

                if(process.ExitCode != 0)
                {
                    Console.WriteLine($"\t\tError:\n\t{result}\n");

                    Environment.Exit(1);

                    return;
                }
            }
        }
    }
}