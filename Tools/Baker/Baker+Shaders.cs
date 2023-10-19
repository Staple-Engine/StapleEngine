using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Baker
{
    static partial class Program
    {
        private static void ProcessShaders(AppPlatform platform, string shadercPath, string inputPath, string outputPath, List<string> shaderDefines, List<Renderer> renderers)
        {
            var pieces = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).ToList();

            while(pieces.Count > 0 && pieces.LastOrDefault() != "StapleEngine")
            {
                pieces.RemoveAt(pieces.Count - 1);
            }

            var bgfxShaderInclude = $"-i \"{Path.GetFullPath(Path.Combine(string.Join(Path.DirectorySeparatorChar, pieces), "Dependencies", "bgfx", "src"))}\"";

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
                //Guid collision fix
                Thread.Sleep(25);

                Console.WriteLine($"\t{shaderFiles[i]}");

                var guid = FindGuid<Shader>(shaderFiles[i]);

                var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(shaderFiles[i]));
                var file = Path.GetFileName(shaderFiles[i]);

                foreach (var renderer in renderers)
                {
                    var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, renderer.ToString(), directory, file);
                    var index = outputFile.IndexOf(inputPath);

                    if (index >= 0 && index < outputFile.Length)
                    {
                        outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                    }

                    if (ShouldProcessFile(shaderFiles[i], outputFile) == false)
                    {
                        continue;
                    }

                    Console.WriteLine($"\t\t -> {outputFile}");

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
                    catch (Exception e)
                    {
                        Console.WriteLine("\t\tError: Unable to read file");

                        continue;
                    }

                    if (shader == null)
                    {
                        Console.WriteLine("\t\tError: Unable to read file");

                        continue;
                    }

                    byte[] ProcessShader(ShaderCompilerType shaderType, Renderer renderer)
                    {
                        var shaderPlatform = "";

                        switch (renderer)
                        {
                            case Renderer.d3d11:
                            case Renderer.d3d12:

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

                        if (shaderType == ShaderCompilerType.vertex || shaderType == ShaderCompilerType.fragment)
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

                        process.WaitForExit(300000);

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
                            catch (Exception)
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
                        catch (Exception e)
                        {
                            return null;
                        }
                    }

                    var generatedShader = new SerializableShader()
                    {
                        metadata = new ShaderMetadata()
                        {
                            guid = guid,
                            sourceBlend = shader.sourceBlend,
                            destinationBlend = shader.destinationBlend,
                        }
                    };

                    if (shader.parameters != null)
                    {
                        generatedShader.metadata.uniforms = shader.parameters
                            .Where(x => x != null && x.semantic == ShaderParameterSemantic.Uniform)
                            .Select(x => new ShaderUniform()
                            {
                                name = x.name,
                                //Should be fine, since it passed the Where clause
                                type = x.type,
                            }).ToList();
                    }

                    string GetNativeShaderType(ShaderUniformType type, string name, int index, bool uniform)
                    {
                        var uniformString = uniform ? "uniform " : "";

                        return type switch
                        {
                            ShaderUniformType.Float => uniform ? $"{uniformString}vec4 {name}_uniform;\n#define {name} {name}_uniform.x\n" : $"float {name}",
                            ShaderUniformType.Vector2 => uniform ? $"{uniformString}vec4 {name}_uniform;\n#define {name} {name}_uniform.xy\n" : $"vec2 {name}",
                            ShaderUniformType.Vector3 => uniform ? $"{uniformString}vec4 {name}_uniform;\n#define {name} {name}_uniform.xyz\n" : $"vec3 {name}",
                            ShaderUniformType.Color or ShaderUniformType.Vector4 => $"{uniformString}vec4 {name}",
                            ShaderUniformType.Texture => $"SAMPLER2D({name}, {index})",
                            ShaderUniformType.Matrix3x3 => $"{uniformString}mat3 {name}",
                            ShaderUniformType.Matrix4x4 => $"{uniformString}mat4 {name}",
                            _ => $"{uniformString}vec4 {name}",
                        };
                    }

                    string code;

                    switch (shader.type)
                    {
                        case ShaderType.Compute:

                            if (shader.compute == null || (shader.compute.code?.Count ?? 0) == 0)
                            {
                                Console.WriteLine("\t\tError: Compute Shader missing Compute section");

                                continue;
                            }

                            code = string.Join("\n", shader.compute.code);

                            try
                            {
                                File.WriteAllText("shader_input", code);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("\t\tError: Failed to write shader data");

                                continue;
                            }

                            generatedShader.metadata.type = ShaderType.Compute;

                            generatedShader.computeShader = ProcessShader(ShaderCompilerType.compute, renderer);

                            if (generatedShader.computeShader == null)
                            {
                                Console.WriteLine("\t\tError: Compute Shader failed to compile");

                                continue;
                            }

                            break;

                        case ShaderType.VertexFragment:

                            if (shader.vertex == null || (shader.vertex.code?.Count ?? 0) == 0 ||
                                shader.fragment == null || (shader.fragment.code?.Count ?? 0) == 0)
                            {
                                Console.WriteLine("\t\tError: Shader missing vertex or fragment section");

                                continue;
                            }

                            var varying = "";

                            if (shader.parameters != null)
                            {

                                bool error = false;

                                var counters = new Dictionary<ShaderUniformType, int>();

                                foreach (var parameter in shader.parameters)
                                {
                                    if (parameter == null)
                                    {
                                        error = true;

                                        break;
                                    }

                                    if (parameter.semantic == ShaderParameterSemantic.Varying)
                                    {
                                        var counter = 0;

                                        if(counters.ContainsKey(parameter.type))
                                        {
                                            counter = counters[parameter.type];
                                        }

                                        counters.AddOrSetKey(parameter.type, counter + 1);

                                        varying += $"\n{GetNativeShaderType(parameter.type, parameter.name, counter, false)} : {parameter.attribute}";

                                        if ((parameter.defaultValue?.Length ?? 0) > 0)
                                        {
                                            varying += $" = {parameter.defaultValue}";
                                        }

                                        varying += ";";
                                    }
                                }

                                if (error)
                                {
                                    Console.WriteLine("\t\tError: Invalid parameter detected");

                                    continue;
                                }

                                try
                                {
                                    File.WriteAllText("varying_temp", varying);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("\t\tError: Failed to write parameter data");

                                    continue;
                                }
                            }

                            byte[] Compile(ShaderPiece piece, ShaderCompilerType type, Renderer renderer, ref string code)
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

                                code += "#include <bgfx_shader.sh>\n";

                                var counters = new Dictionary<ShaderUniformType, int>();

                                foreach(var parameter in shader.parameters)
                                {
                                    if(parameter.semantic == ShaderParameterSemantic.Uniform)
                                    {
                                        var counter = 0;

                                        if (counters.ContainsKey(parameter.type))
                                        {
                                            counter = counters[parameter.type];
                                        }

                                        counters.AddOrSetKey(parameter.type, counter + 1);

                                        code += $"{GetNativeShaderType(parameter.type, parameter.name, counter, true)};\n";
                                    }
                                }
                                
                                code += string.Join("\n", piece.code);

                                try
                                {
                                    File.WriteAllText("shader_input", code);
                                }
                                catch (Exception)
                                {
                                    return null;
                                }

                                var data = ProcessShader(type, renderer);

                                try
                                {
                                    File.Delete("shader_input");
                                }
                                catch (Exception)
                                {
                                }

                                return data;
                            }

                            string vertexCode = "";
                            string fragmentCode = "";

                            generatedShader.vertexShader = Compile(shader.vertex, ShaderCompilerType.vertex, renderer, ref vertexCode);
                            generatedShader.fragmentShader = Compile(shader.fragment, ShaderCompilerType.fragment, renderer, ref fragmentCode);

                            try
                            {
                                File.Delete("varying_temp");
                            }
                            catch (Exception)
                            {
                            }

                            if (generatedShader.vertexShader == null || generatedShader.fragmentShader == null)
                            {
                                Console.WriteLine($"Failed to build shader.\nGenerated code:\nVarying:\n{varying}\nVertex:\n{vertexCode}\nFragment:\n{fragmentCode}\n");

                                continue;
                            }

                            break;
                    }

                    var header = new SerializableShaderHeader();

                    using (var stream = File.OpenWrite(outputFile))
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
}
