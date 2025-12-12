using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Staple.Internal;

/// <summary>
/// Shader resource
/// </summary>
public partial class Shader : IGuidAsset
{
    public static readonly string SkinningKeyword = "SKINNING";
    public static readonly string LitKeyword = "LIT";
    public static readonly string HalfLambertKeyword = "HALF_LAMBERT";
    public static readonly string InstancingKeyword = "INSTANCING";

    public static readonly string[] DefaultVariants =
    [
        SkinningKeyword,
    ];

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

    public class UniformInfo
    {
        internal ShaderUniform uniform;
        internal int count = 1;
        internal bool isAlias = false;
        internal StringID handle;

        public override string ToString()
        {
            return uniform.name;
        }
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
        public Dictionary<StringID, ShaderUniformMapping> fragmentMappings = [];
        public Dictionary<StringID, ShaderUniformField> fragmentFields = [];
        public Dictionary<StringID, int> vertexTextureBindings = [];
        public Dictionary<StringID, int> fragmentTextureBindings = [];
    }

    internal readonly ShaderMetadata metadata;
    internal readonly BlendMode sourceBlend = BlendMode.Off, destinationBlend = BlendMode.Off;

    internal static readonly List<DefaultUniform> DefaultUniforms = [];

    internal readonly Dictionary<StringID, ShaderInstance> instances = [];

    private Dictionary<StringID, UniformInfo> uniforms = [];

    [GeneratedRegex("\\[([0-9]+)\\]")]
    private static partial Regex UniformCountRegex();

    private static readonly Regex uniformCountRegex = UniformCountRegex();

    private readonly GuidHasher guidHasher = new();

    public GuidHasher Guid => guidHasher;

    internal bool IsTransparent
    {
        get
        {
            return ((sourceBlend == BlendMode.Off && destinationBlend == BlendMode.Off) ||
                (sourceBlend == BlendMode.One && destinationBlend == BlendMode.Zero) ||
                (sourceBlend == BlendMode.Zero && destinationBlend == BlendMode.One)) == false;
        }
    }

    /// <summary>
    /// Whether this shader has been disposed
    /// </summary>
    public bool Disposed { get; internal set; } = false;

    public static object Create(string path)
    {
        return ResourceManager.instance.LoadShader(path);
    }

    internal Shader(SerializableShader shader, Dictionary<string, SerializableShaderData> entries)
    {
        metadata = shader.metadata;

        foreach(var pair in entries)
        {
            var vertexMappings = new Dictionary<StringID, ShaderUniformMapping>();
            var vertexFields = new Dictionary<StringID, ShaderUniformField>();
            var fragmentMappings = new Dictionary<StringID, ShaderUniformMapping>();
            var fragmentFields = new Dictionary<StringID, ShaderUniformField>();
            var vertexTextureBindings = new Dictionary<StringID, int>();
            var fragmentTextureBindings = new Dictionary<StringID, int>();

            foreach (var uniform in pair.Value.vertexUniforms.uniforms)
            {
                vertexMappings.AddOrSetKey(new(uniform.name), uniform);

                foreach(var field in uniform.fields)
                {
                    vertexFields.AddOrSetKey(new(field.name), field);
                }
            }

            foreach (var uniform in pair.Value.fragmentUniforms.uniforms)
            {
                fragmentMappings.AddOrSetKey(new(uniform.name), uniform);

                foreach (var field in uniform.fields)
                {
                    fragmentFields.AddOrSetKey(new(field.name), field);
                }
            }

            foreach(var texture in pair.Value.vertexUniforms.textures)
            {
                vertexTextureBindings.Add(texture.name, texture.binding);
            }

            foreach (var texture in pair.Value.fragmentUniforms.textures)
            {
                fragmentTextureBindings.Add(texture.name, texture.binding);
            }

            instances.AddOrSetKey(new(pair.Key), new()
            {
                keyPieces = pair.Key.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.GetHashCode()).ToArray(),
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
                fragmentMappings = fragmentMappings,
                fragmentFields = fragmentFields,
                fragmentTextureBindings = fragmentTextureBindings,
            });
        }

        sourceBlend = metadata.sourceBlend;
        destinationBlend = metadata.destinationBlend;
    }

    ~Shader()
    {
        Destroy();
    }

    private static string NormalizeUniformName(string name, ShaderUniformType type)
    {
        if(uniformCountRegex.IsMatch(name))
        {
            name = name.Replace(uniformCountRegex.Match(name).Value, string.Empty);
        }

        return name;
    }

    private static int NormalizeUniformCount(string name)
    {
        if(uniformCountRegex.IsMatch(name) == false)
        {
            return 1;
        }

        var match = uniformCountRegex.Match(name);

        if(match.Groups.Count == 2)
        {
            return int.TryParse(match.Groups[1].Value, out var value) ? value : 1;
        }

        return 1;
    }

    internal unsafe bool Create()
    {
        foreach(var pair in instances)
        {
            pair.Value.program = RenderSystem.Backend.CreateShaderVertexFragment(pair.Value.vertexShaderSource, pair.Value.fragmentShaderSource,
                pair.Value.vertexShaderMetrics, pair.Value.fragmentShaderMetrics, pair.Value.vertexUniforms, pair.Value.fragmentUniforms);

            if(pair.Value.program == null)
            {
                return false;
            }
        }

        if (uniforms.Count == 0)
        {
            foreach (var uniform in metadata.uniforms)
            {
                AddUniform(DefaultUniform.FromShaderUniform(uniform));
            }

            void EnsureUniform(DefaultUniform u)
            {
                var uniform = GetUniform(u.name);

                if (uniform == null)
                {
                    AddUniform(u);
                }
            }

            foreach(var uniform in DefaultUniforms)
            {
                EnsureUniform(uniform);
            }
        }

        Disposed = false;

        return true;
    }

    internal void AddUniform(DefaultUniform uniform)
    {
        var normalizedName = NormalizeUniformName(uniform.name, uniform.type);

        if(uniforms.ContainsKey(uniform.name) || uniforms.ContainsKey(normalizedName))
        {
            return;
        }

        var u = new UniformInfo()
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
            count = NormalizeUniformCount(uniform.name),
        };

        uniforms.Add(normalizedName, u);

        if (uniforms.ContainsKey(uniform.name) == false)
        {
            uniforms.Add(uniform.name, new()
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

    internal UniformInfo GetUniform(StringID name)
    {
        if (Disposed)
        {
            return null;
        }

        return uniforms.TryGetValue(name, out var u) ? u : null;
    }

    internal ShaderHandle GetUniformHandle(StringID name)
    {
        var uniform = GetUniform(name);

        if(uniform == null)
        {
            return default;
        }

        return new(this, uniform);
    }

    internal bool TryGetUniformData(StringID variantKey, ShaderHandle handle, out UniformInfo uniform,
        out (int, byte[])? vertexData, out (int, byte[])? fragmentData)
    {
        uniform = default;
        vertexData = default;
        fragmentData = default;

        if (Disposed ||
            handle.TryGetUniform(this, out uniform) == false ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false)
        {
            return false;
        }

        if(shaderInstance.vertexFields.TryGetValue(uniform.handle, out var field) &&
            shaderInstance.program.TryGetVertexUniformData(field, out var data))
        {
            vertexData = (field.offset, data);
        }
        else if (shaderInstance.vertexMappings.TryGetValue(uniform.handle, out var mapping) &&
            shaderInstance.program.TryGetVertexUniformData(mapping, out data))
        {
            vertexData = (0, data);
        }

        if (shaderInstance.fragmentFields.TryGetValue(uniform.handle, out field) &&
            shaderInstance.program.TryGetFragmentUniformData(field, out data))
        {
            fragmentData = (field.offset, data);
        }
        else if (shaderInstance.fragmentMappings.TryGetValue(uniform.handle, out var mapping) &&
            shaderInstance.program.TryGetFragmentUniformData(mapping, out data))
        {
            fragmentData = (0, data);
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

    private void SetValue<T>(StringID variantKey, ShaderHandle handle, T value) where T: unmanaged
    {
        if (TryGetUniformData(variantKey, handle, out _, out var vertexData, out var fragmentData) == false)
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

    private void SetValue<T>(StringID variantKey, ShaderHandle handle, ReadOnlySpan<T> value) where T: unmanaged
    {
        if (TryGetUniformData(variantKey, handle, out var uniform, out var vertexData, out var fragmentData) == false)
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

    /// <summary>
    /// Sets a float uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetFloat(StringID variantKey, ShaderHandle handle, float value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(StringID variantKey, ShaderHandle handle, Vector2 value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Vector2> value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector3(StringID variantKey, ShaderHandle handle, Vector3 value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector3(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Vector3> value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector4(StringID variantKey, ShaderHandle handle, Vector4 value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector4(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Vector4> value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetColor(StringID variantKey, ShaderHandle handle, Color value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetColor(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Color> value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Texture uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetTexture(StringID variantKey, ShaderHandle handle, Texture value)
    {
        if (Disposed ||
            value == null ||
            value.Disposed ||
            handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        //TODO
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(StringID variantKey, ShaderHandle handle, Matrix3x3 value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Matrix3x3> value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Matrix4x4 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(StringID variantKey, ShaderHandle handle, Matrix4x4 value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Matrix4x4 array uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Matrix4x4> value)
    {
        SetValue(variantKey, handle, value);
    }

    /// <summary>
    /// Destroys this resource
    /// </summary>
    public void Destroy()
    {
        if (Disposed)
        {
            return;
        }

        Disposed = true;

        foreach(var pair in instances)
        {
            pair.Value.program?.Destroy();

            pair.Value.program = null;
        }
    }

    /// <summary>
    /// Creates from shader data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="entries">The variant entries for the current renderer</param>
    /// <returns>The shader if valid</returns>
    internal static Shader Create(SerializableShader data, Dictionary<string, SerializableShaderData> entries)
    {
        var shader = new Shader(data, entries);

        if (shader.Create())
        {
            return shader;
        }

        return null;
    }
}
