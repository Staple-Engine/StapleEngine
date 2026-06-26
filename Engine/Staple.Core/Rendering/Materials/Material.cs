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
    internal MaterialResource materialResource;

    internal const string MainColorProperty = "mainColor";
    internal const string MainTextureProperty = "mainTexture";

    internal static readonly StringID MainColorPropertyHash = MainColorProperty;
    internal static readonly StringID MainTexturePropertyHash = MainTextureProperty;

    internal static Texture whiteTexture;

    public static Texture WhiteTexture
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

    internal Dictionary<StringID, ShaderHandle> shaderHandles = [];

    internal HashSet<int> shaderKeywords = [];

    internal StringID ShaderVariantKey { get; private set; } = "";
    private bool needsVariantKeyUpdate = true;

    /// <summary>
    /// The material's main color
    /// </summary>
    public Color MainColor
    {
        get => materialResource != null && materialResource.parameters.TryGetValue(MainColorPropertyHash, out var p) ? p.colorValue : Color.White;

        set => SetColor(MainColorProperty, value);
    }

    /// <summary>
    /// The material's main texture
    /// </summary>
    public Texture MainTexture
    {
        get => materialResource != null && materialResource.parameters.TryGetValue(MainTexturePropertyHash, out var p) &&
            !(p.textureValue?.Disposed ?? true) ?
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

        void HandleParameter(StringID key, MaterialResourceParameter parameter)
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

        foreach (var pair in materialResource.parameters)
        {
            HandleParameter(pair.Key, pair.Value);
        }

        foreach (var pair in materialResource.instanceParameters)
        {
            HandleParameter(pair.Key, pair.Value);
        }

        stateHash = hashCode.ToHashCode();
    }

    public GuidHasher Guid => materialResource?.Guid ?? new();

    /// <summary>
    /// Whether this material has been disposed and is now invalid.
    /// </summary>
    public bool Disposed => materialResource == null;

    public bool IsValid => !Disposed && materialResource?.shader != null && !materialResource.shader.Disposed;

    /// <summary>
    /// Valid shader keywords
    /// </summary>
    public IEnumerable<string> Keywords => materialResource?.shader?.shaderResource?.metadata?.variants ?? Enumerable.Empty<string>();

    private IShaderProgram shaderProgram;

    /// <summary>
    /// Gets the current shader program, if valid.
    /// </summary>
    internal IShaderProgram ShaderProgram
    {
        get
        {
            if(Disposed)
            {
                return null;
            }

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
        if(sourceMaterial == null || sourceMaterial.Disposed)
        {
            return;
        }

        materialResource = sourceMaterial.materialResource?.Clone();
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

        materialResource = null;
    }

    public static object Create(string path) => ResourceManager.instance.LoadMaterial(path);

    private void UpdateTextureBindingData()
    {
        if (materialResource == null ||
            !materialResource.shader.shaderResource.instances.TryGetValue(ShaderVariantKey, out var instance))
        {
            materialResource.vertexSamplers = null;
            materialResource.fragmentSamplers = null;

            return;
        }

        var vertexTextureCount = instance.vertexTextureBindings.Count;
        var fragmentTextureCount = instance.fragmentTextureBindings.Count;

        var (vertexSamplers, fragmentSamplers, vertexTextureBindings, fragmentTextureBindings, parameters) =
            (materialResource.vertexSamplers, materialResource.fragmentSamplers, materialResource.vertexTextureBindings,
            materialResource.fragmentTextureBindings, materialResource.parameters);

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

        (materialResource.vertexSamplers, materialResource.fragmentSamplers, materialResource.parameters) =
            (vertexSamplers, fragmentSamplers, parameters);
    }

    private void UpdateVariantKey()
    {
        ShaderVariantKey = "";

        if ((materialResource?.shader?.Disposed ?? true) ||
            materialResource.shader.shaderResource.metadata.type != ShaderType.VertexFragment)
        {
            return;
        }

        var c = shaderKeywords.Count;

        foreach(var pair in materialResource.shader.shaderResource.instances)
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

            shaderProgram = materialResource.shader.shaderResource.instances.TryGetValue(ShaderVariantKey, out var instance) ?
                instance.program : null;

            UpdateTextureBindingData();

            foreach(var parameter in materialResource.parameters)
            {
                parameter.Value.shaderHandle = materialResource.shader.GetUniformHandle(parameter.Key, ShaderVariantKey);
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
        if(materialResource?.shader == null ||
            materialResource.shader.Disposed ||
            (!materialResource.shader.shaderResource.metadata.variants.Contains(name) &&
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
        if(Disposed)
        {
            return;
        }

        var parameters = materialResource.parameters;
        var instanceParameters = materialResource.instanceParameters;

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
                parameters.AddOrSetKey(name, new MaterialResourceParameter()
                {
                    name = name,
                    type = MaterialParameterType.Color,
                    colorValue = value,
                    shaderHandle = materialResource.shader.GetUniformHandle(name, ShaderVariantKey),
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
                instanceParameters.AddOrSetKey(name, new MaterialResourceParameter()
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
        if (Disposed)
        {
            return;
        }

        var parameters = materialResource.parameters;
        var instanceParameters = materialResource.instanceParameters;

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
                parameter = new MaterialResourceParameter()
                {
                    name = name,
                    type = MaterialParameterType.Int,
                    intValue = value,
                    shaderHandle = materialResource.shader.GetUniformHandle(name, ShaderVariantKey),
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
                instanceParameters.AddOrSetKey(name, new MaterialResourceParameter()
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
        if (Disposed)
        {
            return;
        }

        var parameters = materialResource.parameters;
        var instanceParameters = materialResource.instanceParameters;

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
                parameter = new MaterialResourceParameter()
                {
                    name = name,
                    type = MaterialParameterType.Float,
                    floatValue = value,
                    shaderHandle = materialResource.shader.GetUniformHandle(name, ShaderVariantKey),
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
                instanceParameters.AddOrSetKey(name, new MaterialResourceParameter()
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
        if (Disposed)
        {
            return;
        }

        var parameters = materialResource.parameters;
        var instanceParameters = materialResource.instanceParameters;

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
                parameters.AddOrSetKey(name, new MaterialResourceParameter()
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
                instanceParameters.AddOrSetKey(name, new MaterialResourceParameter()
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
        if (Disposed)
        {
            return;
        }

        var parameters = materialResource.parameters;
        var instanceParameters = materialResource.instanceParameters;

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
                parameters.AddOrSetKey(name, new MaterialResourceParameter()
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
                instanceParameters.AddOrSetKey(name, new MaterialResourceParameter()
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
        if (Disposed)
        {
            return;
        }

        var parameters = materialResource.parameters;
        var instanceParameters = materialResource.instanceParameters;

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
                parameters.AddOrSetKey(name, new MaterialResourceParameter()
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
                instanceParameters.AddOrSetKey(name, new MaterialResourceParameter()
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
        if (Disposed)
        {
            return;
        }

        var parameters = materialResource.parameters;
        var instanceParameters = materialResource.instanceParameters;

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
            var original = value;

            value ??= WhiteTexture;

            parameter = new MaterialResourceParameter()
            {
                name = name,
                type = MaterialParameterType.Texture,
                textureValue = value,
                hasTexture = value != null,
                shaderHandle = materialResource.shader.GetUniformHandle(name, ShaderVariantKey),
            };

            parameters.AddOrSetKey(name, parameter);

            if(parameter.shaderHandle.Variant == null)
            {
                return;
            }

            //Textures are replaced with White by default - Ensure we only enable variants if it's not White
            if (parameter.hasTexture && (original != null || value.Guid.Guid != "WHITE"))
            {
                EnableShaderKeyword(parameter.shaderHandle.Variant);
            }
            else
            {
                DisableShaderKeyword(parameter.shaderHandle.Variant);
            }
        }
    }

    /// <summary>
    /// Sets a Matrix3x3 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Matrix3x3 value</param>
    public void SetMatrix3x3(string name, Matrix3x3 value, MaterialParameterSource source = MaterialParameterSource.Uniform)
    {
        if (Disposed)
        {
            return;
        }

        var parameters = materialResource.parameters;
        var instanceParameters = materialResource.instanceParameters;

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
                parameters.AddOrSetKey(name, new MaterialResourceParameter()
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
                instanceParameters.AddOrSetKey(name, new MaterialResourceParameter()
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
        if (Disposed)
        {
            return;
        }

        var parameters = materialResource.parameters;
        var instanceParameters = materialResource.instanceParameters;

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
                parameters.AddOrSetKey(name, new MaterialResourceParameter()
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
                instanceParameters.AddOrSetKey(name, new MaterialResourceParameter()
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
        if(Disposed)
        {
            return default;
        }

        if(shaderHandles.TryGetValue(name, out var shaderHandle))
        {
            return shaderHandle;
        }

        shaderHandle = materialResource.shader.GetUniformHandle(name, ShaderVariantKey);

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
        if(materialResource?.shader == null ||
            !materialResource.shader.shaderResource.instances.TryGetValue(ShaderVariantKey, out var shaderInstance))
        {
            state.shader = null;
            state.shaderInstance = null;

            return;
        }

        var (shader, parameters, sourceBlend, destinationBlend, vertexTextureBindings, fragmentTextureBindings,
            vertexSamplers, fragmentSamplers) =
            (materialResource.shader, materialResource.parameters, materialResource.shader.shaderResource.sourceBlend,
            materialResource.shader.shaderResource.destinationBlend, materialResource.vertexTextureBindings,
            materialResource.fragmentTextureBindings, materialResource.vertexSamplers, materialResource.fragmentSamplers);

        state.shader = shader;
        state.shaderInstance = shaderInstance;
        state.sourceBlend = sourceBlend;
        state.destinationBlend = destinationBlend;
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

        (materialResource.vertexSamplers, materialResource.fragmentSamplers) = (vertexSamplers, fragmentSamplers);
    }
}
