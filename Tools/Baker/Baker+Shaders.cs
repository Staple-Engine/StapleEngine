using MessagePack;
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
    private static readonly string DefaultVaryingData = $$"""
vec3 a_position     :   POSITION;
vec3 a_normal       :   NORMAL;
vec3 a_tangent      :   TANGENT;
vec3 a_bitangent    :   BITANGENT;
vec4 a_color0       :   COLOR0;
vec4 a_color1       :   COLOR1;
vec4 a_color2       :   COLOR2;
vec4 a_color3       :   COLOR3;
vec4 a_weight       :   BLENDWEIGHT;
vec4 a_indices      :   BLENDINDICES;
vec2 a_texcoord0    :   TEXCOORD0;
vec2 a_texcoord1    :   TEXCOORD1;
vec2 a_texcoord2    :   TEXCOORD2;
vec2 a_texcoord3    :   TEXCOORD3;
vec2 a_texcoord4    :   TEXCOORD4;
vec2 a_texcoord5    :   TEXCOORD5;
vec2 a_texcoord6    :   TEXCOORD6;
vec2 a_texcoord7    :   TEXCOORD7;
vec4 i_data0        :   TEXCOORD7;
vec4 i_data1        :   TEXCOORD6;
vec4 i_data2        :   TEXCOORD5;
vec4 i_data3        :   TEXCOORD4;
vec4 i_data4        :   TEXCOORD3;
""";

    private static void ProcessShaders(AppPlatform platform, string shadercPath, string inputPath, string outputPath,
        List<string> shaderDefines, List<Renderer> renderers)
    {
        var pieces = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).ToList();

        while(pieces.Count > 0 && pieces.LastOrDefault() != "StapleEngine")
        {
            pieces.RemoveAt(pieces.Count - 1);
        }

        var stapleBase = Path.Combine(string.Join(Path.DirectorySeparatorChar, pieces));

        var resourcesPath = Path.Combine(stapleBase, "DefaultResources", "DefaultResources-Windows.pak");

        ResourceManager.instance.LoadPak(resourcesPath);

        var bgfxShaderInclude = $"-i \"{Path.GetFullPath(Path.Combine(stapleBase, "Tools", "ShaderIncludes"))}\"";

        var shaderFiles = new List<string>();

        try
        {
            shaderFiles.AddRange(Directory.GetFiles(inputPath, $"*.{AssetSerialization.ShaderExtension}", SearchOption.AllDirectories));
            shaderFiles.AddRange(Directory.GetFiles(inputPath, $"*.{AssetSerialization.ComputeShaderExtension}", SearchOption.AllDirectories));
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

            //Console.WriteLine($"\t{currentShader}");

            var guid = FindGuid<Shader>(currentShader);

            var directory = Path.GetRelativePath(inputPath, Path.GetDirectoryName(currentShader));
            var file = Path.GetFileName(currentShader);

            processedShaders.Add(currentShader.Replace("\\", "/"), guid);

            var outputFile = Path.Combine(outputPath == "." ? "" : outputPath, directory, file);
            var index = outputFile.IndexOf(inputPath);

            if (index >= 0 && index < outputFile.Length)
            {
                outputFile = outputFile.Substring(0, index) + outputFile.Substring(index + inputPath.Length + 1);
            }

            if (ShouldProcessFile(currentShader, outputFile) == false)
            {
                continue;
            }

            WorkScheduler.Dispatch(Path.GetFileName(currentShader), () =>
            {
                //Console.WriteLine($"\t\t -> {outputFile}");

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

                var shader = new UnprocessedShader()
                {
                    type = currentShader.EndsWith(AssetSerialization.ComputeShaderExtension) ? ShaderType.Compute : ShaderType.VertexFragment,
                };

                try
                {
                    text = File.ReadAllText(currentShader);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Unable to read file: {e}");

                    return;
                }

                if (ShaderParser.Parse(text, shader.type, out var blendMode, out var shaderParameters, out shader.variants,
                    out var instancingParameters, out var vertex, out var fragment, out var compute) == false)
                {
                    Console.WriteLine("\t\tError: File has invalid format");

                    return;
                }

                if (blendMode.HasValue)
                {
                    shader.sourceBlend = blendMode.Value.Item1;
                    shader.destinationBlend = blendMode.Value.Item2;
                }

                foreach (var parameter in shaderParameters)
                {
                    var p = new ShaderParameter
                    {
                        name = parameter.name,
                        vertexAttribute = parameter.vertexAttribute,
                        defaultValue = parameter.initializer,
                        attribute = parameter.attribute,
                        variant = shader.variants.Contains(parameter.variant) ? parameter.variant : null,
                    };

                    p.semantic = parameter.type switch
                    {
                        "varying" => ShaderParameterSemantic.Varying,
                        "uniform" => ShaderParameterSemantic.Uniform,
                        _ => ShaderParameterSemantic.Uniform,
                    };

                    var typeValue = parameter.dataType switch
                    {
                        "int" => (int)ShaderUniformType.Int,
                        "float" => (int)ShaderUniformType.Float,
                        "vec2" => (int)ShaderUniformType.Vector2,
                        "vec3" => (int)ShaderUniformType.Vector3,
                        "vec4" => (int)ShaderUniformType.Vector4,
                        "color" => (int)ShaderUniformType.Color,
                        "texture" => (int)ShaderUniformType.Texture,
                        "mat3" => (int)ShaderUniformType.Matrix3x3,
                        "mat4" => (int)ShaderUniformType.Matrix4x4,
                        _ => -1
                    };

                    switch (parameter.type)
                    {
                        case "ROBuffer":

                            typeValue = (int)ShaderUniformType.ReadOnlyBuffer;

                            p.vertexAttribute = parameter.dataType;

                            break;

                        case "WOBuffer":

                            typeValue = (int)ShaderUniformType.WriteOnlyBuffer;

                            p.vertexAttribute = parameter.dataType;

                            break;

                        case "RWBuffer":

                            typeValue = (int)ShaderUniformType.ReadWriteBuffer;

                            p.vertexAttribute = parameter.dataType;

                            break;
                    }

                    if (typeValue < 0)
                    {
                        Console.WriteLine($"\t\tError: Parameter has invalid type: {parameter.name} {parameter.dataType}");

                        return;
                    }

                    p.type = (ShaderUniformType)typeValue;

                    shader.parameters.Add(p);
                }

                if (instancingParameters != null)
                {
                    foreach (var parameter in instancingParameters)
                    {
                        var p = new ShaderInstancingParameter()
                        {
                            name = parameter.name,
                        };

                        var typeValue = parameter.type switch
                        {
                            "int" => (int)ShaderUniformType.Int,
                            "float" => (int)ShaderUniformType.Float,
                            "vec2" => (int)ShaderUniformType.Vector2,
                            "vec3" => (int)ShaderUniformType.Vector3,
                            "vec4" => (int)ShaderUniformType.Vector4,
                            "color" => (int)ShaderUniformType.Color,
                            "mat3" => (int)ShaderUniformType.Matrix3x3,
                            "mat4" => (int)ShaderUniformType.Matrix4x4,
                            _ => -1
                        };

                        if (typeValue < 0)
                        {
                            Console.WriteLine($"\t\tError: Instancing Parameter has invalid type: {parameter.name} {parameter.type}");

                            return;
                        }

                        p.dataType = (ShaderUniformType)typeValue;

                        shader.instancingParameters.Add(p);
                    }
                }
                else
                {
                    shader.instancingParameters = null;
                }

                switch (shader.type)
                {
                    case ShaderType.Compute:

                        shader.compute = new()
                        {
                            code = compute.content,
                            inputs = compute.inputs,
                            outputs = compute.outputs,
                        };

                        break;

                    case ShaderType.VertexFragment:

                        shader.vertex = new()
                        {
                            code = vertex.content,
                            inputs = vertex.inputs,
                            outputs = vertex.outputs,
                        };

                        shader.fragment = new()
                        {
                            code = fragment.content,
                            inputs = fragment.inputs,
                            outputs = fragment.outputs,
                        };

                        break;
                }

                if (shader.instancingParameters != default)
                {
                    shader.variants.Add(Shader.InstancingKeyword);
                }

                var variants = shader.type == ShaderType.VertexFragment ?
                    Utilities.Combinations(shader.variants
                        .Concat(Shader.DefaultVariants)
                        .ToList()) : [];

                Console.WriteLine($"\t\tCompiling {variants.Count} variants");

                var generatedShader = new SerializableShader()
                {
                    metadata = new ShaderMetadata()
                    {
                        guid = guid,
                        sourceBlend = shader.sourceBlend,
                        destinationBlend = shader.destinationBlend,
                        variants = shader.variants,
                    }
                };

                if (shader.parameters != null)
                {
                    generatedShader.metadata.uniforms = shader.parameters
                        .Where(x => x != null && x.semantic == ShaderParameterSemantic.Uniform)
                        .Select(x => new ShaderUniform()
                        {
                            name = x.name,
                            type = x.type,
                            slot = Array.IndexOf([ShaderUniformType.ReadOnlyBuffer, ShaderUniformType.WriteOnlyBuffer, ShaderUniformType.ReadWriteBuffer], x.type) >= 0 &&
                                int.TryParse(x.defaultValue, out var bufferIndex) ? bufferIndex : -1,
                            attribute = x.attribute,
                            variant = x.variant,
                            defaultValue = x.defaultValue,
                        }).ToList();
                }

                if (shader.instancingParameters != null)
                {
                    generatedShader.metadata.instanceParameters = shader.instancingParameters
                        .Where(x => x != null)
                        .Select(x => new ShaderInstanceParameter()
                        {
                            name = x.name,
                            type = x.dataType,
                        })
                        .ToList();
                }

                foreach (var renderer in renderers)
                {
                    //PSSL and d3d12 ignored for now
                    if (renderer == Renderer.pssl || renderer == Renderer.d3d12)
                    {
                        continue;
                    }

                    var entries = new SerializableShaderEntry();

                    generatedShader.data.Add(renderer switch
                    {
                        Renderer.spirv => RendererType.Vulkan,
                        Renderer.opengl => RendererType.OpenGL,
                        Renderer.opengles => RendererType.OpenGLES,
                        Renderer.d3d11 => RendererType.Direct3D11,
                        Renderer.metal => RendererType.Metal,
                        _ => throw new InvalidOperationException($"Invalid renderer type {renderer} when mapping to regular renderer type"),
                    },
                    entries);

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

                        if (extraDefines.Count > 0)
                        {
                            if (defineString.Length > 0)
                            {
                                defineString += $";{string.Join(";", extraDefines)}";
                            }
                            else
                            {
                                defineString = $"--define {string.Join(";", extraDefines)}";
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

                        Utilities.ExecuteAndCollectProcess(process, null);

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

                    string GetNativeShaderType(ShaderParameter parameter, int index, bool uniform)
                    {
                        var uniformString = uniform ? "uniform " : "";
                        var name = parameter.name;

                        if (int.TryParse(parameter.defaultValue, out var bufferIndex) == false)
                        {
                            bufferIndex = -1;
                        }

                        return parameter.type switch
                        {
                            ShaderUniformType.Int => uniform ? $"{uniformString}vec4 {name}_uniform;\n#define {name} int({name}_uniform.x)\n" : $"float {name}",
                            ShaderUniformType.Float => uniform ? $"{uniformString}vec4 {name}_uniform;\n#define {name} {name}_uniform.x\n" : $"float {name}",
                            ShaderUniformType.Vector2 => uniform ? $"{uniformString}vec4 {name}_uniform;\n#define {name} {name}_uniform.xy\n" : $"vec2 {name}",
                            ShaderUniformType.Vector3 => uniform ? $"{uniformString}vec4 {name}_uniform;\n#define {name} {name}_uniform.xyz\n" : $"vec3 {name}",
                            ShaderUniformType.Color or ShaderUniformType.Vector4 => $"{uniformString}vec4 {name}",
                            ShaderUniformType.Texture => $"SAMPLER2D({name}, {index})",
                            ShaderUniformType.Matrix3x3 => $"{uniformString}mat3 {name}",
                            ShaderUniformType.Matrix4x4 => $"{uniformString}mat4 {name}",
                            ShaderUniformType.ReadOnlyBuffer => $"BUFFER_RO({name}, {parameter.vertexAttribute}, {bufferIndex})",
                            ShaderUniformType.WriteOnlyBuffer => $"BUFFER_WO({name}, {parameter.vertexAttribute}, {bufferIndex})",
                            ShaderUniformType.ReadWriteBuffer => $"BUFFER_RW({name}, {parameter.vertexAttribute}, {bufferIndex})",
                            _ => $"{uniformString}vec4 {name}",
                        };
                    }

                    bool ShaderTypeHasEndTerminator(ShaderUniformType type)
                    {
                        return type switch
                        {
                            ShaderUniformType.Int or ShaderUniformType.Float or ShaderUniformType.Vector2 or ShaderUniformType.Vector3 => false,
                            _ => true,
                        };
                    }

                    void Build(string variantKey, List<string> extraDefines)
                    {
                        string code;

                        var varyingFileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                        var shaderFileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                        var shaderObject = new SerializableShaderData();

                        byte[] Compile(ShaderPiece piece, ShaderCompilerType type, Renderer renderer, ref string code)
                        {
                            code = "";

                            var instancing = variantKey.Contains(Shader.InstancingKeyword);

                            if (type == ShaderCompilerType.vertex)
                            {
                                code += $"$input a_weight, a_indices";

                                if (instancing)
                                {
                                    code += ", i_data0, i_data1, i_data2, i_data3, i_data4";
                                }

                                if ((piece.inputs?.Count ?? 0) > 0)
                                {
                                    code += $", {string.Join(", ", piece.inputs)}";
                                }
                            }
                            else if (type == ShaderCompilerType.fragment)
                            {
                                code += $"$input ";

                                if ((piece.inputs?.Count ?? 0) > 0)
                                {
                                    code += $"{string.Join(", ", piece.inputs)}";
                                }
                            }

                            code += "\n";

                            if (type != ShaderCompilerType.compute &&
                                (piece.outputs?.Count ?? 0) > 0)
                            {
                                code += $"$output  {string.Join(", ", piece.outputs)}\n";
                            }

                            if (type == ShaderCompilerType.compute)
                            {
                                code += "#include <StapleCompute.sh>\n";
                            }
                            else
                            {
                                if (type == ShaderCompilerType.vertex && instancing)
                                {
                                    var fieldIndex = 0;

                                    foreach (var instanceParameter in shader.instancingParameters)
                                    {
                                        var dataIndex = fieldIndex / 4;
                                        var componentIndex = fieldIndex % 4;

                                        code += $"#define {instanceParameter.name} ";

                                        switch (instanceParameter.dataType)
                                        {
                                            case ShaderUniformType.Int:

                                                code += $"int(i_data{dataIndex}";

                                                switch (componentIndex)
                                                {
                                                    case 0:
                                                        code += ".x)\n";

                                                        break;

                                                    case 1:
                                                        code += ".y)\n";

                                                        break;

                                                    case 2:
                                                        code += ".z)\n";

                                                        break;

                                                    case 3:
                                                        code += ".w)\n";

                                                        break;
                                                }

                                                fieldIndex++;

                                                break;

                                            case ShaderUniformType.Float:

                                                code += $"i_data{dataIndex}";

                                                switch (componentIndex)
                                                {
                                                    case 0:
                                                        code += ".x\n";

                                                        break;

                                                    case 1:
                                                        code += ".y\n";

                                                        break;

                                                    case 2:
                                                        code += ".z\n";

                                                        break;

                                                    case 3:
                                                        code += ".w\n";

                                                        break;
                                                }

                                                fieldIndex++;

                                                break;

                                            case ShaderUniformType.Color:
                                            case ShaderUniformType.Vector4:

                                                switch (componentIndex)
                                                {
                                                    case 0:

                                                        code += $"i_data{dataIndex}\n";

                                                        break;

                                                    case 1:

                                                        code += $"vec4(i_data{dataIndex}.y, i_data{dataIndex}.z, i_data{dataIndex}.w, i_data{dataIndex + 1}.x)\n";

                                                        break;

                                                    case 2:

                                                        code += $"vec4(i_data{dataIndex}.z, i_data{dataIndex}.w, i_data{dataIndex + 1}.x, i_data{dataIndex + 1}.y)\n";

                                                        break;

                                                    case 3:

                                                        code += $"vec4(i_data{dataIndex}.w, i_data{dataIndex + 1}.x, i_data{dataIndex + 1}.y, i_data{dataIndex + 1}.z)\n";

                                                        break;
                                                }

                                                fieldIndex += 4;

                                                break;

                                            case ShaderUniformType.Vector2:

                                                switch (componentIndex)
                                                {
                                                    case 0:

                                                        code += $"i_data{dataIndex}.xy\n";

                                                        break;

                                                    case 1:

                                                        code += $"i_data{dataIndex}.yz\n";

                                                        break;

                                                    case 2:

                                                        code += $"i_data{dataIndex}.zw\n";

                                                        break;

                                                    case 3:

                                                        code += $"vec2(i_data{dataIndex}.w, i_data{dataIndex + 1}.x)\n";

                                                        break;
                                                }

                                                fieldIndex += 2;

                                                break;

                                            case ShaderUniformType.Vector3:

                                                switch (componentIndex)
                                                {
                                                    case 0:

                                                        code += $"i_data{dataIndex}.xyz\n";

                                                        break;

                                                    case 1:

                                                        code += $"i_data{dataIndex}.yzw\n";

                                                        break;

                                                    case 2:

                                                        code += $"vec3(i_data{dataIndex}.z, i_data{dataIndex}.w, i_data{dataIndex + 1}.x)\n";

                                                        break;

                                                    case 3:

                                                        code += $"vec3(i_data{dataIndex}.w, i_data{dataIndex + 1}.x, i_data{dataIndex + 1}.y)\n";

                                                        break;
                                                }

                                                fieldIndex += 3;

                                                break;

                                            case ShaderUniformType.Matrix3x3:

                                                switch (componentIndex)
                                                {
                                                    case 0:

                                                        code += $"mtxFromCols(i_data{dataIndex}.xyz, vec3(i_data{dataIndex}.w, i_data{dataIndex + 1}.x, i_data{dataIndex + 1}.y), " +
                                                            $"vec3(i_data{dataIndex + 1}.z, i_data{dataIndex + 1}.w, i_data{dataIndex + 2}.x))\n";

                                                        break;

                                                    case 1:

                                                        code += $"mtxFromCols(i_data{dataIndex}.y, i_data{dataIndex}.z, i_data{dataIndex}.w), " +
                                                            $"vec3(i_data{dataIndex + 1}.x, i_data{dataIndex + 1}.y, i_data{dataIndex + 1}.z), " +
                                                            $"vec3(i_data{dataIndex + 1}.w, i_data{dataIndex + 2}.x, i_data{dataIndex + 2}.y))\n";

                                                        break;

                                                    case 2:

                                                        code += $"mtxFromCols(i_data{dataIndex}.z, i_data{dataIndex}.w, i_data{dataIndex + 1}.x), " +
                                                            $"vec3(i_data{dataIndex + 1}.y, i_data{dataIndex + 1}.z, i_data{dataIndex + 1}.w), " +
                                                            $"vec3(i_data{dataIndex + 2}.x, i_data{dataIndex + 2}.y, i_data{dataIndex + 2}.z))\n";

                                                        break;

                                                    case 3:

                                                        code += $"mtxFromCols(i_data{dataIndex}.w, i_data{dataIndex + 1}.x, i_data{dataIndex + 1}.y), " +
                                                            $"vec3(i_data{dataIndex + 1}.z, i_data{dataIndex + 1}.w, i_data{dataIndex + 2}.x), " +
                                                            $"vec3(i_data{dataIndex + 2}.y, i_data{dataIndex + 2}.z, i_data{dataIndex + 2}.w))\n";

                                                        break;
                                                }

                                                fieldIndex += 9;

                                                break;

                                            case ShaderUniformType.Matrix4x4:

                                                switch (componentIndex)
                                                {
                                                    case 0:

                                                        code += $"mtxFromCols(i_data{dataIndex}, i_data{dataIndex + 1}, i_data{dataIndex + 2}, i_data{dataIndex + 3})\n";

                                                        break;

                                                    case 1:

                                                        code += $"mtxFromCols(vec4(i_data{dataIndex}.y, i_data{dataIndex}.z, i_data{dataIndex}.w, i_data{dataIndex + 1}.x), " +
                                                            $"vec4(i_data{dataIndex + 1}.y, i_data{dataIndex + 1}.z, i_data{dataIndex + 1}.w, i_data{dataIndex + 2}.x), " +
                                                            $"vec4(i_data{dataIndex + 2}.y, i_data{dataIndex + 2}.z, i_data{dataIndex + 2}.w, i_data{dataIndex + 3}.x))\n";

                                                        break;

                                                    case 2:

                                                        code += $"mtxFromCols(vec4(i_data{dataIndex}.z, i_data{dataIndex}.w, i_data{dataIndex + 1}.x, i_data{dataIndex + 1}.y), " +
                                                            $"vec4(i_data{dataIndex + 1}.z, i_data{dataIndex + 1}.w, i_data{dataIndex + 2}.x, i_data{dataIndex + 2}.y), " +
                                                            $"vec4(i_data{dataIndex + 2}.z, i_data{dataIndex + 2}.w, i_data{dataIndex + 3}.x, i_data{dataIndex + 3}.y))\n";

                                                        break;

                                                    case 3:

                                                        code += $"mtxFromCols(vec4(i_data{dataIndex}.w, i_data{dataIndex + 1}.x, i_data{dataIndex + 1}.y, i_data{dataIndex + 1}.z), " +
                                                            $"vec4(i_data{dataIndex + 1}.w, i_data{dataIndex + 2}.x, i_data{dataIndex + 2}.y, i_data{dataIndex + 2}.z), " +
                                                            $"vec4(i_data{dataIndex + 2}.w, i_data{dataIndex + 3}.x, i_data{dataIndex + 3}.y, i_data{dataIndex + 3}.z))\n";

                                                        break;
                                                }

                                                fieldIndex += 16;

                                                break;
                                        }
                                    }
                                }

                                code += "#include <StapleShader.sh>\n";
                            }

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

                                    code += $"{GetNativeShaderType(parameter, counter, true)}";

                                    if (ShaderTypeHasEndTerminator(parameter.type))
                                    {
                                        code += ";";
                                    }

                                    code += "\n";
                                }
                            }

                            code += piece.code;

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

                        switch (shader.type)
                        {
                            case ShaderType.Compute:

                                if (shader.compute == null || (shader.compute.code?.Length ?? 0) == 0)
                                {
                                    Console.WriteLine("\t\tError: Compute Shader missing Compute section");

                                    return;
                                }

                                code = shader.compute.code;

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

                                string computeCode = "";

                                shaderObject.computeShader = Compile(shader.compute, ShaderCompilerType.compute, renderer, ref computeCode);

                                if (shaderObject.computeShader == null)
                                {
                                    Console.WriteLine($"\t\tError: Compute Shader failed to compile\nGenerated code:\n{computeCode}");

                                    return;
                                }

                                entries.data.AddOrSetKey(variantKey, shaderObject);

                                break;

                            case ShaderType.VertexFragment:

                                if (shader.vertex == null || (shader.vertex.code?.Length ?? 0) == 0 ||
                                    shader.fragment == null || (shader.fragment.code?.Length ?? 0) == 0)
                                {
                                    Console.WriteLine("\t\tError: Shader missing vertex or fragment section");

                                    return;
                                }

                                var varying = DefaultVaryingData;

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

                                            varying += $"\n{GetNativeShaderType(parameter, counter, false)}";

                                            if ((parameter.vertexAttribute?.Length ?? 0) > 0)
                                            {
                                                varying += $" : {parameter.vertexAttribute}";
                                            }

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

                                entries.data.AddOrSetKey(variantKey, shaderObject);

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
