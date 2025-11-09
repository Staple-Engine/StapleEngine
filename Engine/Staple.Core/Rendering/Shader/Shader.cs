using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        public ShaderUniformContainer uniforms;
        public Dictionary<string, ShaderUniformField> fields = [];
    }

    internal readonly ShaderMetadata metadata;
    internal readonly BlendMode sourceBlend = BlendMode.Off, destinationBlend = BlendMode.Off;

    internal static readonly List<DefaultUniform> DefaultUniforms = [];

    internal readonly Dictionary<string, ShaderInstance> instances = [];

    private UniformInfo[] uniforms = [];
    private readonly IntLookupCache<int> uniformIndices = new();

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
            var fields = new Dictionary<string, ShaderUniformField>();

            foreach(var uniform in pair.Value.uniforms.uniforms)
            {
                foreach(var field in uniform.fields)
                {
                    fields.AddOrSetKey(field.name, field);
                }
            }

            instances.AddOrSetKey(pair.Key, new()
            {
                keyPieces = pair.Key.Split(' ').Select(x => x.GetHashCode()).ToArray(),
                vertexShaderSource = pair.Value.vertexShader,
                fragmentShaderSource = pair.Value.fragmentShader,
                computeShaderSource = pair.Value.computeShader,
                vertexShaderMetrics = pair.Value.vertexMetrics,
                fragmentShaderMetrics = pair.Value.fragmentMetrics,
                computeShaderMetrics = pair.Value.computeMetrics,
                attributes = pair.Value.vertexAttributes,
                uniforms = pair.Value.uniforms,
                fields = fields,
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

        return type switch
        {
            ShaderUniformType.Int or ShaderUniformType.Float or ShaderUniformType.Vector2 or ShaderUniformType.Vector3 => $"{name}_uniform",
            _ => name
        };
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
                pair.Value.vertexShaderMetrics, pair.Value.fragmentShaderMetrics, pair.Value.attributes,
                pair.Value.uniforms);

            if(pair.Value.program == null)
            {
                return false;
            }
        }

        if (uniforms.Length == 0)
        {
            foreach (var uniform in metadata.uniforms)
            {
                AddUniform(DefaultUniform.FromShaderUniform(uniform));
            }

            void EnsureUniform(DefaultUniform u)
            {
                var uniform = GetUniform(u.name.GetHashCode());

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
        var nameHash = uniform.name.GetHashCode();
        var normalizedHash = normalizedName.GetHashCode();

        var uniformIndex = uniformIndices.IndexOf(nameHash);

        if(uniformIndex >= 0)
        {
            return;
        }

        uniformIndex = uniformIndices.IndexOf(normalizedHash);

        if (uniformIndex >= 0)
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
            count = NormalizeUniformCount(uniform.name),
        };

        var i = uniforms.Length;

        uniformIndices.Add(normalizedHash, i);
        uniforms = uniforms.Concat([u]).ToArray();

        if (uniformIndices.IndexOf(nameHash) < 0)
        {
            uniformIndices.Add(nameHash, i);

            uniforms = uniforms.Concat([new()
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
            }]).ToArray();
        }
    }

    internal UniformInfo GetUniform(int hash)
    {
        if (Disposed)
        {
            return null;
        }

        return uniformIndices.TryGetValue(hash, out var index) ? uniforms[index] : null;
    }

    internal ShaderHandle GetUniformHandle(int hash)
    {
        var uniform = GetUniform(hash);

        if(uniform == null)
        {
            return default;
        }

        return new(this, uniform);
    }

    /// <summary>
    /// Sets a float uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetFloat(string variantKey, ShaderHandle handle, float value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            var target = new Span<byte>(data, field.offset, sizeof(float));
            var source = new Span<byte>(&value, sizeof(float));

            source.CopyTo(target);
        }
    }

    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(string variantKey, ShaderHandle handle, Vector2 value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Vector2>());
            var source = new Span<byte>(&value, Marshal.SizeOf<Vector2>());

            source.CopyTo(target);
        }
    }

    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(string variantKey, ShaderHandle handle, ReadOnlySpan<Vector2> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            uniform.count != value.Length ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            fixed(void *ptr = value)
            {
                var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Vector2>() * uniform.count);
                var source = new Span<byte>(ptr, Marshal.SizeOf<Vector2>() * uniform.count);

                source.CopyTo(target);
            }
        }
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector3(string variantKey, ShaderHandle handle, Vector3 value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Vector3>());
            var source = new Span<byte>(&value, Marshal.SizeOf<Vector3>());

            source.CopyTo(target);
        }
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector3(string variantKey, ShaderHandle handle, ReadOnlySpan<Vector3> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            uniform.count != value.Length ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Vector3>() * uniform.count);
                var source = new Span<byte>(ptr, Marshal.SizeOf<Vector3>() * uniform.count);

                source.CopyTo(target);
            }
        }
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector4(string variantKey, ShaderHandle handle, Vector4 value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Vector4>());
            var source = new Span<byte>(&value, Marshal.SizeOf<Vector4>());

            source.CopyTo(target);
        }
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector4(string variantKey, ShaderHandle handle, ReadOnlySpan<Vector4> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            uniform.count != value.Length ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Vector4>() * uniform.count);
                var source = new Span<byte>(ptr, Marshal.SizeOf<Vector4>() * uniform.count);

                source.CopyTo(target);
            }
        }
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetColor(string variantKey, ShaderHandle handle, Color value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Color>());
            var source = new Span<byte>(&value, Marshal.SizeOf<Color>());

            source.CopyTo(target);
        }
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetColor(string variantKey, ShaderHandle handle, ReadOnlySpan<Color> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            uniform.count != value.Length ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Color>() * uniform.count);
                var source = new Span<byte>(ptr, Marshal.SizeOf<Color>() * uniform.count);

                source.CopyTo(target);
            }
        }
    }

    /// <summary>
    /// Sets a Texture uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetTexture(string variantKey, ShaderHandle handle, Texture value)
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
    public void SetMatrix3x3(string variantKey, ShaderHandle handle, Matrix3x3 value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Matrix3x3>());
            var source = new Span<byte>(&value, Marshal.SizeOf<Matrix3x3>());

            source.CopyTo(target);
        }
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(string variantKey, ShaderHandle handle, ReadOnlySpan<Matrix3x3> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            uniform.count != value.Length ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Matrix3x3>() * uniform.count);
                var source = new Span<byte>(ptr, Marshal.SizeOf<Matrix3x3>() * uniform.count);

                source.CopyTo(target);
            }
        }
    }

    /// <summary>
    /// Sets a Matrix4x4 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(string variantKey, ShaderHandle handle, Matrix4x4 value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Matrix4x4>());
            var source = new Span<byte>(&value, Marshal.SizeOf<Matrix4x4>());

            source.CopyTo(target);
        }
    }

    /// <summary>
    /// Sets a Matrix4x4 array uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(string variantKey, ShaderHandle handle, ReadOnlySpan<Matrix4x4> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false ||
            uniform.count != value.Length ||
            instances.TryGetValue(variantKey, out var shaderInstance) == false ||
            shaderInstance.fields.TryGetValue(uniform.uniform.name, out var field) == false ||
            shaderInstance.program.TryGetUniformData((byte)field.binding, out var data) == false)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                var target = new Span<byte>(data, field.offset, Marshal.SizeOf<Matrix4x4>() * uniform.count);
                var source = new Span<byte>(ptr, Marshal.SizeOf<Matrix4x4>() * uniform.count);

                source.CopyTo(target);
            }
        }
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
