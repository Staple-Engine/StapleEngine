using MessagePack;
using Newtonsoft.Json;
using Staple;
using Staple.Internal;
using Staple.Tooling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Baker;

static partial class Program
{
    private static void ProcessShaders(AppPlatform platform, string shadercPath, string inputPath, string outputPath, List<string> shaderDefines, List<Renderer> renderers)
    {
        var pieces = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).ToList();

        while(pieces.Count > 0 && pieces.LastOrDefault() != "StapleEngine")
        {
            pieces.RemoveAt(pieces.Count - 1);
        }

        var bgfxShaderInclude = $"-i \"{Path.GetFullPath(Path.Combine(string.Join(Path.DirectorySeparatorChar, pieces), "Tools", "ShaderIncludes"))}\"";

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

        bool ShouldClear()
        {
            for (var i = 0; i < shaderFiles.Count; i++)
            {
                var currentShader = shaderFiles[i];

                var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(currentShader));
                var file = Path.GetFileName(currentShader);

                foreach (var renderer in renderers)
                {
                    var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, renderer.ToString(), directory, file);
                    var index = outputFile.IndexOf(inputPath);

                    if (index >= 0 && index < outputFile.Length)
                    {
                        outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                    }

                    if (ShouldProcessFile(currentShader, outputFile))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        if(ShouldClear())
        {
            foreach (var renderer in renderers)
            {
                var outputDirectory = Path.Combine(outputPath == "." ? "" : outputPath, renderer.ToString());

                try
                {
                    Directory.Delete(outputDirectory, true);
                }
                catch (Exception)
                {
                }
            }
        }

        for (var i = 0; i < shaderFiles.Count; i++)
        {
            var currentShader = shaderFiles[i];

            Console.WriteLine($"\t{currentShader}");

            var guid = FindGuid<Shader>(currentShader);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(currentShader));
            var file = Path.GetFileName(currentShader);

            foreach (var renderer in renderers)
            {
                var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, renderer.ToString(), directory, file);
                var index = outputFile.IndexOf(inputPath);

                if (index >= 0 && index < outputFile.Length)
                {
                    outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
                }

                if (ShouldProcessFile(currentShader, outputFile) == false)
                {
                    continue;
                }

                Console.WriteLine($"\t\t -> {outputFile}");

                WorkScheduler.Dispatch(Path.GetFileName(currentShader.Replace(".meta", "")), () =>
                {
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
                        text = File.ReadAllText(currentShader);
                        shader = JsonConvert.DeserializeObject<UnprocessedShader>(text);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\t\tError: Unable to read file");

                        return;
                    }

                    if (shader == null)
                    {
                        Console.WriteLine("\t\tError: Unable to read file");

                        return;
                    }

                    var variants = Utilities.Combinations(shader.variants.Concat(Shader.DefaultVariants).ToList());

                    Console.WriteLine($"\t\tCompiling {variants.Count} variants");

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

                    byte[] ProcessShader(string varyingFileName, string shaderFileName, List<string> extraDefines, ShaderCompilerType shaderType, Renderer renderer)
                    {
                        var shaderPlatform = "";
                        var outShaderFileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                        switch (renderer)
                        {
                            case Renderer.d3d11:
                            case Renderer.d3d12:

                                shaderPlatform = "--platform windows -O 3 ";

                                switch (shaderType)
                                {
                                    case ShaderCompilerType.vertex:

                                        shaderPlatform += "-p s_5_0";

                                        break;

                                    case ShaderCompilerType.fragment:

                                        shaderPlatform += "-p s_5_0";

                                        break;

                                    case ShaderCompilerType.compute:

                                        shaderPlatform += "-p s_5_0";

                                        break;
                                }

                                break;

                            case Renderer.opengles:

                                shaderPlatform = "--platform android -p 300_es";

                                break;

                            case Renderer.metal:

                                shaderPlatform = "--platform osx -p metal";

                                break;

                            case Renderer.opengl:

                                shaderPlatform = "--platform linux ";

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
                            varyingParameter = $" --varyingdef {varyingFileName}";
                        }

                        try
                        {
                            Directory.CreateDirectory(outputPath);
                        }
                        catch (Exception)
                        {
                        }

                        var defineString = shaderDefineString;

                        if(extraDefines.Count > 0)
                        {
                            if(defineString.Length == 0)
                            {
                                defineString = $"--define {string.Join(",", extraDefines)}";
                            }
                            else
                            {
                                defineString += $",{string.Join(",", extraDefines)}";
                            }
                        }

                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = shadercPath,
                                Arguments = $"-f \"{shaderFileName}\" -o \"{outShaderFileName}\" {defineString} --type {shaderType} {bgfxShaderInclude} {shaderPlatform} {varyingParameter}",
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
                            Console.WriteLine($"Arguments: {process.StartInfo.Arguments}");

                            try
                            {
                                File.Delete(outShaderFileName);
                            }
                            catch (Exception)
                            {
                            }

                            return null;
                        }

                        process.Close();

                        try
                        {
                            var data = File.ReadAllBytes(outShaderFileName);

                            File.Delete(outShaderFileName);

                            return data;
                        }
                        catch (Exception e)
                        {
                            return null;
                        }
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

                    bool ShaderTypeHasEndTerminator(ShaderUniformType type)
                    {
                        return type switch
                        {
                            ShaderUniformType.Float or ShaderUniformType.Vector2 or ShaderUniformType.Vector3 => false,
                            _ => true,
                        };
                    }

                    void Build(string variantKey, List<string> extraDefines)
                    {
                        string code;

                        var varyingFileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                        var shaderFileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                        var shaderObject = new SerializableShaderData();

                        switch (shader.type)
                        {
                            case ShaderType.Compute:

                                if (shader.compute == null || (shader.compute.code?.Count ?? 0) == 0)
                                {
                                    Console.WriteLine("\t\tError: Compute Shader missing Compute section");

                                    return;
                                }

                                code = string.Join("\n", shader.compute.code);

                                try
                                {
                                    File.WriteAllText(shaderFileName, code);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("\t\tError: Failed to write shader data");

                                    return;
                                }

                                generatedShader.metadata.type = ShaderType.Compute;

                                shaderObject.computeShader = ProcessShader(varyingFileName, shaderFileName, extraDefines, ShaderCompilerType.compute, renderer);

                                if (shaderObject.computeShader == null)
                                {
                                    Console.WriteLine("\t\tError: Compute Shader failed to compile");

                                    return;
                                }

                                break;

                            case ShaderType.VertexFragment:

                                if (shader.vertex == null || (shader.vertex.code?.Count ?? 0) == 0 ||
                                    shader.fragment == null || (shader.fragment.code?.Count ?? 0) == 0)
                                {
                                    Console.WriteLine("\t\tError: Shader missing vertex or fragment section");

                                    return;
                                }

                                var varying = $"""
vec4 a_weight : BLENDWEIGHT;
vec4 a_indices : BLENDINDICES;
""";

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

                                            if (counters.ContainsKey(parameter.type))
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

                                        return;
                                    }

                                    try
                                    {
                                        File.WriteAllText(varyingFileName, varying);
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("\t\tError: Failed to write parameter data");

                                        return;
                                    }
                                }

                                byte[] Compile(ShaderPiece piece, ShaderCompilerType type, Renderer renderer, ref string code)
                                {
                                    code = "";

                                    if (type == ShaderCompilerType.vertex)
                                    {
                                        code += $"$input a_weight, a_indices ";

                                        if ((piece.inputs?.Count ?? 0) > 0)
                                        {
                                            code += $", {string.Join(", ", piece.inputs)}";
                                        }
                                    }
                                    else
                                    {

                                        if ((piece.inputs?.Count ?? 0) > 0)
                                        {
                                            code += $"$input {string.Join(", ", piece.inputs)}";
                                        }
                                    }

                                    code += "\n";

                                    if ((piece.outputs?.Count ?? 0) > 0)
                                    {
                                        code += $"$output  {string.Join(", ", piece.outputs)}\n";
                                    }

                                    code += "#include <StapleShader.sh>\n";

                                    var counters = new Dictionary<ShaderUniformType, int>();

                                    foreach (var parameter in shader.parameters)
                                    {
                                        if (parameter.semantic == ShaderParameterSemantic.Uniform)
                                        {
                                            var counter = 0;

                                            if (counters.ContainsKey(parameter.type))
                                            {
                                                counter = counters[parameter.type];
                                            }

                                            counters.AddOrSetKey(parameter.type, counter + 1);

                                            code += $"{GetNativeShaderType(parameter.type, parameter.name, counter, true)}";

                                            if (ShaderTypeHasEndTerminator(parameter.type))
                                            {
                                                code += ";";
                                            }

                                            code += "\n";
                                        }
                                    }

                                    code += string.Join("\n", piece.code);

                                    try
                                    {
                                        File.WriteAllText(shaderFileName, code);
                                    }
                                    catch (Exception)
                                    {
                                        return null;
                                    }

                                    var data = ProcessShader(varyingFileName, shaderFileName, extraDefines, type, renderer);

                                    try
                                    {
                                        File.Delete(shaderFileName);
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    return data;
                                }

                                string vertexCode = "";
                                string fragmentCode = "";

                                shaderObject.vertexShader = Compile(shader.vertex, ShaderCompilerType.vertex, renderer, ref vertexCode);
                                shaderObject.fragmentShader = Compile(shader.fragment, ShaderCompilerType.fragment, renderer, ref fragmentCode);

                                try
                                {
                                    File.Delete(varyingFileName);
                                }
                                catch (Exception)
                                {
                                }

                                if (shaderObject.vertexShader == null || shaderObject.fragmentShader == null)
                                {
                                    Console.WriteLine($"Failed to build shader.\nGenerated code:\nVarying:\n{varying}\nVertex:\n{vertexCode}\nFragment:\n{fragmentCode}\n");

                                    return;
                                }

                                generatedShader.data.AddOrSetKey(variantKey, shaderObject);

                                break;
                        }
                    }

                    if (shader.type == ShaderType.Compute)
                    {
                        Build("", []);
                    }
                    else
                    {
                        foreach (var pair in variants)
                        {
                            var variantKey = string.Join(" ", pair);

                            Build(variantKey, pair);
                        }
                    }

                    var header = new SerializableShaderHeader();

                    using var stream = File.OpenWrite(outputFile);
                    using var writer = new BinaryWriter(stream);

                    var encoded = MessagePackSerializer.Serialize(header)
                        .Concat(MessagePackSerializer.Serialize(generatedShader));

                    writer.Write(encoded.ToArray());
                });
            }
        }
    }
}
