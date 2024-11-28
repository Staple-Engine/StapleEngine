using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Staple.Internal;

/// <summary>
/// Shader resource
/// </summary>
internal partial class Shader : IGuidAsset
{
    public static readonly string SkinningKeyword = "SKINNING";
    public static readonly string LitKeyword = "LIT";
    public static readonly string HalfLambertKeyword = "HALF_LAMBERT";

    public static readonly string[] DefaultVariants =
    [
        SkinningKeyword,
    ];

    internal class UniformInfo
    {
        public ShaderUniform uniform;
        public bgfx.UniformHandle handle;
        public byte stage;
        public int count = 1;
        public bool isAlias = false;

        public bool Create()
        {
            if(isAlias)
            {
                return true;
            }

            bgfx.UniformType type;

            switch (uniform.type)
            {
                case ShaderUniformType.Float:
                case ShaderUniformType.Vector2:
                case ShaderUniformType.Vector3:
                case ShaderUniformType.Vector4:

                    type = bgfx.UniformType.Vec4;

                    break;

                case ShaderUniformType.Color:

                    type = bgfx.UniformType.Vec4;

                    break;

                case ShaderUniformType.Matrix4x4:

                    type = bgfx.UniformType.Mat4;

                    break;

                case ShaderUniformType.Matrix3x3:

                    type = bgfx.UniformType.Mat3;

                    break;

                case ShaderUniformType.Texture:

                    type = bgfx.UniformType.Sampler;

                    break;

                default:

                    return false;
            }

            handle = bgfx.create_uniform(uniform.name, type, (ushort)count);

            return handle.Valid;
        }
    }

    internal class ShaderInstance
    {
        public bgfx.ProgramHandle program;
        public int[] keyPieces;

        public byte[] vertexShaderSource;
        public byte[] fragmentShaderSource;
    }

    internal readonly ShaderMetadata metadata;
    internal readonly BlendMode sourceBlend = BlendMode.Off, destinationBlend = BlendMode.Off;

    internal static readonly List<(string, ShaderUniformType)> DefaultUniforms = [];

    internal readonly Dictionary<string, ShaderInstance> instances = [];

    private UniformInfo[] uniforms = [];
    private readonly IntLookupCache<int> uniformIndices = new();

    private int usedTextureStages = 0;

    [GeneratedRegex("\\[([0-9]+)\\]")]
    private static partial Regex UniformCountRegex();

    private static readonly Regex uniformCountRegex = UniformCountRegex();

    public string Guid { get; set; }

    /// <summary>
    /// Whether this shader has been disposed
    /// </summary>
    public bool Disposed { get; internal set; } = false;

    public static object Create(string path)
    {
        return ResourceManager.instance.LoadShader(path);
    }

    internal Shader(SerializableShader shader)
    {
        metadata = shader.metadata;

        foreach(var pair in shader.data)
        {
            instances.AddOrSetKey(pair.Key, new()
            {
                keyPieces = pair.Key.Split(' ').Select(x => x.GetHashCode()).ToArray(),
                fragmentShaderSource = pair.Value.fragmentShader,
                vertexShaderSource = pair.Value.vertexShader,
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
            ShaderUniformType.Float or ShaderUniformType.Vector2 or ShaderUniformType.Vector3 => $"{name}_uniform",
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
            bgfx.Memory* vs, fs;

            fixed (void* ptr = pair.Value.vertexShaderSource)
            {
                vs = bgfx.copy(ptr, (uint)pair.Value.vertexShaderSource.Length);
            }

            fixed (void* ptr = pair.Value.fragmentShaderSource)
            {
                fs = bgfx.copy(ptr, (uint)pair.Value.fragmentShaderSource.Length);
            }

            var vertexShader = bgfx.create_shader(vs);
            var fragmentShader = bgfx.create_shader(fs);

            if (vertexShader.Valid == false || fragmentShader.Valid == false)
            {
                if (vertexShader.Valid)
                {
                    bgfx.destroy_shader(vertexShader);
                }

                if (fragmentShader.Valid)
                {
                    bgfx.destroy_shader(fragmentShader);
                }

                return false;
            }

            pair.Value.program = bgfx.create_program(vertexShader, fragmentShader, true);

            if (pair.Value.program.Valid == false)
            {
                bgfx.destroy_shader(vertexShader);
                bgfx.destroy_shader(fragmentShader);

                return false;
            }
        }

        if (uniforms.Length > 0)
        {
            foreach (var uniform in uniforms)
            {
                uniform.Create();
            }
        }
        else
        {
            foreach (var uniform in metadata.uniforms)
            {
                AddUniform(uniform.name, uniform.type);
            }

            void EnsureUniform(string name, ShaderUniformType type)
            {
                var uniform = GetUniform(name.GetHashCode());

                if (uniform == null)
                {
                    AddUniform(name, type);
                }
            }

            foreach(var uniform in DefaultUniforms)
            {
                EnsureUniform(uniform.Item1, uniform.Item2);
            }
        }

        Disposed = false;

        return true;
    }

    internal void AddUniform(string name, ShaderUniformType type)
    {
        var normalizedName = NormalizeUniformName(name, type);
        var nameHash = name.GetHashCode();
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
                type = type,
            },
            count = NormalizeUniformCount(name),
        };

        if (u.Create())
        {
            if (type == ShaderUniformType.Texture)
            {
                u.stage = (byte)usedTextureStages;

                usedTextureStages++;
            }

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
                    handle = u.handle,
                    stage = u.stage,
                    uniform = new()
                    {
                        name = u.uniform.name,
                        type = type,
                    },
                }]).ToArray();
            }
        }
    }

    internal bgfx.StateFlags BlendingFlag
    {
        get
        {
            if (sourceBlend != BlendMode.Off && destinationBlend != BlendMode.Off)
            {
                return (bgfx.StateFlags)RenderSystem.BlendFunction((bgfx.StateFlags)sourceBlend, (bgfx.StateFlags)destinationBlend);
            }

            return 0;
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

        return new(uniform);
    }

    /// <summary>
    /// Sets a float uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetFloat(ShaderHandle handle, float value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            var temp = new Vector4(value, 0, 0, 0);

            bgfx.set_uniform(uniform.handle, &temp, 1);
        }
    }

    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(ShaderHandle handle, Vector2 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            var temp = new Vector4(value, 0, 0);

            bgfx.set_uniform(uniform.handle, &temp, 1);
        }
    }

    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(ShaderHandle handle, ReadOnlySpan<Vector2> value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            var temp = new Vector4[value.Length];

            for (var i = 0; i < value.Length; i++)
            {
                temp[i] = value[i].ToVector4();
            }

            fixed (void* ptr = temp)
            {
                bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
            }
        }
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector3(ShaderHandle handle, Vector3 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            var temp = new Vector4(value, 0);

            bgfx.set_uniform(uniform.handle, &temp, 1);
        }
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector3(ShaderHandle handle, ReadOnlySpan<Vector3> value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            var temp = new Vector4[value.Length];

            for (var i = 0; i < value.Length; i++)
            {
                temp[i] = value[i].ToVector4();
            }

            fixed (void* ptr = temp)
            {
                bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
            }
        }
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector4(ShaderHandle handle, Vector4 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            bgfx.set_uniform(uniform.handle, &value, 1);
        }
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector4(ShaderHandle handle, ReadOnlySpan<Vector4> value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
            }
        }
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetColor(ShaderHandle handle, Color value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        var colorValue = new Vector4(value.r, value.g, value.b, value.a);

        unsafe
        {
            bgfx.set_uniform(uniform.handle, &colorValue, 1);
        }
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetColor(ShaderHandle handle, ReadOnlySpan<Color> value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
            }
        }
    }

    /// <summary>
    /// Sets a Texture uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    /// <param name="overrideFlags">Flags to override texture state</param>
    public void SetTexture(ShaderHandle handle, Texture value, TextureFlags overrideFlags = (TextureFlags)uint.MaxValue)
    {
        if (Disposed || value == null || value.Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            value.SetActive(uniform.stage, uniform.handle, overrideFlags);
        }
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(ShaderHandle handle, Matrix3x3 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            bgfx.set_uniform(uniform.handle, &value, 1);
        }
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(ShaderHandle handle, ReadOnlySpan<Matrix3x3> value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
            }
        }
    }

    /// <summary>
    /// Sets a Matrix4x4 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(ShaderHandle handle, Matrix4x4 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            bgfx.set_uniform(uniform.handle, &value, 1);
        }
    }

    /// <summary>
    /// Sets a Matrix4x4 array uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(ShaderHandle handle, ReadOnlySpan<Matrix4x4> value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = handle.uniform;

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
            }
        }
    }

    /// <summary>
    /// Destroys this resource
    /// </summary>
    internal void Destroy()
    {
        if (Disposed)
        {
            return;
        }

        Disposed = true;

        foreach(var pair in instances)
        {
            if (pair.Value.program.Valid)
            {
                bgfx.destroy_program(pair.Value.program);

                pair.Value.program = new()
                {
                    idx = ushort.MaxValue,
                };
            }
        }

        foreach (var uniform in uniforms)
        {
            if(uniform.isAlias)
            {
                continue;
            }

            if(uniform.handle.Valid)
            {
                bgfx.destroy_uniform(uniform.handle);
            }
        }
    }

    /// <summary>
    /// Creates from shader data
    /// </summary>
    /// <param name="data">The data</param>
    /// <returns>The shader if valid</returns>
    internal static Shader Create(SerializableShader data)
    {
        var shader = new Shader(data);

        if (shader.Create())
        {
            return shader;
        }

        return null;
    }
}
