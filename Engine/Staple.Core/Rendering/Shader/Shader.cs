using System;
using System.Collections.Generic;
using System.Numerics;

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

    public GuidHasher Guid { get; private set; }

    /// <summary>
    /// Whether this shader has been disposed
    /// </summary>
    public bool Disposed => shaderResource is not null;

    internal ShaderResource shaderResource;

    public static object Create(string path)
    {
        return ResourceManager.instance.LoadShader(path);
    }

    internal Shader(ShaderResource resource)
    {
        shaderResource = resource;

        Guid = resource?.Guid;
    }

    ~Shader()
    {
        Destroy();
    }

    internal ShaderHandle GetUniformHandle(StringID name, StringID variantKey)
    {
        if(shaderResource == null)
        {
            return default;
        }

        var uniform = shaderResource.GetUniform(name, variantKey);

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
    public void SetFloat(StringID variantKey, ShaderHandle handle, float value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(StringID variantKey, ShaderHandle handle, Vector2 value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }
 
    /// <summary>
    /// Sets a Vector2 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector2(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Vector2> value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector3(StringID variantKey, ShaderHandle handle, Vector3 value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector3(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Vector3> value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector4(StringID variantKey, ShaderHandle handle, Vector4 value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Vector4 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetVector4(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Vector4> value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetColor(StringID variantKey, ShaderHandle handle, Color value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Color uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetColor(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Color> value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(StringID variantKey, ShaderHandle handle, Matrix3x3 value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Matrix3x3 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix3x3(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Matrix3x3> value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Matrix4x4 uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(StringID variantKey, ShaderHandle handle, Matrix4x4 value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
    }

    /// <summary>
    /// Sets a Matrix4x4 array uniform's value
    /// </summary>
    /// <param name="variantKey">The shader variant key to apply to</param>
    /// <param name="handle">The shader handle to use</param>
    /// <param name="value">The value</param>
    public void SetMatrix4x4(StringID variantKey, ShaderHandle handle, ReadOnlySpan<Matrix4x4> value)
    {
        if (shaderResource == null)
        {
            return;
        }

        shaderResource.SetValue(this, variantKey, handle, value);
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
    /// Creates a shader resource from shader data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="entries">The variant entries for the current renderer</param>
    /// <returns>The shader if valid</returns>
    internal static ShaderResource Create(SerializableShader data, Dictionary<string, SerializableShaderData> entries)
    {
        var resource = new ShaderResource(data, entries);

        if (resource.Create())
        {
            return resource;
        }

        return null;
    }
}
