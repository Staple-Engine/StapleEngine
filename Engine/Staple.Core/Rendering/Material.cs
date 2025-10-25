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
        public ShaderHandle[] relatedShaderHandles = [];
        public ParameterInfo[] relatedParameters = [];
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
                relatedShaderHandles = relatedShaderHandles,
                relatedParameters = relatedParameters
                    .Where(x => x != null)
                    .Select(x => x.Clone())
                    .ToArray(),
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

    internal enum ApplyMode
    {
        IgnoreTextures,
        TexturesOnly,
        All,
    }

    internal const string MainColorProperty = "mainColor";
    internal const string MainTextureProperty = "mainTexture";

    internal static readonly int MainColorPropertyHash = MainColorProperty.GetHashCode();
    internal static readonly int MainTexturePropertyHash = MainTextureProperty.GetHashCode();

    internal static Texture whiteTexture;

    internal static Texture WhiteTexture
    {
        get
        {
            if (whiteTexture == null)
            {
                var pixels = Enumerable.Repeat((byte)255, 64 * 64 * 4).ToArray();

                whiteTexture = Texture.CreatePixels("WHITE", pixels, 64, 64, new TextureMetadata()
                {
                    filter = TextureFilter.Linear,
                    format = TextureMetadataFormat.RGBA8,
                    type = TextureType.Texture,
                    useMipmaps = false,
                }, TextureFormat.RGBA8);
            }

            return whiteTexture;
        }
    }

    internal Shader shader;
    internal MaterialMetadata metadata;

    internal Texture[] textures;

    internal Texture[] Textures
    {
        get
        {
            var textureCount = hasMainTexture ? 1 : 0;

            foreach(var pair in parameters)
            {
                if(pair.Value?.textureValue?.Disposed ?? true)
                {
                    continue;
                }

                textureCount++;
            }

            if(textures == null || textures.Length != textureCount)
            {
                Array.Resize(ref textures, textureCount);
            }

            if(hasMainTexture)
            {
                textures[0] = MainTexture;
            }

            var counter = 1;

            foreach (var pair in parameters)
            {
                if (pair.Value?.textureValue?.Disposed ?? true)
                {
                    continue;
                }

                textures[counter++] = pair.Value.textureValue;
            }

            return textures;
        }
    }

    internal Dictionary<int, ParameterInfo> parameters = [];

    internal Dictionary<int, ParameterInfo> instanceParameters = [];

    internal Dictionary<int, ShaderHandle> shaderHandles = [];

    internal HashSet<int> shaderKeywords = [];

    private Action[] applyPropertiesCallbacks = [];

    private readonly Dictionary<int, string> cachedShaderVariantKeys = [];

    private Color mainColor = Color.White;
    private ShaderHandle mainColorHandle;
    private bool hasMainColor;
    private bool initedMainColor;

    private Texture mainTexture;
    private ShaderHandle mainTextureHandle;
    private bool hasMainTexture;
    private bool initedMainTexture;

    internal string ShaderVariantKey { get; private set; } = "";

    /// <summary>
    /// The material's main color
    /// </summary>
    public Color MainColor
    {
        get => mainColor;

        set
        {
            mainColor = value;

            if(initedMainColor == false)
            {
                initedMainColor = true;

                mainColorHandle = shader.GetUniformHandle(MainColorPropertyHash);

                hasMainColor = mainColorHandle.IsValid;
            }
        }
    }

    /// <summary>
    /// The material's main texture
    /// </summary>
    public Texture MainTexture
    {
        get => mainTexture;

        set
        {
            mainTexture = value;

            if (initedMainTexture == false)
            {
                initedMainTexture = true;

                mainTextureHandle = shader.GetUniformHandle(MainTexturePropertyHash);

                hasMainTexture = mainTextureHandle.IsValid;
            }
        }
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
            if(needsHashUpdate)
            {
                needsHashUpdate = false;

                UpdateStateHash();
            }

            return stateHash;
        }
    }

    internal void UpdateStateHash()
    {
        var hashCode = new HashCode();

        hashCode.Add(Guid.GuidHash);

        void HandleParameter(int key, ParameterInfo parameter)
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

    private readonly GuidHasher guidHasher = new();

    public GuidHasher Guid => guidHasher;

    /// <summary>
    /// Whether this material has been disposed and is now invalid.
    /// </summary>
    public bool Disposed { get; internal set; } = false;

    public bool IsValid
    {
        get
        {
            return Disposed == false &&
                shader != null &&
                shader.Disposed == false;
        }
    }

    /// <summary>
    /// Valid shader keywords
    /// </summary>
    public IEnumerable<string> Keywords => shader.metadata?.variants ?? Enumerable.Empty<string>();

    /// <summary>
    /// Gets the current shader program, if valid.
    /// </summary>
    internal IShaderProgram ShaderProgram => shader != null &&
        shader.Disposed == false &&
        shader.metadata.type == ShaderType.VertexFragment &&
        shader.instances.TryGetValue(ShaderVariantKey, out var instance) ?
        instance.program : null;

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
        guidHasher = sourceMaterial.guidHasher;
        shader = sourceMaterial.shader;
        shaderHandles = new(sourceMaterial.shaderHandles);
        CullingMode = sourceMaterial.CullingMode;

        if(sourceMaterial.hasMainColor)
        {
            MainColor = sourceMaterial.MainColor;
        }

        if(sourceMaterial.hasMainTexture)
        {
            MainTexture = sourceMaterial.MainTexture;
        }
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

    private void UpdateVariantKey()
    {
        ShaderVariantKey = "";

        if(shader == null ||
            shader.Disposed)
        {
            return;
        }

        var hash = 0;

        foreach(var keyword in shaderKeywords)
        {
            hash ^= keyword.GetHashCode();
        }

        if (cachedShaderVariantKeys.TryGetValue(hash, out var variantKey))
        {
            ShaderVariantKey = variantKey;

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
                found |= shaderKeywords.Contains(piece) == false;

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

            cachedShaderVariantKeys.Add(hash, pair.Key);

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
            (shader.metadata.variants.Contains(name) == false &&
            Shader.DefaultVariants.Contains(name) == false))
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
        var hash = name.GetHashCode();

        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Color)
                {
                    parameter.colorValue = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Color,
                    colorValue = value,
                    shaderHandle = shader.GetUniformHandle(hash),
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Color)
                {
                    parameter.colorValue = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Color,
                    colorValue = value,
                    shaderHandle = shader.GetUniformHandle(hash),
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
        var hash = name.GetHashCode();

        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Int)
                {
                    parameter.intValue = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Int,
                    intValue = value,
                    shaderHandle = shader.GetUniformHandle(hash),
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Int)
                {
                    parameter.intValue = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Int,
                    intValue = value,
                    shaderHandle = shader.GetUniformHandle(hash),
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
        var hash = name.GetHashCode();

        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Float)
                {
                    parameter.floatValue = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Float,
                    floatValue = value,
                    shaderHandle = shader.GetUniformHandle(hash),
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Float)
                {
                    parameter.floatValue = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Float,
                    floatValue = value,
                    shaderHandle = shader.GetUniformHandle(hash),
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
        var hash = name.GetHashCode();

        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector2)
                {
                    parameter.vector2Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector2,
                    vector2Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector2)
                {
                    parameter.vector2Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector2,
                    vector2Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
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
        var hash = name.GetHashCode();

        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector3)
                {
                    parameter.vector3Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector3,
                    vector3Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector3)
                {
                    parameter.vector3Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector3,
                    vector3Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
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
        var hash = name.GetHashCode();

        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector4)
                {
                    parameter.vector4Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector4,
                    vector4Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector4)
                {
                    parameter.vector4Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Vector4,
                    vector4Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
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
        var hash = name.GetHashCode();

        needsHashUpdate = true;

        if (parameters.TryGetValue(hash, out var parameter))
        {
            if (parameter.type == MaterialParameterType.Texture)
            {
                if(value == null && (parameter.shaderHandle.DefaultValue?.Length ?? 0) > 0)
                {
                    if(parameter.shaderHandle.DefaultValue == WhiteTexture.Guid.Guid)
                    {
                        value = WhiteTexture;
                    }
                    else
                    {
                        value = ResourceManager.instance.LoadTexture(parameter.shaderHandle.DefaultValue);
                    }
                }

                parameter.textureValue = value;
                parameter.hasTexture = value != null;
            }
        }
        else
        {
            var handle = shader.GetUniformHandle(hash);

            if (value == null && (handle.DefaultValue?.Length ?? 0) > 0)
            {
                if (handle.DefaultValue == WhiteTexture.Guid.Guid)
                {
                    value = WhiteTexture;
                }
                else
                {
                    value = ResourceManager.instance.LoadTexture(handle.DefaultValue);
                }
            }

            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Texture,
                textureValue = value,
                hasTexture = value != null,
                shaderHandle = handle,
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
        var hash = name.GetHashCode();

        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix3x3)
                {
                    parameter.matrix3x3Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Matrix3x3,
                    matrix3x3Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix3x3)
                {
                    parameter.matrix3x3Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Matrix3x3,
                    matrix3x3Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
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
        var hash = name.GetHashCode();

        needsHashUpdate = true;

        if (source == MaterialParameterSource.Uniform)
        {
            if (parameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix4x4)
                {
                    parameter.matrix4x4Value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Matrix4x4,
                    matrix4x4Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
                });
            }
        }
        else
        {
            if (instanceParameters.TryGetValue(hash, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix4x4)
                {
                    parameter.matrix4x4Value = value;
                }
            }
            else
            {
                instanceParameters.AddOrSetKey(hash, new ParameterInfo()
                {
                    name = name,
                    type = MaterialParameterType.Matrix4x4,
                    matrix4x4Value = value,
                    shaderHandle = shader.GetUniformHandle(hash),
                });
            }
        }
    }

    /// <summary>
    /// Gets and caches a shader handle from a name
    /// </summary>
    /// <param name="name">The uniform name</param>
    /// <returns>The shader handle, or null</returns>
    internal ShaderHandle GetShaderHandle(string name)
    {
        var hash = name.GetHashCode();

        if(shaderHandles.TryGetValue(hash, out var shaderHandle))
        {
            return shaderHandle;
        }

        shaderHandle = shader.GetUniformHandle(hash);

        if(shaderHandle.IsValid)
        {
            shaderHandles.Add(hash, shaderHandle);
        }

        return shaderHandle;
    }

    /// <summary>
    /// Applies the default properties of this material to the shader
    /// </summary>
    /// <param name="applyMode">How to apply the properties</param>
    /// <param name="state">The render state to apply to</param>
    internal void ApplyProperties(ApplyMode applyMode, ref RenderState state)
    {
        if(shader == null)
        {
            return;
        }

        if(applyMode != ApplyMode.IgnoreTextures)
        {
            state.textures = Textures;
        }

        if(hasMainColor)
        {
            shader.SetColor(mainColorHandle, mainColor);
        }

        if (applyPropertiesCallbacks.Length != parameters.Count)
        {
            applyPropertiesCallbacks = new Action[parameters.Count];

            var counter = 0;

            foreach (var parameter in parameters.Values)
            {
                if((applyMode == ApplyMode.IgnoreTextures && parameter.type == MaterialParameterType.Texture) ||
                    (applyMode == ApplyMode.TexturesOnly && parameter.type != MaterialParameterType.Texture))
                {
                    continue;
                }

                var key = parameter.name;

                switch (parameter.type)
                {
                    case MaterialParameterType.Matrix3x3:

                        applyPropertiesCallbacks[counter++] = () =>
                        {
                            shader.SetMatrix3x3(parameter.shaderHandle, parameter.matrix3x3Value);
                        };

                        break;

                    case MaterialParameterType.Matrix4x4:

                        applyPropertiesCallbacks[counter++] = () =>
                        {
                            shader.SetMatrix4x4(parameter.shaderHandle, parameter.matrix4x4Value);
                        };

                        break;

                    case MaterialParameterType.Float:

                        applyPropertiesCallbacks[counter++] = () =>
                        {
                            shader.SetVector4(parameter.shaderHandle, new Vector4(parameter.floatValue, 0, 0, 0));

                            if (parameter.shaderHandle.Variant != null)
                            {
                                if(parameter.floatValue <= 0)
                                {
                                    DisableShaderKeyword(parameter.shaderHandle.Variant);
                                }
                                else
                                {
                                    EnableShaderKeyword(parameter.shaderHandle.Variant);
                                }
                            }
                        };

                        break;

                    case MaterialParameterType.Int:

                        applyPropertiesCallbacks[counter++] = () =>
                        {
                            shader.SetVector4(parameter.shaderHandle, new Vector4(parameter.intValue, 0, 0, 0));

                            if (parameter.shaderHandle.Variant != null)
                            {
                                if (parameter.intValue <= 0)
                                {
                                    DisableShaderKeyword(parameter.shaderHandle.Variant);
                                }
                                else
                                {
                                    EnableShaderKeyword(parameter.shaderHandle.Variant);
                                }
                            }
                        };

                        break;

                    case MaterialParameterType.Vector2:

                        applyPropertiesCallbacks[counter++] = () =>
                        {
                            shader.SetVector4(parameter.shaderHandle, new Vector4(parameter.vector2Value, 0, 0));
                        };

                        break;

                    case MaterialParameterType.Vector3:

                        applyPropertiesCallbacks[counter++] = () =>
                        {
                            shader.SetVector4(parameter.shaderHandle, new Vector4(parameter.vector3Value, 0));
                        };

                        break;

                    case MaterialParameterType.Vector4:

                        applyPropertiesCallbacks[counter++] = () =>
                        {
                            shader.SetVector4(parameter.shaderHandle, parameter.vector4Value);
                        };

                        break;

                    case MaterialParameterType.Color:

                        applyPropertiesCallbacks[counter++] = () =>
                        {
                            shader.SetColor(parameter.shaderHandle, parameter.colorValue);
                        };

                        break;
                }
            }
        }

        var l = applyPropertiesCallbacks.Length;

        for(var i = 0; i < l; i++)
        {
            applyPropertiesCallbacks[i]?.Invoke();
        }
    }
}
