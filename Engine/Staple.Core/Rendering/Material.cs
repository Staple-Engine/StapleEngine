using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple;

/// <summary>
/// Material resource
/// </summary>
public sealed class Material : IGuidAsset
{
    internal class ParameterInfo
    {
        public string name;
        public MaterialParameterType type;
        public ShaderHandle shaderHandle;
        public int intValue;
        public float floatValue;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Vector4 vector4Value;
        public Color colorValue;
        public Texture textureValue;
        public Matrix3x3 matrix3x3Value = Matrix3x3.Identity;
        public Matrix4x4 matrix4x4Value = Matrix4x4.Identity;
        public TextureWrap textureWrapValue;
        public bool hasTexture;

        public ParameterInfo Clone()
        {
            return new()
            {
                name = name,
                type = type,
                shaderHandle = shaderHandle,
                intValue = intValue,
                floatValue = floatValue,
                vector2Value = vector2Value,
                vector3Value = vector3Value,
                vector4Value = vector4Value,
                colorValue = colorValue,
                textureValue = textureValue,
                matrix3x3Value = matrix3x3Value,
                matrix4x4Value = matrix4x4Value,
                textureWrapValue = textureWrapValue,
                hasTexture = hasTexture,
            };
        }

        public override string ToString()
        {
            return $"{name} ({type})";
        }
    }

    internal const string MainColorProperty = "mainColor";
    internal const string MainTextureProperty = "mainTexture";

    internal static readonly StringID MainColorPropertyHash = MainColorProperty;
    internal static readonly StringID MainTexturePropertyHash = MainTextureProperty;

    internal static Texture whiteTexture;

    internal static Texture WhiteTexture
    {
        get
        {
            if (whiteTexture != null)
            {
                return whiteTexture;
            }
            
            var pixels = Enumerable.Repeat((byte)255, 64 * 64 * 4).ToArray();

            whiteTexture = Texture.CreatePixels("WHITE", pixels, 64, 64, new TextureMetadata()
            {
                filter = TextureFilter.Linear,
                format = TextureMetadataFormat.RGBA8,
                type = TextureType.Texture,
                useMipmaps = false,
            }, TextureFormat.RGBA8);

            return whiteTexture;
        }
    }

    internal Shader shader;
    internal MaterialMetadata metadata;

    internal readonly Dictionary<ParameterInfo, int> vertexTextureBindings = [];
    internal readonly Dictionary<ParameterInfo, int> fragmentTextureBindings = [];

    internal Texture[] vertexSamplers;

    internal Texture[] fragmentSamplers;

    internal Dictionary<StringID, ParameterInfo> parameters = [];

    internal Dictionary<StringID, ParameterInfo> instanceParameters = [];

    internal Dictionary<StringID, ShaderHandle> shaderHandles = [];

    internal HashSet<int> shaderKeywords = [];

    internal StringID ShaderVariantKey { get; private set; } = "";
    private bool needsVariantKeyUpdate = true;

    /// <summary>
    /// The material's main color
    /// </summary>
    public Color MainColor
    {
        get => parameters.TryGetValue(MainColorPropertyHash, out var p) ? p.colorValue : Color.White;

        set => SetColor(MainColorProperty, value);
    }

    /// <summary>
    /// The material's main texture
    /// </summary>
    public Texture MainTexture
    {
        get => parameters.TryGetValue(MainTexturePropertyHash, out var p) && !(p.textureValue?.Disposed ?? true) ?
            p.textureValue : WhiteTexture;

        set => SetTexture(MainTextureProperty, value);
    }

    /// <summary>
    /// Culling mode for this material
    /// </summary>
    public CullingMode CullingMode { get; set; } = CullingMode.Back;

    internal bool needsHashUpdate = true;
    internal int stateHash;

    public int StateHash
    {
        get
        {
            if (!needsHashUpdate)
            {
                return stateHash;
            }
            
            needsHashUpdate = false;

            UpdateStateHash();

            return stateHash;
        }
    }

    internal void UpdateStateHash()
    {
        var hashCode = new HashCode();

        hashCode.Add(Guid.GuidHash);

        void HandleParameter(StringID key, ParameterInfo parameter)
        {
            hashCode.Add(key);
            hashCode.Add(parameter.type);

            switch (parameter.type)
            {
                case MaterialParameterType.Int:

                    hashCode.Add(parameter.intValue);

                    break;

                case MaterialParameterType.Color:

                    hashCode.Add(parameter.colorValue);

                    break;

                case MaterialParameterType.Float:

                    hashCode.Add(parameter.floatValue);

                    break;

                case MaterialParameterType.Matrix3x3:

                    hashCode.Add(parameter.matrix3x3Value);

                    break;

                case MaterialParameterType.Matrix4x4:

                    hashCode.Add(parameter.matrix4x4Value);

                    break;

                case MaterialParameterType.Texture:

                    hashCode.Add(parameter.textureValue != null ? parameter.textureValue.Guid.GuidHash : 0);

                    break;

                case MaterialParameterType.TextureWrap:

                    hashCode.Add(parameter.textureWrapValue);

                    break;

                case MaterialParameterType.Vector2:

                    hashCode.Add(parameter.vector2Value);

                    break;

                case MaterialParameterType.Vector3:

                    hashCode.Add(parameter.vector3Value);

                    break;

                case MaterialParameterType.Vector4:

                    hashCode.Add(parameter.vector4Value);

                    break;
            }
        }

        foreach (var pair in parameters)
        {
            HandleParameter(pair.Key, pair.Value);
        }

        foreach (var pair in instanceParameters)
        {
            HandleParameter(pair.Key, pair.Value);
        }

        stateHash = hashCode.ToHashCode();
    }

    public GuidHasher Guid { get; } = new();

    /// <summary>
    /// Whether this material has been disposed and is now invalid.
    /// </summary>
    public bool Disposed { get; internal set; } = false;

    public bool IsValid => !Disposed && shader != null && !shader.Disposed;

    /// <summary>
    /// Valid shader keywords
    /// </summary>
    public IEnumerable<string> Keywords => shader.metadata?.variants ?? Enumerable.Empty<string>();

    private IShaderProgram shaderProgram;

    /// <summary>
    /// Gets the current shader program, if valid.
    /// </summary>
    internal IShaderProgram ShaderProgram
    {
        get
        {
            if (!needsVariantKeyUpdate)
            {
                return shaderProgram;
            }
            
            needsVariantKeyUpdate = false;

            UpdateVariantKey();

            return shaderProgram;
        }
    }

    public Material()
    {
    }

    public Material(Material sourceMaterial)
    {
        if(sourceMaterial == null)
        {
            return;
        }

        foreach (var parameter in sourceMaterial.parameters)
        {
            parameters.AddOrSetKey(parameter.Key, parameter.Value.Clone());
        }

        metadata = sourceMaterial.metadata;
        Guid = sourceMaterial.Guid;
        shader = sourceMaterial.shader;
        shaderHandles = new(sourceMaterial.shaderHandles);
        CullingMode = sourceMaterial.CullingMode;

        MainColor = sourceMaterial.MainColor;
        MainTexture = sourceMaterial.MainTexture;
    }

    ~Material()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys this material's resources.
    /// </summary>
    public void Destroy()
    {
        if(Disposed)
        {
            return;
        }

        Disposed = true;
    }

    /// <summary>
    /// IPathAsset implementation. Loads a material from path.
    /// </summary>
    /// <param name="path">The path to load from</param>
    /// <returns>The material, or null</returns>
    public static object Create(string path) => ResourceManager.instance.LoadMaterial(path);

    private void UpdateTextureBindingData()
    {
        if (!shader.instances.TryGetValue(ShaderVariantKey, out var instance))
        {
            vertexSamplers = null;
            fragmentSamplers = null;

            return;
        }

        var vertexTextureCount = instance.vertexTextureBindings.Count;
        var fragmentTextureCount = instance.fragmentTextureBindings.Count;

        if (vertexSamplers == null || vertexSamplers.Length != vertexTextureCount)
        {
            Array.Resize(ref vertexSamplers, vertexTextureCount);
        }

        if (fragmentSamplers == null || fragmentSamplers.Length != fragmentTextureCount)
        {
            Array.Resize(ref fragmentSamplers, fragmentTextureCount);
        }

        for(var i = 0; i < vertexSamplers.Length; i++)
        {
            vertexSamplers[i] = WhiteTexture;
        }

        for (var i = 0; i < fragmentSamplers.Length; i++)
        {
            fragmentSamplers[i] = WhiteTexture;
        }

        vertexTextureBindings.Clear();
        fragmentTextureBindings.Clear();

        foreach(var pair in instance.vertexTextureBindings)
        {
            if (!parameters.TryGetValue(pair.Key, out var p))
            {
                continue;
            }
            
            vertexTextureBindings.Add(p, pair.Value);

            vertexSamplers[pair.Value] = p.textureValue;
        }

        foreach (var pair in instance.fragmentTextureBindings)
        {
            if (!parameters.TryGetValue(pair.Key, out var p))
            {
                continue;
            }
            
            fragmentTextureBindings.Add(p, pair.Value);

            fragmentSamplers[pair.Value] = p.textureValue;
        }
    }

    private void UpdateVariantKey()
    {
        ShaderVariantKey = "";

        if ((shader?.Disposed ?? true) ||
            shader.metadata.type != ShaderType.VertexFragment)
        {
            return;
        }

        var c = shaderKeywords.Count;

        foreach(var pair in shader.instances)
        {
            if(pair.Value.keyPieces.Length != c)
            {
                continue;
            }

            var found = false;

            foreach(var piece in pair.Value.keyPieces)
            {
                found |= !shaderKeywords.Contains(piece);

                if(found)
                {
                    break;
                }
            }

            if(found)
            {
                continue;
            }

            ShaderVariantKey = pair.Key;

            shaderProgram = shader.instances.TryGetValue(ShaderVariantKey, out var instance) ?
                instance.program : null;

            UpdateTextureBindingData();

            foreach(var parameter in parameters)
            {
                parameter.Value.shaderHandle = shader.GetUniformHandle(parameter.Key, ShaderVariantKey);
            }

            break;
        }
    }

    /// <summary>
    /// Enables a shader keyword
    /// </summary>
    /// <param name="name">The keyword</param>
    public void EnableShaderKeyword(string name)
    {
        if(shader == null ||
            shader.Disposed ||
            (!shader.metadata.variants.Contains(name) &&
            !Shader.DefaultVariants.Contains(name)))
        {
            return;
        }

        var length = shaderKeywords.Count;

        shaderKeywords.Add(name.GetHashCode());

        if (length != shaderKeywords.Count)
        {
            UpdateVariantKey();
        }
    }

    /// <summary>
    /// Disables a shader keyword
    /// </summary>
    /// <param name="name">The keyword</param>
    public void DisableShaderKeyword(string name)
    {
        var length = shaderKeywords.Count;

        shaderKeywords.Remove(name.GetHashCode());

        if (length != shaderKeywords.Count)
        {
            UpdateVariantKey();
        }
    }

    /// <summary>
    /// Checks whether a shader keyword is enabled for this material
    /// </summary>
    /// <param name="name">The keyword</param>
    /// <returns>Whether it is enabled</returns>
    public bool IsShaderKeywordEnabled(string name)
    {
        return shaderKeywords.Contains(name.GetHashCode());
    }

    /// <summary>
    /// Sets a color property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The color value</param>
    public void SetColor(string name, Color value, MaterialParameterSource source = MaterialParameterSource.Uniform)
    {
        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Color)
                {
                    parameter.colorValue = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Color,
                    colorValue = value,
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Color)
                {
                    parameter.colorValue = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Color,
                    colorValue = value,
                });
            }
        }
    }

    /// <summary>
    /// Sets an int property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The int value</param>
    public void SetInt(string name, int value, MaterialParameterSource source = MaterialParameterSource.Uniform)
    {
        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type != MaterialParameterType.Int)
                {
                    return;
                }
                
                parameter.intValue = value;

                if (parameter.shaderHandle.Variant == null)
                {
                    return;
                }
                
                if (parameter.intValue <= 0)
                {
                    DisableShaderKeyword(parameter.shaderHandle.Variant);
                }
                else
                {
                    EnableShaderKeyword(parameter.shaderHandle.Variant);
                }
            }
            else
            {
                parameter = new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Int,
                    intValue = value,
                };

                parameters.AddOrSetKey(name, parameter);

                if (parameter.shaderHandle.Variant == null)
                {
                    return;
                }
                
                if (parameter.intValue <= 0)
                {
                    DisableShaderKeyword(parameter.shaderHandle.Variant);
                }
                else
                {
                    EnableShaderKeyword(parameter.shaderHandle.Variant);
                }
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Int)
                {
                    parameter.intValue = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Int,
                    intValue = value,
                });
            }
        }
    }

    /// <summary>
    /// Sets a float property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The float value</param>
    public void SetFloat(string name, float value, MaterialParameterSource source = MaterialParameterSource.Uniform)
    {
        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type != MaterialParameterType.Float)
                {
                    return;
                }
                
                parameter.floatValue = value;

                if (parameter.shaderHandle.Variant == null)
                {
                    return;
                }
                
                if (parameter.floatValue <= 0)
                {
                    DisableShaderKeyword(parameter.shaderHandle.Variant);
                }
                else
                {
                    EnableShaderKeyword(parameter.shaderHandle.Variant);
                }
            }
            else
            {
                parameter = new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Float,
                    floatValue = value,
                };

                parameters.AddOrSetKey(name, parameter);

                if (parameter.shaderHandle.Variant == null)
                {
                    return;
                }
                
                if (parameter.floatValue <= 0)
                {
                    DisableShaderKeyword(parameter.shaderHandle.Variant);
                }
                else
                {
                    EnableShaderKeyword(parameter.shaderHandle.Variant);
                }
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Float)
                {
                    parameter.floatValue = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Float,
                    floatValue = value,
                });
            }
        }
    }

    /// <summary>
    /// Sets a Vector2 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Vector2 value</param>
    public void SetVector2(string name, Vector2 value, MaterialParameterSource source = MaterialParameterSource.Uniform)
    {
        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector2)
                {
                    parameter.vector2Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector2,
                    vector2Value = value,
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector2)
                {
                    parameter.vector2Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector2,
                    vector2Value = value,
                });
            }
        }
    }

    /// <summary>
    /// Sets a Vector3 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Vector3 value</param>
    public void SetVector3(string name, Vector3 value, MaterialParameterSource source = MaterialParameterSource.Uniform)
    {
        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector3)
                {
                    parameter.vector3Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector3,
                    vector3Value = value,
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector3)
                {
                    parameter.vector3Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector3,
                    vector3Value = value,
                });
            }
        }
    }

    /// <summary>
    /// Sets a Vector4 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Vector4 value</param>
    public void SetVector4(string name, Vector4 value, MaterialParameterSource source = MaterialParameterSource.Uniform)
    {
        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector4)
                {
                    parameter.vector4Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector4,
                    vector4Value = value,
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector4)
                {
                    parameter.vector4Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector4,
                    vector4Value = value,
                });
            }
        }
    }

    /// <summary>
    /// Sets a Texture property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Texture value</param>
    public void SetTexture(string name, Texture value)
    {
        needsHashUpdate = true;

        if (parameters.TryGetValue(name, out var parameter))
        {
            if (parameter.type != MaterialParameterType.Texture)
            {
                return;
            }
            
            if(value == null)
            {
                if(!string.IsNullOrEmpty(parameter.shaderHandle.DefaultValue))
                {
                    if (parameter.shaderHandle.DefaultValue == WhiteTexture.Guid.Guid)
                    {
                        value = WhiteTexture;
                    }
                    else
                    {
                        value = ResourceManager.instance.LoadTexture(parameter.shaderHandle.DefaultValue);
                    }
                }
                else
                {
                    value = WhiteTexture;
                }
            }

            parameter.textureValue = value;
            parameter.hasTexture = value != null;
        }
        else
        {
            value ??= WhiteTexture;

            parameters.AddOrSetKey(name, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Texture,
                textureValue = value,
                hasTexture = value != null,
            });
        }
    }

    /// <summary>
    /// Sets a Matrix3x3 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Matrix3x3 value</param>
    public void SetMatrix3x3(string name, Matrix3x3 value, MaterialParameterSource source = MaterialParameterSource.Uniform)
    {
        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix3x3)
                {
                    parameter.matrix3x3Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Matrix3x3,
                    matrix3x3Value = value,
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix3x3)
                {
                    parameter.matrix3x3Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Matrix3x3,
                    matrix3x3Value = value,
                });
            }
        }
    }

    /// <summary>
    /// Sets a Matrix4x4 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Matrix4x4 value</param>
    public void SetMatrix4x4(string name, Matrix4x4 value, MaterialParameterSource source = MaterialParameterSource.Uniform)
    {
        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix4x4)
                {
                    parameter.matrix4x4Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Matrix4x4,
                    matrix4x4Value = value,
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix4x4)
                {
                    parameter.matrix4x4Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(name, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Matrix4x4,
                    matrix4x4Value = value,
                });
            }
        }
    }

    /// <summary>
    /// Gets and caches a shader handle from a name
    /// </summary>
    /// <param name="name">The uniform name</param>
    /// <returns>The shader handle, or null</returns>
    internal ShaderHandle GetShaderHandle(StringID name)
    {
        if(shaderHandles.TryGetValue(name, out var shaderHandle))
        {
            return shaderHandle;
        }

        shaderHandle = shader.GetUniformHandle(name, ShaderVariantKey);

        if(shaderHandle.IsValid)
        {
            shaderHandles.Add(name, shaderHandle);
        }

        return shaderHandle;
    }

    /// <summary>
    /// Applies the default properties of this material to the shader
    /// </summary>
    /// <param name="state">The render state to apply to</param>
    internal void ApplyProperties(ref RenderState state)
    {
        if(shader == null ||
            !shader.instances.TryGetValue(ShaderVariantKey, out var shaderInstance))
        {
            state.shader = null;
            state.shaderInstance = null;

            return;
        }

        state.shader = shader;
        state.shaderInstance = shaderInstance;
        state.sourceBlend = shader.sourceBlend;
        state.destinationBlend = shader.destinationBlend;
        state.cull = CullingMode;

        foreach (var parameter in parameters.Values)
        {
            if(!parameter.shaderHandle.IsValid)
            {
                continue;
            }

            switch (parameter.type)
            {
                case MaterialParameterType.Texture:

                    if (parameter.textureValue?.Disposed ?? true)
                    {
                        continue;
                    }

                    if (vertexTextureBindings.TryGetValue(parameter, out var binding))
                    {
                        vertexSamplers[binding] = parameter.textureValue;
                    }

                    if (fragmentTextureBindings.TryGetValue(parameter, out binding))
                    {
                        fragmentSamplers[binding] = parameter.textureValue;
                    }

                    break;

                case MaterialParameterType.Matrix3x3:

                    shader.SetMatrix3x3(ShaderVariantKey, parameter.shaderHandle, parameter.matrix3x3Value);

                    break;

                case MaterialParameterType.Matrix4x4:

                    shader.SetMatrix4x4(ShaderVariantKey, parameter.shaderHandle, parameter.matrix4x4Value);

                    break;

                case MaterialParameterType.Float:

                    shader.SetFloat(ShaderVariantKey, parameter.shaderHandle, parameter.floatValue);

                    break;

                case MaterialParameterType.Int:

                    shader.SetFloat(ShaderVariantKey, parameter.shaderHandle, parameter.intValue);

                    break;

                case MaterialParameterType.Vector2:

                    shader.SetVector2(ShaderVariantKey, parameter.shaderHandle, parameter.vector2Value);

                    break;

                case MaterialParameterType.Vector3:

                    shader.SetVector3(ShaderVariantKey, parameter.shaderHandle, parameter.vector3Value);

                    break;

                case MaterialParameterType.Vector4:

                    shader.SetVector4(ShaderVariantKey, parameter.shaderHandle, parameter.vector4Value);

                    break;

                case MaterialParameterType.Color:

                    shader.SetColor(ShaderVariantKey, parameter.shaderHandle, parameter.colorValue);

                    break;
            }
        }

        state.vertexTextures = vertexSamplers;
        state.fragmentTextures = fragmentSamplers;
    }
}
