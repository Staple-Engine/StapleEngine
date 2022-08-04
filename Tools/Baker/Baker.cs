using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
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
        private enum ShaderCompilerType
        {
            vertex,
            fragment,
            compute
        }

        private enum ParameterSemantic
        {
            Varying,
            Uniform
        }

        [Serializable]
        private class Parameter
        {
            public string name;
            public ParameterSemantic semantic;
            public string type;
            public string attribute;
            public string defaultValue;
        }

        [Serializable]
        private class ShaderPiece
        {
            public List<string> inputs = new List<string>();
            public List<string> outputs = new List<string>();
            public List<string> code = new List<string>();
        }

        [Serializable]
        private class UnprocessedShader
        {
            public ShaderType type;
            public List<Parameter> parameters = new List<Parameter>();
            public ShaderPiece vertex;
            public ShaderPiece fragment;
            public ShaderPiece compute;
        }

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

        private enum Renderer
        {
            d3d11,
            metal,
            opengl,
            opengles,
            pssl,
            spirv
        }

        private static string shadercBinName
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "shadercRelease.exe";
                }

                return "shaderc";
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

                return "texturec";
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
        }

        private static void ProcessTextures(string shadercPath, string inputPath, string outputPath, Renderer renderer)
        {
            var textureFiles = new List<string>();

            foreach (var extension in textureExtensions)
            {
                try
                {
                    textureFiles.AddRange(Directory.GetFiles(inputPath, $"*.{extension}.meta", SearchOption.AllDirectories));
                }
                catch (Exception)
                {
                }
            }

            Console.WriteLine($"Processing {textureFiles.Count} textures...");

            for (var i = 0; i < textureFiles.Count; i++)
            {
                Console.WriteLine($"\t{textureFiles[i].Replace(".meta", "")}");

                try
                {
                    if (File.Exists(textureFiles[i].Replace(".meta", "")) == false)
                    {
                        Console.WriteLine($"\t\tError: {textureFiles[i].Replace(".meta", "")} doesn't exist");

                        continue;
                    }
                }
                catch(Exception)
                {
                    Console.WriteLine($"\t\tError: {textureFiles[i].Replace(".meta", "")} doesn't exist");

                    continue;
                }

                var directory = Path.GetDirectoryName(textureFiles[i]);
                var file = Path.GetFileName(textureFiles[i]);
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file.Replace(".meta", ""));

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                string text;
                TextureMetadata metadata;

                try
                {
                    text = File.ReadAllText(textureFiles[i]);
                }
                catch (Exception)
                {
                    Console.WriteLine($"\t\tError: Failed to read file");

                    continue;
                }

                try
                {
                    metadata = JsonConvert.DeserializeObject<TextureMetadata>(text);
                }
                catch(Exception)
                {
                    Console.WriteLine($"\t\tError: Metadata is corrupted");

                    continue;
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }
                catch (Exception)
                {
                }

                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception)
                {
                }

                var parameters = $"-t {metadata.format} -q {metadata.quality.ToString().ToLowerInvariant()} --max {metadata.maxSize} --as dds";

                if(metadata.isLinear)
                {
                    parameters += " --linear";
                }

                if(metadata.useMipmaps)
                {
                    parameters += " --mips";
                }

                switch(metadata.type)
                {
                    case TextureType.NormalMap:

                        parameters += " --normalmap";

                        break;
                }

                if(metadata.premultiplyAlpha)
                {
                    parameters += " --pma";
                }

                try
                {
                    File.Delete(outputFile);
                }
                catch(Exception)
                {
                }

                var outputFileTemp = $"{outputFile}_temp";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = shadercPath,
                        Arguments = $"-f \"{textureFiles[i].Replace(".meta", "")}\" -o \"{outputFileTemp}\" {parameters} --validate",
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

                try
                {
                    var texture = new SerializableTexture()
                    {
                        metadata = metadata,
                        data = File.ReadAllBytes(outputFileTemp),
                    };

                    var header = new SerializableTextureHeader();

                    using(var stream = File.OpenWrite(outputFile))
                    {
                        using(var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(texture));

                            writer.Write(encoded.ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to save baked texture: {e}");

                    Environment.Exit(1);

                    return;
                }
                finally
                {
                    try
                    {
                        File.Delete(outputFileTemp);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private static void ProcessShaders(string shadercPath, string inputPath, string outputPath, List<string> shaderDefines, Renderer renderer)
        {
            var bgfxShaderInclude = $"-i \"{Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Dependencies", "bgfx", "src"))}\"";

            var shaderFiles = new List<string>();

            try
            {
                shaderFiles.AddRange(Directory.GetFiles(inputPath, "*.stsh", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
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
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);

                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception)
                {
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                }
                catch (Exception)
                {
                }

                string text;
                UnprocessedShader shader;

                try
                {
                    text = File.ReadAllText(shaderFiles[i]);
                    shader = JsonConvert.DeserializeObject<UnprocessedShader>(text);
                }
                catch(Exception e)
                {
                    Console.WriteLine("\t\tError: Unable to read file");

                    continue;
                }

                if(shader == null)
                {
                    Console.WriteLine("\t\tError: Unable to read file");

                    continue;
                }

                byte[] ProcessShader(ShaderCompilerType shaderType)
                {
                    var shaderPlatform = "";

                    switch (renderer)
                    {
                        case Renderer.d3d11:

                            shaderPlatform = "--platform windows -O 3 ";

                            switch (shaderType)
                            {
                                case ShaderCompilerType.vertex:

                                    shaderPlatform += "-p vs_5_0";

                                    break;

                                case ShaderCompilerType.fragment:

                                    shaderPlatform += "-p ps_5_0";

                                    break;

                                case ShaderCompilerType.compute:

                                    shaderPlatform += "-p cs_5_0";

                                    break;
                            }

                            break;

                        case Renderer.opengles:

                            shaderPlatform = "--platform android";

                            break;

                        case Renderer.metal:

                            shaderPlatform = "--platform osx -p metal";

                            break;

                        case Renderer.opengl:

                            shaderPlatform = "--platform linux";

                            switch (shaderType)
                            {
                                case ShaderCompilerType.vertex:

                                    shaderPlatform += "-p 120";

                                    break;

                                case ShaderCompilerType.fragment:

                                    shaderPlatform += "-p 120";

                                    break;

                                case ShaderCompilerType.compute:

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

                    var varyingParameter = "";

                    if(shaderType == ShaderCompilerType.vertex || shaderType == ShaderCompilerType.fragment)
                    {
                        varyingParameter = " --varyingdef varying_temp";
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
                            Arguments = $"-f \"shader_input\" -o \"shader_temp\" {shaderDefineString} --type {shaderType} {bgfxShaderInclude} {shaderPlatform} {varyingParameter}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                        }
                    };

                    process.Start();

                    while (!process.StandardOutput.EndOfStream)
                    {
                        Console.WriteLine(process.StandardOutput.ReadLine());
                    }

                    if (process.ExitCode != 0)
                    {
                        try
                        {
                            File.Delete("shader_temp");
                        }
                        catch(Exception)
                        {
                        }

                        return null;
                    }

                    process.Close();

                    try
                    {
                        var data = File.ReadAllBytes("shader_temp");

                        File.Delete("shader_temp");

                        return data;
                    }
                    catch(Exception e)
                    {
                        return null;
                    }
                }

                var generatedShader = new SerializableShader()
                {
                    metadata = new ShaderMetadata(),
                };

                string code;

                switch (shader.type)
                {
                    case ShaderType.Compute:

                        if(shader.compute == null || (shader.compute.code?.Count ?? 0) == 0)
                        {
                            Console.WriteLine("\t\tError: Compute Shader missing Compute section");

                            continue;
                        }

                        code = string.Join("\n", shader.compute.code);

                        try
                        {
                            File.WriteAllText("shader_input", code);
                        }
                        catch(Exception)
                        {
                            Console.WriteLine("\t\tError: Failed to write shader data");

                            continue;
                        }

                        generatedShader.metadata.type = ShaderType.Compute;

                        generatedShader.computeShader = ProcessShader(ShaderCompilerType.compute);

                        if(generatedShader.computeShader == null)
                        {
                            Console.WriteLine("\t\tError: Compute Shader failed to compile");

                            continue;
                        }

                        break;

                    case ShaderType.VertexFragment:

                        if(shader.vertex == null || (shader.vertex.code?.Count ?? 0) == 0 ||
                            shader.fragment == null || (shader.fragment.code?.Count ?? 0) == 0)
                        {
                            Console.WriteLine("\t\tError: Shader missing vertex or fragment section");

                            continue;
                        }

                        if(shader.parameters != null)
                        {
                            var varying = "";

                            bool error = false;

                            foreach(var parameter in shader.parameters)
                            {
                                if(parameter == null)
                                {
                                    error = true;

                                    break;
                                }

                                if(parameter.semantic == ParameterSemantic.Varying)
                                {
                                    varying += $"\n{parameter.type}  {parameter.name} : {parameter.attribute}";

                                    if((parameter.defaultValue?.Length ?? 0) > 0)
                                    {
                                        varying += $" = {parameter.defaultValue}";
                                    }

                                    varying += ";";
                                }
                            }

                            if(error)
                            {
                                Console.WriteLine("\t\tError: Invalid parameter detected");

                                continue;
                            }

                            try
                            {
                                File.WriteAllText("varying_temp", varying);
                            }
                            catch(Exception)
                            {
                                Console.WriteLine("\t\tError: Failed to write parameter data");

                                continue;
                            }
                        }

                        byte[] Compile(ShaderPiece piece, ShaderCompilerType type)
                        {
                            code = "";

                            if ((piece.inputs?.Count ?? 0) > 0)
                            {
                                code += $"$input  {string.Join(", ", piece.inputs)}\n";
                            }

                            if ((piece.outputs?.Count ?? 0) > 0)
                            {
                                code += $"$output  {string.Join(", ", piece.outputs)}\n";
                            }

                            code += "#include <bgfx_shader.sh>\n" + string.Join("\n", piece.code);

                            try
                            {
                                File.WriteAllText("shader_input", code);
                            }
                            catch(Exception)
                            {
                                return null;
                            }

                            var data = ProcessShader(type);

                            try
                            {
                                File.Delete("shader_input");
                            }
                            catch(Exception)
                            {
                            }

                            return data;
                        }

                        generatedShader.vertexShader = Compile(shader.vertex, ShaderCompilerType.vertex);
                        generatedShader.fragmentShader = Compile(shader.fragment, ShaderCompilerType.fragment);

                        try
                        {
                            File.Delete("varying_temp");
                        }
                        catch (Exception)
                        {
                        }

                        if (generatedShader.vertexShader == null || generatedShader.fragmentShader == null)
                        {
                            Console.WriteLine("Failed to build shader");

                            continue;
                        }

                        break;
                }

                var header = new SerializableShaderHeader();

                using(var stream = File.OpenWrite(outputFile))
                {
                    using (var writer = new BinaryWriter(stream))
                    {
                        var encoded = MessagePackSerializer.Serialize(header)
                            .Concat(MessagePackSerializer.Serialize(generatedShader));

                        writer.Write(encoded.ToArray());
                    }
                }
            }
        }
    }
}