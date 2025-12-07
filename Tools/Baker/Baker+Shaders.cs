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
using System.Threading;

namespace Baker;

static partial class Program
{
    private static void ProcessShaders(AppPlatform platform, string shaderCompilerPath, string shaderTranspilerPath,
        string inputPath, string outputPath, List<string> shaderDefines, List<Renderer> renderers)
    {
        var pieces = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).ToList();

        while(pieces.Count > 0 && pieces.LastOrDefault() != "StapleEngine")
        {
            pieces.RemoveAt(pieces.Count - 1);
        }

        var stapleBase = Path.Combine(string.Join(Path.DirectorySeparatorChar, pieces));

        var shaderInclude = $"-I \"{Path.GetFullPath(Path.Combine(stapleBase, "Tools", "ShaderIncludes"))}\"";

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

        var shaderDefineString = "";
        
        foreach(var define in shaderDefines)
        {
            shaderDefineString += $"-D{define} ";
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

            WorkScheduler.Main.Dispatch(Path.GetFileName(currentShader), () =>
            {
                //Console.WriteLine($"\t\t -> {outputFile}");

                var success = true;

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
                    out var instancingParameters, out var vertexInputs, out var vertex, out var fragment, out var compute) == false)
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

                    p.semantic = ShaderParameterSemantic.Uniform;

                    var typeValue = parameter.dataType switch
                    {
                        "int" => (int)ShaderUniformType.Int,
                        "float" => (int)ShaderUniformType.Float,
                        "float2" => (int)ShaderUniformType.Vector2,
                        "float3" => (int)ShaderUniformType.Vector3,
                        "float4" => (int)ShaderUniformType.Vector4,
                        "color" => (int)ShaderUniformType.Color,
                        "texture" => (int)ShaderUniformType.Texture,
                        "float3x3" => (int)ShaderUniformType.Matrix3x3,
                        "float4x4" => (int)ShaderUniformType.Matrix4x4,
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
                            "float2" => (int)ShaderUniformType.Vector2,
                            "float3" => (int)ShaderUniformType.Vector3,
                            "float4" => (int)ShaderUniformType.Vector4,
                            "color" => (int)ShaderUniformType.Color,
                            "float3x3" => (int)ShaderUniformType.Matrix3x3,
                            "float4x4" => (int)ShaderUniformType.Matrix4x4,
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
                        };

                        break;

                    case ShaderType.VertexFragment:

                        shader.vertex = new()
                        {
                            code = vertex.content,
                        };

                        shader.fragment = new()
                        {
                            code = fragment.content,
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
                    },
                };

                if (shader.parameters != null)
                {
                    generatedShader.metadata.uniforms = shader.parameters
                        .Where(x => x != null && x.semantic == ShaderParameterSemantic.Uniform)
                        .Select(x => new ShaderUniform()
                        {
                            name = x.name,
                            type = x.type,
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
                    if(success == false)
                    {
                        break;
                    }

                    var entries = new SerializableShaderEntry();

                    Lock entryLock = new();

                    generatedShader.data.Add(renderer switch
                    {
                        Renderer.spirv => RendererType.Vulkan,
                        Renderer.d3d12 => RendererType.Direct3D12,
                        Renderer.metal => RendererType.Metal,
                        _ => throw new InvalidOperationException($"Invalid renderer type {renderer} when mapping to regular renderer type"),
                    },
                    entries);

                    byte[] ProcessShader(string shaderFileName, List<string> extraDefines,
                        ShaderCompilerType shaderType, Renderer renderer, out object shaderMetrics,
                        out ShaderReflectionData reflectionData)
                    {
                        var shaderExtension = shaderType switch
                        {
                            ShaderCompilerType.vertex => ".vert.spv",
                            ShaderCompilerType.fragment => ".frag.spv",
                            ShaderCompilerType.compute => ".comp.spv",
                            _ => "",
                        };

                        var destinationFormat = "";
                        var outShaderFileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                        var outShaderFileNameTranspiled = $"{Path.Combine(Path.GetTempPath(), Path.GetTempFileName())}{shaderExtension}";
                        var reflectionJsonFileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

                        var skip = false;

                        switch (renderer)
                        {
                            case Renderer.d3d12:

                                destinationFormat = "-d dxil";

                                break;

                            case Renderer.metal:

                                destinationFormat = "-d msl";

                                break;

                            case Renderer.spirv:

                                skip = true;
                                outShaderFileName = outShaderFileNameTranspiled;

                                break;
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
                            foreach(var define in extraDefines)
                            {
                                defineString += $" -D{define}";
                            }
                        }

                        switch(shaderType)
                        {
                            case ShaderCompilerType.vertex:

                                defineString += " -DSTAPLE_VERTEX_SHADER";

                                break;

                            case ShaderCompilerType.fragment:

                                defineString += " -DSTAPLE_FRAGMENT_SHADER";

                                break;

                            case ShaderCompilerType.compute:

                                defineString += " -DSTAPLE_COMPUTE_SHADER";

                                break;
                        }

                        {
                            var stage = shaderType switch
                            {
                                ShaderCompilerType.vertex => "-stage vertex",
                                ShaderCompilerType.fragment => "-stage fragment",
                                ShaderCompilerType.compute => "-stage compute",
                                _ => ""
                            };

                            var entry = shaderType switch
                            {
                                ShaderCompilerType.vertex => "-entry VertexMain",
                                ShaderCompilerType.fragment => "-entry FragmentMain",
                                _ => "",
                            };

                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = shaderTranspilerPath,
                                    Arguments = $"-o \"{outShaderFileNameTranspiled}\" -profile sm_6_0 -target spirv {stage} {entry} {defineString} {shaderInclude} -reflection-json \"{reflectionJsonFileName}\" \"{shaderFileName}\"",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true,
                                }
                            };

                            Utilities.ExecuteAndCollectProcess(process, null);

                            if (process.ExitCode != 0)
                            {
                                shaderMetrics = null;
                                reflectionData = null;

                                Console.WriteLine($"Arguments: {process.StartInfo.Arguments}");

                                try
                                {
                                    File.Delete(outShaderFileNameTranspiled);
                                }
                                catch (Exception)
                                {
                                }

                                return null;
                            }

                            process.Close();
                        }

                        try
                        {
                            var text = File.ReadAllText(reflectionJsonFileName);

                            reflectionData = JsonConvert.DeserializeObject<ShaderReflectionData>(text);

                            //For debugging
                            //File.Copy(reflectionJsonFileName, $"{outputFile}.reflection.json", true);
                        }
                        catch (Exception e)
                        {
                            shaderMetrics = null;
                            reflectionData = null;

                            Console.WriteLine($"Failed to process reflection: {e}");

                            File.Delete(reflectionJsonFileName);

                            return null;
                        }

                        if (skip == false)
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = shaderCompilerPath,
                                    Arguments = $"\"{outShaderFileNameTranspiled}\" -o \"{outShaderFileName}\" {defineString} {shaderInclude} {destinationFormat}",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true,
                                }
                            };

                            Utilities.ExecuteAndCollectProcess(process, null);

                            if (process.ExitCode != 0)
                            {
                                shaderMetrics = null;

                                Console.WriteLine($"Arguments: {process.StartInfo.Arguments}");

                                try
                                {
                                    File.Delete(outShaderFileNameTranspiled);
                                    File.Delete(outShaderFileName);
                                }
                                catch (Exception)
                                {
                                }

                                return null;
                            }

                            process.Close();
                        }

                        {
                            var shaderData = Array.Empty<byte>();

                            try
                            {
                                shaderData = File.ReadAllBytes(outShaderFileName);
                            }
                            catch (Exception e)
                            {
                                shaderMetrics = null;

                                return null;
                            }

                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = shaderCompilerPath,
                                    Arguments = $"\"{outShaderFileNameTranspiled}\" -o \"{outShaderFileName}.json\" {defineString} {shaderInclude} -d json",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true,
                                }
                            };

                            Utilities.ExecuteAndCollectProcess(process, null);

                            if (process.ExitCode != 0)
                            {
                                shaderMetrics = null;

                                Console.WriteLine($"Arguments: {process.StartInfo.Arguments}");

                                try
                                {
                                    File.Delete(outShaderFileName);
                                    File.Delete(outShaderFileNameTranspiled);
                                }
                                catch (Exception)
                                {
                                }

                                return null;
                            }

                            process.Close();

                            try
                            {
                                var text = File.ReadAllText($"{outShaderFileName}.json");

                                File.Delete($"{outShaderFileName}.json");
                                File.Delete(outShaderFileName);
                                File.Delete(outShaderFileNameTranspiled);

                                var stats = JsonConvert.DeserializeObject<Dictionary<string, object>>(text);

                                switch(shaderType)
                                {
                                    case ShaderCompilerType.vertex:
                                    case ShaderCompilerType.fragment:

                                        {
                                            if (stats.TryGetValue("samplers", out var samplersObject) &&
                                                samplersObject is long samplerCount &&
                                                stats.TryGetValue("storage_textures", out var storageTexturesObject) &&
                                                storageTexturesObject is long storageTextureCount &&
                                                stats.TryGetValue("storage_buffers", out var storageBuffersObject) &&
                                                storageBuffersObject is long storageBufferCount &&
                                                stats.TryGetValue("uniform_buffers", out var uniformBuffersObject) &&
                                                uniformBuffersObject is long uniformBufferCount)
                                            {
                                                var metrics = new VertexFragmentShaderMetrics()
                                                {
                                                    samplerCount = (int)samplerCount,
                                                    storageBufferCount = (int)storageBufferCount,
                                                    storageTextureCount = (int)storageTextureCount,
                                                    uniformBufferCount = (int)uniformBufferCount,
                                                };

                                                shaderMetrics = metrics;

                                                return shaderData;
                                            }
                                        }

                                        break;

                                    case ShaderCompilerType.compute:

                                        {
                                            if (stats.TryGetValue("samplers", out var samplersObject) &&
                                                samplersObject is long samplerCount &&
                                                stats.TryGetValue("read_only_storage_textures", out var readOnlyStorageTexturesObject) &&
                                                readOnlyStorageTexturesObject is long readOnlyStorageTextureCount &&
                                                stats.TryGetValue("read_only_storage_buffers", out var readOnlyStorageBuffersObject) &&
                                                readOnlyStorageBuffersObject is long readOnlyStorageBufferCount &&
                                                stats.TryGetValue("read_write_storage_textures", out var readWriteStorageTexturesObject) &&
                                                readWriteStorageTexturesObject is long readWriteStorageTextureCount &&
                                                stats.TryGetValue("read_write_storage_buffers", out var readWriteStorageBuffersObject) &&
                                                readWriteStorageBuffersObject is long readWriteStorageBufferCount &&
                                                stats.TryGetValue("uniform_buffers", out var uniformBuffersObject) &&
                                                uniformBuffersObject is long uniformBufferCount &&
                                                stats.TryGetValue("thread_count_x", out var threadCountXObject) &&
                                                threadCountXObject is long threadCountX &&
                                                stats.TryGetValue("thread_count_y", out var threadCountYObject) &&
                                                threadCountYObject is long threadCountY &&
                                                stats.TryGetValue("thread_count_z", out var threadCountZObject) &&
                                                threadCountZObject is long threadCountZ)
                                            {
                                                var metrics = new ComputeShaderMetrics()
                                                {
                                                    samplerCount = (int)samplerCount,
                                                    readOnlyStorageBufferCount = (int)readOnlyStorageBufferCount,
                                                    readOnlyStorageTextureCount = (int)readOnlyStorageTextureCount,
                                                    readWriteStorageBufferCount = (int)readWriteStorageBufferCount,
                                                    readWriteStorageTextureCount = (int)readWriteStorageTextureCount,
                                                    threadCountX = (int)threadCountX,
                                                    threadCountY = (int)threadCountY,
                                                    threadCountZ = (int)threadCountZ,
                                                    uniformBufferCount = (int)uniformBufferCount,
                                                };

                                                shaderMetrics = metrics;

                                                return shaderData;
                                            }
                                        }

                                        break;
                                }

                                shaderMetrics = null;

                                return null;
                            }
                            catch (Exception e)
                            {
                                shaderMetrics = null;

                                File.Delete(outShaderFileName);
                                File.Delete(outShaderFileNameTranspiled);

                                return null;
                            }
                        }
                    }

                    bool Build(string variantKey, List<string> extraDefines)
                    {
                        string code;

                        var shaderFileName = $"{Path.Combine(Path.GetTempPath(), Path.GetTempFileName())}.slang";

                        var shaderObject = new SerializableShaderData()
                        {
                            vertexAttributes = vertexInputs.ToArray(),
                        };

                        byte[] Compile(ShaderPiece piece, ShaderCompilerType type, Renderer renderer, ref string code,
                            out object shaderMetrics, out ShaderUniformContainer uniforms)
                        {
                            code = "import Staple;\n";

                            var instancing = variantKey.Contains(Shader.InstancingKeyword);

                            code += piece.code;

                            try
                            {
                                File.WriteAllText(shaderFileName, code);
                            }
                            catch (Exception)
                            {
                                shaderMetrics = null;
                                uniforms = null;

                                return null;
                            }

                            var data = ProcessShader(shaderFileName, extraDefines, type, renderer, out shaderMetrics,
                                out var reflection);

                            if(reflection == null)
                            {
                                uniforms = new();
                            }
                            else
                            {
                                uniforms = reflection.ToContainer();
                            }

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

                                    return false;
                                }

                                code = shader.compute.code;

                                try
                                {
                                    File.WriteAllText(shaderFileName, code);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("\t\tError: Failed to write shader data");

                                    return false;
                                }

                                generatedShader.metadata.type = ShaderType.Compute;

                                string computeCode = "";

                                shaderObject.computeShader = Compile(shader.compute, ShaderCompilerType.compute, renderer, ref computeCode,
                                    out var computeMetrics, out var reflectionData);

                                if (shaderObject.computeShader == null)
                                {
                                    Console.WriteLine($"\t\tError: Compute Shader failed to compile\nGenerated code:\n{computeCode}");

                                    return false;
                                }

                                if (computeMetrics is ComputeShaderMetrics c)
                                {
                                    shaderObject.computeMetrics = c;
                                }

                                if(reflectionData != null)
                                {
                                    shaderObject.computeUniforms = reflectionData;

                                    shaderObject.computeMetrics?.uniformBufferCount = reflectionData.uniforms
                                        .Where(x =>
                                            x.type != ShaderUniformType.ReadWriteBuffer &&
                                            x.type != ShaderUniformType.ReadOnlyBuffer &&
                                            x.type != ShaderUniformType.WriteOnlyBuffer)
                                        .Count();
                                }

                                lock (entryLock)
                                {
                                    entries.data.AddOrSetKey(variantKey, shaderObject);
                                }

                                return true;

                            case ShaderType.VertexFragment:

                                if (shader.vertex == null || (shader.vertex.code?.Length ?? 0) == 0 ||
                                    shader.fragment == null || (shader.fragment.code?.Length ?? 0) == 0)
                                {
                                    Console.WriteLine("\t\tError: Shader missing vertex or fragment section");

                                    return false;
                                }

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

                                            /*
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
                                            */
                                        }
                                    }

                                    if (error)
                                    {
                                        Console.WriteLine("\t\tError: Invalid parameter detected");

                                        return false;
                                    }
                                }

                                string vertexCode = "";
                                string fragmentCode = "";

                                shaderObject.vertexShader = Compile(shader.vertex, ShaderCompilerType.vertex, renderer, ref vertexCode,
                                    out var vertexMetrics, out var vertexReflectionData);

                                shaderObject.fragmentShader = Compile(shader.fragment, ShaderCompilerType.fragment, renderer, ref fragmentCode,
                                    out var fragmentMetrics, out var fragmentReflectionData);

                                if (shaderObject.vertexShader == null || shaderObject.fragmentShader == null)
                                {
                                    Console.WriteLine($"Failed to build shader.\nGenerated code:\nVertex:\n{vertexCode}\nFragment:\n{fragmentCode}\n");

                                    return false;
                                }

                                if (vertexMetrics is VertexFragmentShaderMetrics v)
                                {
                                    shaderObject.vertexMetrics = v;
                                }

                                if (fragmentMetrics is VertexFragmentShaderMetrics f)
                                {
                                    shaderObject.fragmentMetrics = f;
                                }

                                if (vertexReflectionData != null)
                                {
                                    shaderObject.vertexUniforms = vertexReflectionData;

                                    shaderObject.vertexMetrics?.uniformBufferCount = vertexReflectionData.uniforms
                                        .Where(x =>
                                            x.type != ShaderUniformType.ReadWriteBuffer &&
                                            x.type != ShaderUniformType.ReadOnlyBuffer &&
                                            x.type != ShaderUniformType.WriteOnlyBuffer)
                                        .Count();
                                }

                                if (fragmentReflectionData != null)
                                {
                                    shaderObject.fragmentUniforms = fragmentReflectionData;

                                    shaderObject.fragmentMetrics?.uniformBufferCount = fragmentReflectionData.uniforms
                                        .Where(x =>
                                            x.type != ShaderUniformType.ReadWriteBuffer &&
                                            x.type != ShaderUniformType.ReadOnlyBuffer &&
                                            x.type != ShaderUniformType.WriteOnlyBuffer)
                                        .Count();
                                }

                                lock (entryLock)
                                {
                                    entries.data.AddOrSetKey(variantKey, shaderObject);
                                }

                                return true;
                        }

                        return false;
                    }

                    if (shader.type == ShaderType.Compute)
                    {
                        success = success && Build("", []);
                    }
                    else
                    {
                        var workScheduler = new WorkScheduler()
                        {
                            logCompletion = false,
                        };

                        foreach (var pair in variants)
                        {
                            var variantKey = string.Join(" ", pair);

                            workScheduler.Dispatch(variantKey, () =>
                            {
                                success = success && Build(variantKey, pair);
                            });
                        }

                        workScheduler.WaitForTasks();
                    }
                }

                if(success == false)
                {
                    return;
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
