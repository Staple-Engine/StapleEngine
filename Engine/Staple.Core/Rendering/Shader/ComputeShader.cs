using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Compute shader resource
/// </summary>
public partial class ComputeShader : IGuidAsset
{
    internal static readonly List<ShaderResource.DefaultUniform> DefaultUniforms = [];

    internal ComputeShaderResource shaderResource;

    public GuidHasher Guid { get; private set; }

    /// <summary>
    /// Whether this shader has been disposed
    /// </summary>
    public bool Disposed => shaderResource is not null;

    public static object Create(string path)
    {
        return ResourceManager.instance.LoadComputeShader(path);
    }

    internal ComputeShader(ComputeShaderResource resource)
    {
        shaderResource = resource;
        Guid = resource.Guid;
    }

    ~ComputeShader()
    {
        Destroy();
    }

    internal ShaderResource.UniformInfo GetUniform(int hash)
    {
        if (Disposed)
        {
            return null;
        }

        return shaderResource.uniformIndices.TryGetValue(hash, out var index) ? shaderResource.uniforms[index] : null;
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
            !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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
        if (Disposed || !handle.TryGetUniform(this, out var uniform))
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

        shaderResource.Destroy();

        shaderResource = null;

        Guid = new();
    }

    /// <summary>
    /// Creates from shader data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="entries">The variant entries for the current renderer</param>
    /// <returns>The shader if valid</returns>
    internal static ComputeShader Create(SerializableShader data, Dictionary<string, SerializableShaderData> entries)
    {
        var resource = new ComputeShaderResource(data, entries);

        if (resource.Create())
        {
            return new(resource);
        }

        return null;
    }
}
