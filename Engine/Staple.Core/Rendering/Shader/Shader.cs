using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        public static DefaultUniform FromShaderUniform(ShaderUniform uniform)
        {
            return new()
            {
                name = uniform.name,
                type = uniform.type,
                attribute = uniform.attribute,
                variant = uniform.variant,
                defaultValue = uniform.defaultValue
            };
        }
    }

    public class UniformInfo
    {
        internal ShaderUniform uniform;
        //internal bgfx.UniformHandle handle;
        internal byte stage;
        internal int count = 1;
        internal bool isAlias = false;

        internal bool Create()
        {
            if(isAlias)
            {
                return true;
            }

            /*
            bgfx.UniformType type;

            switch (uniform.type)
            {
                case ShaderUniformType.Int:
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
            */

            return false;
        }

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
    }

    internal readonly ShaderMetadata metadata;
    internal readonly BlendMode sourceBlend = BlendMode.Off, destinationBlend = BlendMode.Off;

    internal static readonly List<DefaultUniform> DefaultUniforms = [];

    internal readonly Dictionary<string, ShaderInstance> instances = [];

    private UniformInfo[] uniforms = [];
    private readonly IntLookupCache<int> uniformIndices = new();

    private int usedTextureStages = 0;

    [GeneratedRegex("\\[([0-9]+)\\]")]
    private static partial Regex UniformCountRegex();

    private static readonly Regex uniformCountRegex = UniformCountRegex();

    private readonly GuidHasher guidHasher = new();

    public GuidHasher Guid => guidHasher;

    /*
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
    */

    internal bool IsTransparent
    {
        get
        {
            return ((sourceBlend == BlendMode.Off && destinationBlend == BlendMode.Off) ||
                (sourceBlend == BlendMode.One && destinationBlend == BlendMode.Zero) ||
                (sourceBlend == BlendMode.Zero && destinationBlend == BlendMode.One)) == false;
        }
    }

    /*
    internal bgfx.StateFlags StateFlags
    {
        get
        {
            var baseFlags = bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA | bgfx.StateFlags.WriteZ | bgfx.StateFlags.DepthTestLequal;

            return baseFlags | BlendingFlag;
        }
    }
    */

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
            instances.AddOrSetKey(pair.Key, new()
            {
                keyPieces = pair.Key.Split(' ').Select(x => x.GetHashCode()).ToArray(),
                vertexShaderSource = pair.Value.vertexShader,
                fragmentShaderSource = pair.Value.fragmentShader,
                computeShaderSource = pair.Value.computeShader,
                vertexShaderMetrics = pair.Value.vertexMetrics,
                fragmentShaderMetrics = pair.Value.fragmentMetrics,
                computeShaderMetrics = pair.Value.computeMetrics,
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
                pair.Value.vertexShaderMetrics, pair.Value.fragmentShaderMetrics);

            if(pair.Value.program == null)
            {
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
            },
            count = NormalizeUniformCount(uniform.name),
        };

        if (u.Create())
        {
            if (uniform.type == ShaderUniformType.Texture)
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
                    //handle = u.handle,
                    stage = u.stage,
                    uniform = new()
                    {
                        name = u.uniform.name,
                        type = uniform.type,
                        attribute = uniform.attribute,
                        variant = uniform.variant,
                        defaultValue = uniform.defaultValue,
                    },
                }]).ToArray();
            }
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
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetFloat(ShaderHandle handle, float value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            var temp = new Vector4(value, 0, 0, 0);

            //bgfx.set_uniform(uniform.handle, &temp, 1);
        }
    }

    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(ShaderHandle handle, Vector2 value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            var temp = new Vector4(value, 0, 0);

            //bgfx.set_uniform(uniform.handle, &temp, 1);
        }
    }

    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(ShaderHandle handle, ReadOnlySpan<Vector2> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
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
                //bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
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
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            var temp = new Vector4(value, 0);

            //bgfx.set_uniform(uniform.handle, &temp, 1);
        }
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector3(ShaderHandle handle, ReadOnlySpan<Vector3> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
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
                //bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
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
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            //bgfx.set_uniform(uniform.handle, &value, 1);
        }
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector4(ShaderHandle handle, ReadOnlySpan<Vector4> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                //bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
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
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        var colorValue = new Vector4(value.r, value.g, value.b, value.a);

        unsafe
        {
            //bgfx.set_uniform(uniform.handle, &colorValue, 1);
        }
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetColor(ShaderHandle handle, ReadOnlySpan<Color> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                //bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
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
        if (Disposed ||
            value == null ||
            value.Disposed ||
            handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            //value.SetActive(uniform.stage, uniform.handle, overrideFlags);
        }
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(ShaderHandle handle, Matrix3x3 value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            //bgfx.set_uniform(uniform.handle, &value, 1);
        }
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(ShaderHandle handle, ReadOnlySpan<Matrix3x3> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                //bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
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
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            //bgfx.set_uniform(uniform.handle, &value, 1);
        }
    }

    /// <summary>
    /// Sets a Matrix4x4 array uniform's value
    /// </summary>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(ShaderHandle handle, ReadOnlySpan<Matrix4x4> value)
    {
        if (Disposed || handle.TryGetUniform(this, out var uniform) == false)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = value)
            {
                //bgfx.set_uniform(uniform.handle, ptr, (ushort)value.Length);
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

        /*
        foreach (var uniform in uniforms)
        {
            if (uniform.isAlias)
            {
                uniform.handle.idx = ushort.MaxValue;

                continue;
            }

            if(uniform.handle.Valid)
            {
                bgfx.destroy_uniform(uniform.handle);

                uniform.handle.idx = ushort.MaxValue;
            }
        }
        */
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
