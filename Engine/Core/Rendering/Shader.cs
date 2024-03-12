using Bgfx;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Staple.Internal;

/// <summary>
/// Shader resource
/// </summary>
internal partial class Shader : IGuidAsset
{
    internal class UniformInfo<T>
    {
        public ShaderUniform uniform;
        public bgfx.UniformHandle handle;
        public T value;
        public byte stage;
        public int count = 1;

        public bool Create()
        {
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

    internal bgfx.ShaderHandle vertexShader;
    internal bgfx.ShaderHandle fragmentShader;
    internal bgfx.ProgramHandle program;

    internal readonly ShaderMetadata metadata;
    internal readonly BlendMode sourceBlend = BlendMode.Off, destinationBlend = BlendMode.Off;
    internal readonly byte[] vertexShaderSource;
    internal readonly byte[] fragmentShaderSource;

    internal Dictionary<ShaderUniformType, object> uniforms = new();

    [GeneratedRegex("\\[([0-9]+)\\]")]
    private static partial Regex UniformCountRegex();

    private static Regex uniformCountRegex = UniformCountRegex();

    public string Guid { get; set; }

    /// <summary>
    /// Whether this shader has been disposed
    /// </summary>
    public bool Disposed { get; internal set; } = false;

    public static object Create(string path)
    {
        return ResourceManager.instance.LoadShader(path);
    }

    internal Shader(ShaderMetadata metadata, byte[] vertexShaderSource, byte[] fragmentShaderSource)
    {
        this.vertexShaderSource = vertexShaderSource;
        this.fragmentShaderSource = fragmentShaderSource;
        this.metadata = metadata;

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
        bgfx.Memory* vs, fs;

        fixed (void* ptr = vertexShaderSource)
        {
            vs = bgfx.copy(ptr, (uint)vertexShaderSource.Length);
        }

        fixed (void* ptr = fragmentShaderSource)
        {
            fs = bgfx.copy(ptr, (uint)fragmentShaderSource.Length);
        }

        vertexShader = bgfx.create_shader(vs);
        fragmentShader = bgfx.create_shader(fs);

        if (vertexShader.Valid == false || fragmentShader.Valid == false)
        {
            if(vertexShader.Valid)
            {
                bgfx.destroy_shader(vertexShader);
            }

            if(fragmentShader.Valid)
            {
                bgfx.destroy_shader(vertexShader);
            }

            return false;
        }

        program = bgfx.create_program(vertexShader, fragmentShader, true);

        if (program.Valid == false)
        {
            bgfx.destroy_shader(vertexShader);
            bgfx.destroy_shader(fragmentShader);

            return false;
        }

        if(uniforms.Count > 0)
        {
            void Apply<T>(object value, Action<UniformInfo<T>> callback)
            {
                if (value is Dictionary<int, UniformInfo<T>> container)
                {
                    foreach (var p in container)
                    {
                        var uniform = p.Value;

                        if (uniform.Create())
                        {
                            if (uniform.value != null)
                            {
                                callback?.Invoke(uniform);
                            }
                        }
                    }
                }
            }

            foreach (var pair in uniforms)
            {
                switch(pair.Key)
                {
                    case ShaderUniformType.Texture:

                        Apply<Texture>(pair.Value, (uniform) => SetTexture(uniform.uniform.name, uniform.value));

                        break;

                    case ShaderUniformType.Matrix3x3:

                        Apply<Matrix3x3>(pair.Value, (uniform) => SetMatrix3x3(uniform.uniform.name, uniform.value));

                        break;

                    case ShaderUniformType.Matrix4x4:

                        Apply<Matrix4x4>(pair.Value, (uniform) => SetMatrix4x4(uniform.uniform.name, uniform.value));

                        break;

                    case ShaderUniformType.Vector4:

                        Apply<Vector4>(pair.Value, (uniform) => SetVector4(uniform.uniform.name, uniform.value));

                        break;

                    case ShaderUniformType.Color:

                        Apply<Color>(pair.Value, (uniform) => SetColor(uniform.uniform.name, uniform.value));

                        break;

                    case ShaderUniformType.Vector3:

                        Apply<Vector3>(pair.Value, (uniform) => SetVector3(uniform.uniform.name, uniform.value));

                        break;

                    case ShaderUniformType.Vector2:

                        Apply<Vector2>(pair.Value, (uniform) => SetVector2(uniform.uniform.name, uniform.value));

                        break;

                    case ShaderUniformType.Float:

                        Apply<float>(pair.Value, (uniform) => SetFloat(uniform.uniform.name, uniform.value));

                        break;
                }
            }
        }
        else
        {
            foreach (var uniform in metadata.uniforms)
            {
                switch (uniform.type)
                {
                    case ShaderUniformType.Texture:

                        AddUniform<Texture>(uniform.name, uniform.type);

                        break;

                    case ShaderUniformType.Matrix3x3:

                        AddUniform<Matrix3x3>(uniform.name, uniform.type);

                        break;

                    case ShaderUniformType.Matrix4x4:

                        AddUniform<Matrix4x4>(uniform.name, uniform.type);

                        break;

                    case ShaderUniformType.Vector4:

                        AddUniform<Vector4>(uniform.name, uniform.type);

                        break;

                    case ShaderUniformType.Color:

                        AddUniform<Color>(uniform.name, uniform.type);

                        break;

                    case ShaderUniformType.Vector3:

                        AddUniform<Vector3>(uniform.name, uniform.type);

                        break;

                    case ShaderUniformType.Vector2:

                        AddUniform<Vector2>(uniform.name, uniform.type);

                        break;

                    case ShaderUniformType.Float:

                        AddUniform<float>(uniform.name, uniform.type);

                        break;
                }
            }

            void EnsureUniform<T>(string name, ShaderUniformType type)
            {
                var uniform = GetUniform<T>(name, type);

                if (uniform == null)
                {
                    AddUniform<T>(name, type);
                }
            }

            EnsureUniform<float>("u_isSkinning", ShaderUniformType.Float);
            EnsureUniform<Matrix4x4>("u_boneMatrices[128]", ShaderUniformType.Matrix4x4);
        }

        Disposed = false;

        return true;
    }

    internal void AddUniform<T>(string name, ShaderUniformType type)
    {
        if (uniforms.TryGetValue(type, out var container) == false)
        {
            container = new Dictionary<int, UniformInfo<T>>();

            uniforms.Add(type, container);
        }

        if (container is Dictionary<int, UniformInfo<T>> c)
        {
            var u = new UniformInfo<T>()
            {
                uniform = new()
                {
                    name = NormalizeUniformName(name, type),
                    type = type,
                },
                count = NormalizeUniformCount(name),
            };

            if (u.Create())
            {
                if (type == ShaderUniformType.Texture)
                {
                    u.stage = (byte)c.Count;
                }

                c.Add(u.uniform.name.GetHashCode(), u);
            }
        }
    }

    internal bgfx.StateFlags BlendingFlag()
    {
        if(sourceBlend != BlendMode.Off && destinationBlend != BlendMode.Off)
        {
            return (bgfx.StateFlags)RenderSystem.BlendFunction((bgfx.StateFlags)sourceBlend, (bgfx.StateFlags)destinationBlend);
        }

        return 0;
    }

    internal UniformInfo<T> GetUniform<T>(string name, ShaderUniformType type)
    {
        if (Disposed)
        {
            return null;
        }

        name = NormalizeUniformName(name, type);

        return uniforms.TryGetValue(type, out var container) &&
            container is Dictionary<int, UniformInfo<T>> c &&
            c.TryGetValue(name.GetHashCode(), out var outValue) ? outValue : null;
    }

    internal UniformInfo<T> GetUniform<T>(int hashCode, ShaderUniformType type)
    {
        if (Disposed)
        {
            return null;
        }

        return uniforms.TryGetValue(type, out var container) &&
            container is Dictionary<int, UniformInfo<T>> c &&
            c.TryGetValue(hashCode, out var outValue) ? outValue : null;
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="name">The uniform's name</param>
    /// <param name="value">The value</param>
    public void SetFloat(string name, float value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = GetUniform<float>(name, ShaderUniformType.Float);

        if (uniform == null)
        {
            return;
        }

        uniform.value = value;

        unsafe
        {
            var temp = new Vector4(value, 0, 0, 0);

            bgfx.set_uniform(uniform.handle, &temp, 1);
        }
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="name">The uniform's name</param>
    /// <param name="value">The value</param>
    public void SetVector2(string name, Vector2 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = GetUniform<Vector2>(name, ShaderUniformType.Vector2);

        if (uniform == null)
        {
            return;
        }

        uniform.value = value;

        unsafe
        {
            var temp = new Vector4(value, 0, 0);

            bgfx.set_uniform(uniform.handle, &temp, 1);
        }
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="name">The uniform's name</param>
    /// <param name="value">The value</param>
    public void SetVector3(string name, Vector3 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = GetUniform<Vector3>(name, ShaderUniformType.Vector3);

        if (uniform == null)
        {
            return;
        }

        uniform.value = value;

        unsafe
        {
            var temp = new Vector4(value, 0);

            bgfx.set_uniform(uniform.handle, &temp, 1);
        }
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="name">The uniform's name</param>
    /// <param name="value">The value</param>
    public void SetVector4(string name, Vector4 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = GetUniform<Vector4>(name, ShaderUniformType.Vector4);

        if(uniform == null)
        {
            return;
        }

        uniform.value = value;

        unsafe
        {
            bgfx.set_uniform(uniform.handle, &value, 1);
        }
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="name">The uniform's name</param>
    /// <param name="value">The value</param>
    public void SetColor(string name, Color value)
    {
        if(Disposed)
        {
            return;
        }


        var uniform = GetUniform<Color>(name, ShaderUniformType.Color);

        if (uniform == null)
        {
            return;
        }

        var colorValue = new Vector4(value.r, value.g, value.b, value.a);

        uniform.value = value;

        unsafe
        {
            bgfx.set_uniform(uniform.handle, &colorValue, 1);
        }
    }

    /// <summary>
    /// Sets a Texture uniform's value
    /// </summary>
    /// <param name="name">The uniform's name</param>
    /// <param name="value">The value</param>
    /// <param name="overrideFlags">Flags to override texture state</param>
    public void SetTexture(string name, Texture value, TextureFlags overrideFlags = (TextureFlags)uint.MaxValue)
    {
        if (Disposed || value == null || value.Disposed)
        {
            return;
        }

        var uniform = GetUniform<Texture>(name, ShaderUniformType.Texture);

        if (uniform == null)
        {
            return;
        }

        uniform.value = value;

        unsafe
        {
            value.SetActive(uniform.stage, uniform.handle, overrideFlags);
        }
    }

    /// <summary>
    /// Sets a Matrix4x4 uniform's value
    /// </summary>
    /// <param name="name">The uniform's name</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(string name, Matrix4x4 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = GetUniform<Matrix4x4>(name, ShaderUniformType.Matrix4x4);

        if (uniform == null)
        {
            return;
        }

        uniform.value = value;

        unsafe
        {
            bgfx.set_uniform(uniform.handle, &value, 1);
        }
    }

    /// <summary>
    /// Sets a Matrix4x4 array uniform's value
    /// </summary>
    /// <param name="name">The uniform's name</param>
    /// <param name="value">The value</param>
    /// <param name="count">The amount of elements</param>
    public void SetMatrix4x4(string name, Matrix4x4[] value, int count)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = GetUniform<Matrix4x4>(name, ShaderUniformType.Matrix4x4);

        if (uniform == null)
        {
            return;
        }

        unsafe
        {
            fixed(void *ptr = value)
            {
                bgfx.set_uniform(uniform.handle, ptr, (ushort)count);
            }
        }
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="name">The uniform's name</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(string name, Matrix3x3 value)
    {
        if (Disposed)
        {
            return;
        }

        var uniform = GetUniform<Matrix3x3>(name, ShaderUniformType.Matrix3x3);

        if (uniform == null)
        {
            return;
        }

        uniform.value = value;

        unsafe
        {
            bgfx.set_uniform(uniform.handle, &value, 1);
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

        if(program.Valid)
        {
            bgfx.destroy_program(program);
        }

        static void DestroyUniforms<T>(object value)
        {
            if (value is Dictionary<int, UniformInfo<T>> container)
            {
                foreach (var p in container)
                {
                    var uniform = p.Value;

                    if (uniform.handle.Valid)
                    {
                        bgfx.destroy_uniform(uniform.handle);
                    }
                }
            }
        }

        foreach (var pair in uniforms)
        {
            switch(pair.Key)
            {
                case ShaderUniformType.Texture:

                    DestroyUniforms<Texture>(pair.Value);

                    break;

                case ShaderUniformType.Vector4:

                    DestroyUniforms<Vector4>(pair.Value);

                    break;

                case ShaderUniformType.Matrix3x3:

                    DestroyUniforms<Matrix3x3>(pair.Value);

                    break;

                case ShaderUniformType.Matrix4x4:

                    DestroyUniforms<Matrix4x4>(pair.Value);

                    break;

                case ShaderUniformType.Color:

                    DestroyUniforms<Color>(pair.Value);

                    break;
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
        unsafe
        {
            switch(data.metadata.type)
            {
                case ShaderType.Compute:

                    //TODO
                    return null;

                case ShaderType.VertexFragment:

                    {
                        var shader = new Shader(data.metadata, data.vertexShader, data.fragmentShader);

                        if(shader.Create())
                        {
                            return shader;
                        }

                        return null;
                    }

                default:

                    return null;
            }
        }
    }
}
