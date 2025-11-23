using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Staple.Internal;

/// <summary>
/// Compute shader resource
/// </summary>
public partial class ComputeShader : IGuidAsset
{
    internal readonly ShaderMetadata metadata;

    internal static readonly List<Shader.DefaultUniform> DefaultUniforms = [];

    private Shader.UniformInfo[] uniforms = [];
    private readonly IntLookupCache<int> uniformIndices = new();

    private readonly byte[] shaderSource = [];

    private readonly ComputeShaderMetrics metrics;

    private readonly ShaderUniformContainer uniformContainer;

    private readonly Dictionary<string, ShaderUniformField> fields = [];

    private IShaderProgram program;

    [GeneratedRegex("\\[([0-9]+)\\]")]
    private static partial Regex UniformCountRegex();

    private static readonly Regex uniformCountRegex = UniformCountRegex();

    private readonly GuidHasher guidHasher = new();

    public GuidHasher Guid => guidHasher;

    /// <summary>
    /// Whether this shader has been disposed
    /// </summary>
    public bool Disposed { get; internal set; } = false;

    public static object Create(string path)
    {
        return ResourceManager.instance.LoadComputeShader(path);
    }

    internal ComputeShader(SerializableShader shader, Dictionary<string, SerializableShaderData> entries)
    {
        metadata = shader.metadata;

        var entry = entries.FirstOrDefault().Value;

        shaderSource = entry.computeShader ?? [];
        metrics = entry.computeMetrics ?? new();
        uniformContainer = entry.computeUniforms;

        foreach (var uniform in entry.computeUniforms.uniforms)
        {
            if((uniform.fields?.Count ?? 0) == 0)
            {
                //TODO: Actual uniforms
            }
            else
            {
                foreach (var field in uniform.fields)
                {
                    fields.AddOrSetKey(field.name, field);
                }
            }
        }
    }

    ~ComputeShader()
    {
        Destroy();
    }

    private static string NormalizeUniformName(string name, ShaderUniformType type)
    {
        if (uniformCountRegex.IsMatch(name))
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
        if (uniformCountRegex.IsMatch(name) == false)
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

    internal unsafe bool Create()
    {
        if((shaderSource?.Length ?? 0) == 0)
        {
            return false;
        }

        program = RenderSystem.Backend.CreateShaderCompute(shaderSource, metrics, uniformContainer);

        if (program == null)
        {
            return false;
        }

        /*
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
                AddUniform(Shader.DefaultUniform.FromShaderUniform(uniform));
            }

            void EnsureUniform(Shader.DefaultUniform u)
            {
                var uniform = GetUniform(u.name.GetHashCode());

                if (uniform == null)
                {
                    AddUniform(u);
                }
            }

            foreach (var uniform in DefaultUniforms)
            {
                EnsureUniform(uniform);
            }
        }

        Disposed = false;

        return true;
        */

        return false;
    }

    internal void AddUniform(Shader.DefaultUniform uniform)
    {
        var normalizedName = NormalizeUniformName(uniform.name, uniform.type);
        var nameHash = uniform.name.GetHashCode();
        var normalizedHash = normalizedName.GetHashCode();

        var uniformIndex = uniformIndices.IndexOf(nameHash);

        if (uniformIndex >= 0)
        {
            return;
        }

        uniformIndex = uniformIndices.IndexOf(normalizedHash);

        if (uniformIndex >= 0)
        {
            return;
        }

        var u = new Shader.UniformInfo()
        {
            uniform = new()
            {
                name = normalizedName,
                type = uniform.type,
                attribute = uniform.attribute,
                variant = uniform.variant,
                defaultValue = uniform.defaultValue,
            },
            handle = new(normalizedName),
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
                handle = new(normalizedName),
            }]).ToArray();
        }
    }

    internal Shader.UniformInfo GetUniform(int hash)
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

        if (uniform == null)
        {
            return default;
        }

        return new(this, uniform);
    }

    /// <summary>
    /// Dispatches this shader
    /// </summary>
    /// <param name="viewId">The view ID to dispatch at</param>
    /// <param name="x">The amount of X threads</param>
    /// <param name="y">The amount of Y threads</param>
    /// <param name="z">The amount of Z threads</param>
    public void Dispatch(ushort viewId, int x, int y, int z)
    {
        /*
        if (Disposed ||
            metadata.type != ShaderType.Compute ||
            programHandle.Valid == false)
        {
            return;
        }

        bgfx.dispatch(viewId, programHandle, (uint)x, (uint)y, (uint)z, (byte)bgfx.DiscardFlags.All);
        */
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
    public void SetTexture(ShaderHandle handle, Texture value)
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

        /*
        if(programHandle.Valid)
        {
            bgfx.destroy_program(programHandle);

            programHandle = new()
            {
                idx = ushort.MaxValue,
            };
        }
        */

        /*
        foreach (var uniform in uniforms)
        {
            if (uniform.isAlias)
            {
                uniform.handle.idx = ushort.MaxValue;

                continue;
            }

            if (uniform.handle.Valid)
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
    internal static ComputeShader Create(SerializableShader data, Dictionary<string, SerializableShaderData> entries)
    {
        var shader = new ComputeShader(data, entries);

        if (shader.Create())
        {
            return shader;
        }

        return null;
    }
}
