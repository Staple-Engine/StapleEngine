using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Staple.Internal;

internal partial class ShaderResource
{
    internal class DefaultUniform
    {
        public string name;
        public ShaderUniformType type;
        public string attribute;
        public string variant;
        public string defaultValue;
        public int slot;

        public static DefaultUniform FromShaderUniform(ShaderUniform uniform)
        {
            return new()
            {
                name = uniform.name,
                type = uniform.type,
                attribute = uniform.attribute,
                variant = uniform.variant,
                defaultValue = uniform.defaultValue,
                slot = uniform.slot,
            };
        }
    }

    internal class ShaderUniformData
    {
        public int offset;
        public int length;
        public byte[] buffer;
        public int binding;
    }

    internal class ShaderInstance
    {
        public IShaderProgram program;
        public int[] keyPieces;

        public byte[] vertexShaderSource;
        public byte[] fragmentShaderSource;
        public byte[] computeShaderSource;
        public VertexFragmentShaderMetrics vertexShaderMetrics;
        public VertexFragmentShaderMetrics fragmentShaderMetrics;
        public ComputeShaderMetrics computeShaderMetrics;
        public VertexAttribute[] attributes = [];
        public ShaderUniformContainer vertexUniforms;
        public ShaderUniformContainer fragmentUniforms;
        public Dictionary<StringID, ShaderUniformMapping> vertexMappings = [];
        public Dictionary<StringID, ShaderUniformField> vertexFields = [];
        public Dictionary<StringID, ShaderUniformData> vertexUniformData = [];
        public Dictionary<StringID, ShaderUniformData> vertexUniformContainers = [];
        public Dictionary<StringID, ShaderUniformMapping> fragmentMappings = [];
        public Dictionary<StringID, ShaderUniformField> fragmentFields = [];
        public Dictionary<StringID, ShaderUniformData> fragmentUniformData = [];
        public Dictionary<StringID, ShaderUniformData> fragmentUniformContainers = [];
        public Dictionary<StringID, int> vertexTextureBindings = [];
        public Dictionary<StringID, int> fragmentTextureBindings = [];
        public readonly Dictionary<StringID, ShaderUniformInfo> uniforms = [];
        public int entityTransformsBufferBinding = -1;
        public int entityTransformIDsBufferBinding = -1;
        public ShaderUniformData renderDataEntry;
        public ShaderUniformData fragmentDataEntry;
    }

    internal static readonly List<DefaultUniform> DefaultUniforms = [];

    internal readonly Dictionary<StringID, string> uniformAttributes = [];

    internal readonly Dictionary<StringID, ShaderUniformAttributeType> uniformAttributeTypes = [];

    internal readonly Dictionary<StringID, ShaderInstance> instances = [];

    internal readonly ShaderMetadata metadata;
    internal readonly BlendMode sourceBlend = BlendMode.Off, destinationBlend = BlendMode.Off;

    public GuidHasher Guid = new();

    [GeneratedRegex("\\[([0-9]+)\\]")]
    private static partial Regex UniformCountRegex();

    private static readonly Regex uniformCountRegex = UniformCountRegex();

    internal bool IsTransparent
    {
        get
        {
            return !((sourceBlend == BlendMode.Off && destinationBlend == BlendMode.Off) ||
                (sourceBlend == BlendMode.One && destinationBlend == BlendMode.Zero) ||
                (sourceBlend == BlendMode.Zero && destinationBlend == BlendMode.One));
        }
    }

    internal ShaderResource(SerializableShader shader, Dictionary<string, SerializableShaderData> entries)
    {
        metadata = shader.metadata;

        foreach(var uniform in metadata.uniforms)
        {
            if(string.IsNullOrEmpty(uniform.attribute))
            {
                continue;
            }

            if(Enum.TryParse<ShaderUniformAttributeType>(uniform.attribute, true, out var defaultAttribute))
            {
                uniformAttributeTypes.AddOrSetKey(uniform.name, defaultAttribute);
            }

            uniformAttributes.AddOrSetKey(uniform.name, uniform.attribute);
        }

        foreach (var pair in entries)
        {
            var vertexMappings = new Dictionary<StringID, ShaderUniformMapping>();
            var vertexFields = new Dictionary<StringID, ShaderUniformField>();
            var fragmentMappings = new Dictionary<StringID, ShaderUniformMapping>();
            var fragmentFields = new Dictionary<StringID, ShaderUniformField>();
            var vertexTextureBindings = new Dictionary<StringID, int>();
            var fragmentTextureBindings = new Dictionary<StringID, int>();
            var vertexUniformData = new Dictionary<StringID, ShaderUniformData>();
            var fragmentUniformData = new Dictionary<StringID, ShaderUniformData>();
            var vertexUniformContainers = new Dictionary<StringID, ShaderUniformData>();
            var fragmentUniformContainers = new Dictionary<StringID, ShaderUniformData>();
            var entityTransformsBufferBinding = -1;
            var entityTransformIDsBufferBinding = -1;

            foreach (var buffer in pair.Value.vertexUniforms.storageBuffers)
            {
                if (buffer.name == "StapleEntityTransforms")
                {
                    entityTransformsBufferBinding = buffer.binding;
                }
                else if (buffer.name == "StapleEntityTransformIDs")
                {
                    entityTransformIDsBufferBinding = buffer.binding;
                }
            }

            foreach (var uniform in pair.Value.vertexUniforms.uniforms)
            {
                vertexMappings.AddOrSetKey(uniform.name, uniform);

                var buffer = new byte[uniform.size];

                vertexUniformData.Add(uniform.name, new()
                {
                    binding = uniform.binding,
                    length = uniform.size,
                    buffer = buffer,
                });

                var uniformData = new ShaderUniformData()
                {
                    binding = uniform.binding,
                    length = uniform.size,
                    buffer = buffer,
                };

                vertexUniformContainers.Add(uniform.name, uniformData);

                foreach (var field in uniform.fields)
                {
                    vertexFields.AddOrSetKey(field.name, field);

                    vertexUniformData.Add(field.name, new()
                    {
                        offset = field.offset,
                        length = field.size,
                        buffer = buffer,
                    });
                }
            }

            foreach (var uniform in pair.Value.fragmentUniforms.uniforms)
            {
                fragmentMappings.AddOrSetKey(new(uniform.name), uniform);

                var buffer = new byte[uniform.size];

                fragmentUniformData.Add(uniform.name, new()
                {
                    binding = uniform.binding,
                    length = uniform.size,
                    buffer = buffer,
                });

                fragmentUniformContainers.Add(uniform.name, new()
                {
                    binding = uniform.binding,
                    length = uniform.size,
                    buffer = buffer,
                });

                foreach (var field in uniform.fields)
                {
                    fragmentFields.AddOrSetKey(new(field.name), field);

                    fragmentUniformData.Add(field.name, new()
                    {
                        offset = field.offset,
                        length = field.size,
                        buffer = buffer,
                    });
                }
            }

            foreach (var texture in pair.Value.vertexUniforms.textures)
            {
                vertexTextureBindings.Add(texture.name, texture.binding);
            }

            foreach (var texture in pair.Value.fragmentUniforms.textures)
            {
                fragmentTextureBindings.Add(texture.name, texture.binding);
            }

            var instance = new ShaderInstance()
            {
                keyPieces = pair.Key
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.GetHashCode())
                    .ToArray(),
                vertexShaderSource = pair.Value.vertexShader,
                fragmentShaderSource = pair.Value.fragmentShader,
                computeShaderSource = pair.Value.computeShader,
                vertexShaderMetrics = pair.Value.vertexMetrics,
                fragmentShaderMetrics = pair.Value.fragmentMetrics,
                computeShaderMetrics = pair.Value.computeMetrics,
                attributes = pair.Value.vertexAttributes,
                vertexUniforms = pair.Value.vertexUniforms,
                fragmentUniforms = pair.Value.fragmentUniforms,
                vertexMappings = vertexMappings,
                vertexFields = vertexFields,
                vertexTextureBindings = vertexTextureBindings,
                vertexUniformContainers = vertexUniformContainers,
                vertexUniformData = vertexUniformData,
                fragmentMappings = fragmentMappings,
                fragmentFields = fragmentFields,
                fragmentTextureBindings = fragmentTextureBindings,
                fragmentUniformContainers = fragmentUniformContainers,
                fragmentUniformData = fragmentUniformData,
                entityTransformIDsBufferBinding = entityTransformIDsBufferBinding,
                entityTransformsBufferBinding = entityTransformsBufferBinding,
            };

            instances.AddOrSetKey(pair.Key, instance);
        }

        sourceBlend = metadata.sourceBlend;
        destinationBlend = metadata.destinationBlend;
    }

    /// <summary>
    /// Attempts to get the attribute a uniform might have
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="attribute">The attribute, or null</param>
    /// <returns>Whether the attribute was found</returns>
    public bool TryGetUniformAttribute(StringID name, out string attribute)
    {
        return uniformAttributes.TryGetValue(name, out attribute);
    }

    /// <summary>
    /// Attempts to get a default attribute type that a uniform might have
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="attribute">The attribute, or <see cref="ShaderUniformAttributeType.None"/></param>
    /// <returns>Whether the attribute was found</returns>
    public bool TryGetUniformAttributeType(StringID name, out ShaderUniformAttributeType attribute)
    {
        return uniformAttributeTypes.TryGetValue(name, out attribute);
    }

    private static string NormalizeUniformName(string name, ShaderUniformType type)
    {
        if (uniformCountRegex.IsMatch(name))
        {
            name = name.Replace(uniformCountRegex.Match(name).Value, string.Empty);
        }

        return name;
    }

    private static int NormalizeUniformCount(string name)
    {
        if (!uniformCountRegex.IsMatch(name))
        {
            return 1;
        }

        var match = uniformCountRegex.Match(name);

        if (match.Groups.Count == 2)
        {
            return int.TryParse(match.Groups[1].Value, out var value) ? value : 1;
        }

        return 1;
    }

    internal bool Create()
    {
        foreach (var pair in instances)
        {
            pair.Value.program = RenderSystem.Backend.CreateShaderVertexFragment(pair.Value.vertexShaderSource, pair.Value.fragmentShaderSource,
                pair.Value.vertexShaderMetrics, pair.Value.fragmentShaderMetrics);

            if (pair.Value.program == null)
            {
                return false;
            }

            if (pair.Value.uniforms.Count == 0)
            {
                foreach (var uniform in metadata.uniforms)
                {
                    AddUniform(DefaultUniform.FromShaderUniform(uniform), pair.Value);
                }

                void EnsureUniform(DefaultUniform u)
                {
                    var uniform = GetUniform(u.name, pair.Value);

                    if (uniform == null)
                    {
                        AddUniform(u, pair.Value);
                    }
                }

                foreach (var uniform in DefaultUniforms)
                {
                    EnsureUniform(uniform);
                }
            }
        }

        return true;
    }

    internal static void AddUniform(DefaultUniform uniform, ShaderInstance instance)
    {
        var normalizedName = NormalizeUniformName(uniform.name, uniform.type);

        if (instance.uniforms.ContainsKey(uniform.name) || instance.uniforms.ContainsKey(normalizedName))
        {
            return;
        }

        var u = new ShaderUniformInfo()
        {
            uniform = new()
            {
                name = normalizedName,
                type = uniform.type,
                attribute = uniform.attribute,
                variant = uniform.variant,
                defaultValue = uniform.defaultValue,
                slot = uniform.slot,
            },
            handle = new(normalizedName),
            count = 1,
        };

        instance.uniforms.Add(normalizedName, u);

        if (!instance.uniforms.ContainsKey(uniform.name))
        {
            instance.uniforms.Add(uniform.name, new()
            {
                count = u.count,
                isAlias = true,
                uniform = new()
                {
                    name = u.uniform.name,
                    type = uniform.type,
                    attribute = uniform.attribute,
                    variant = uniform.variant,
                    defaultValue = uniform.defaultValue,
                    slot = uniform.slot,
                },
                handle = new(normalizedName),
            });
        }
    }

    internal static ShaderUniformInfo GetUniform(StringID name, ShaderInstance instance)
    {
        if (instance.uniforms.TryGetValue(name, out var u))
        {
            return u;
        }

        foreach (var uniform in instance.vertexUniforms.uniforms)
        {
            if (uniform.name == name)
            {
                return new()
                {
                    uniform = new()
                    {
                        name = uniform.name,
                        type = uniform.type,
                    },
                    handle = name,
                    count = uniform.count,
                };
            }
        }

        foreach (var uniform in instance.fragmentUniforms.uniforms)
        {
            if (uniform.name == name)
            {
                return new()
                {
                    uniform = new()
                    {
                        name = uniform.name,
                        type = uniform.type,
                    },
                    handle = name,
                    count = uniform.count,
                };
            }
        }

        foreach (var pair in instance.vertexFields)
        {
            if (pair.Key == name)
            {
                return new()
                {
                    uniform = new()
                    {
                        name = pair.Value.name,
                        type = pair.Value.type,
                    },
                    handle = pair.Key,
                    count = pair.Value.count,
                };
            }
        }

        foreach (var pair in instance.fragmentFields)
        {
            if (pair.Key == name)
            {
                return new()
                {
                    uniform = new()
                    {
                        name = pair.Value.name,
                        type = pair.Value.type,
                    },
                    handle = pair.Key,
                    count = pair.Value.count,
                };
            }
        }

        return null;
    }

    internal ShaderUniformInfo GetUniform(StringID name, StringID variantKey)
    {
        if (!instances.TryGetValue(variantKey, out var instance))
        {
            return null;
        }

        return GetUniform(name, instance);
    }

    internal bool TryGetUniformData(Shader owner, StringID variantKey, ShaderHandle handle, out ShaderUniformInfo uniform,
        out (int, byte[])? vertexData, out (int, byte[])? fragmentData)
    {
        uniform = default;
        vertexData = default;
        fragmentData = default;

        if (!handle.TryGetUniform(owner, out uniform) ||
            !instances.TryGetValue(variantKey, out var shaderInstance))
        {
            return false;
        }

        if (shaderInstance.vertexUniformData.TryGetValue(uniform.handle, out var d))
        {
            vertexData = (d.offset, d.buffer);
        }

        if (shaderInstance.fragmentUniformData.TryGetValue(uniform.handle, out d))
        {
            fragmentData = (d.offset, d.buffer);
        }

        return vertexData != null || fragmentData != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CanStoreUniformData((int, byte[]) data, int size)
    {
        //Logic: If we have an array of 1 element, 0 + 1 < 1 fails, so must be <=
        return data.Item1 >= 0 && data.Item1 + size <= data.Item2.Length;
    }

    private static void SetValueInternal(Span<byte> source, (int, byte[])? vertexData, (int, byte[])? fragmentData)
    {
        if (vertexData != null && CanStoreUniformData(vertexData.Value, source.Length))
        {
            var target = new Span<byte>(vertexData.Value.Item2, vertexData.Value.Item1, source.Length);

            source.CopyTo(target);
        }

        if (fragmentData != null && CanStoreUniformData(fragmentData.Value, source.Length))
        {
            var target = new Span<byte>(fragmentData.Value.Item2, fragmentData.Value.Item1, source.Length);

            source.CopyTo(target);
        }
    }

    internal void SetValue<T>(Shader owner, StringID variantKey, ShaderHandle handle, T value) where T : unmanaged
    {
        if (!TryGetUniformData(owner, variantKey, handle, out _, out var vertexData, out var fragmentData))
        {
            return;
        }

        var size = Marshal.SizeOf<T>();

        unsafe
        {
            var source = new Span<byte>(&value, size);

            SetValueInternal(source, vertexData, fragmentData);
        }
    }

    internal void SetValue<T>(Shader owner, StringID variantKey, ShaderHandle handle, ReadOnlySpan<T> value) where T : unmanaged
    {
        if (!TryGetUniformData(owner, variantKey, handle, out var uniform, out var vertexData, out var fragmentData))
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                var count = value.Length < uniform.count ? value.Length : uniform.count;
                var size = Marshal.SizeOf<T>() * count;
                var source = new Span<byte>(ptr, size);

                SetValueInternal(source, vertexData, fragmentData);
            }
        }
    }

    public void Destroy()
    {
        foreach (var pair in instances)
        {
            pair.Value.program?.Destroy();

            pair.Value.program = null;
        }
    }
}
