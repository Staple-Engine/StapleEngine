using Bgfx;
using Staple.Internal;
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
        public object value;
        public ShaderHandle shaderHandle;
        public ShaderHandle[] relatedShaderHandles = [];
        public ParameterInfo[] relatedParameters = [];

        public ParameterInfo Clone()
        {
            return new()
            {
                name = name,
                type = type,
                value = value,
                shaderHandle = shaderHandle,
                relatedShaderHandles = relatedShaderHandles,
                relatedParameters = relatedParameters
                    .Where(x => x != null)
                    .Select(x => x.Clone())
                    .ToArray(),
            };
        }
    }

    internal const string MainColorProperty = "mainColor";
    internal const string MainTextureProperty = "mainTexture";

    internal static readonly int MainColorPropertyHash = MainColorProperty.GetHashCode();
    internal static readonly int MainTexturePropertyHash = MainTextureProperty.GetHashCode();

    internal static Texture WhiteTexture;

    internal Shader shader;
    internal string guid;
    internal MaterialMetadata metadata;

    internal Dictionary<int, ParameterInfo> parameters = [];

    internal Dictionary<int, ShaderHandle> shaderHandles = [];

    internal HashSet<int> shaderKeywords = [];

    private readonly Dictionary<int, string> cachedShaderVariantKeys = [];

    internal string ShaderVariantKey { get; private set; } = "";

    /// <summary>
    /// The material's main color
    /// </summary>
    public Color MainColor
    {
        get => parameters.TryGetValue(MainColorPropertyHash, out var parameter) ? (Color)parameter.value : Color.White;

        set
        {
            if(parameters.TryGetValue(MainColorPropertyHash, out var parameter) && parameter != null && parameter.type == MaterialParameterType.Color)
            {
                parameter.value = value;
            }
            else
            {
                parameters.AddOrSetKey(MainColorPropertyHash, new ParameterInfo()
                {
                    name = MainColorProperty,
                    type = MaterialParameterType.Color,
                    value = value,
                    shaderHandle = shader?.GetUniformHandle(MainColorPropertyHash),
                });
            }
        }
    }

    /// <summary>
    /// The material's main texture
    /// </summary>
    public Texture MainTexture
    {
        get => parameters.TryGetValue(MainTexturePropertyHash, out var parameter) ? (Texture)parameter.value : null;

        set
        {
            if (parameters.TryGetValue(MainTexturePropertyHash, out var parameter) && parameter != null && parameter.type == MaterialParameterType.Texture)
            {
                parameter.value = value;
            }
            else
            {
                parameters.AddOrSetKey(MainTexturePropertyHash, new ParameterInfo()
                {
                    name = MainTextureProperty,
                    type = MaterialParameterType.Texture,
                    value = value,
                    shaderHandle = shader?.GetUniformHandle(MainTexturePropertyHash),
                });
            }
        }
    }

    /// <summary>
    /// Culling mode for this material
    /// </summary>
    public CullingMode CullingMode { get; set; } = CullingMode.Back;

    internal bgfx.StateFlags CullingFlag
    {
        get
        {
            return CullingMode switch
            {
                Staple.CullingMode.Back => bgfx.StateFlags.CullCcw,
                Staple.CullingMode.Front => bgfx.StateFlags.CullCw,
                _ => 0,
            };
        }
    }

    /// <summary>
    /// The asset's guid (if any)
    /// </summary>
    public string Guid
    {
        get => guid;

        set => guid = value;
    }

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
    public IEnumerable<string> Keywords => shader?.metadata?.variants ?? Enumerable.Empty<string>();

    /// <summary>
    /// Gets the current shader program, if valid.
    /// </summary>
    public bgfx.ProgramHandle ShaderProgram => shader != null &&
        shader.Disposed == false &&
        shader.instances.TryGetValue(ShaderVariantKey, out var instance) ?
        instance.program : new()
        {
            idx = ushort.MaxValue,
        };

    public Material()
    {
        SetColor(MainColorProperty, Color.White);
    }

    public Material(Material sourceMaterial)
    {
        metadata = sourceMaterial.metadata;
        guid = sourceMaterial.guid;
        shader = sourceMaterial.shader;
        shaderHandles = new(sourceMaterial.shaderHandles);
        CullingMode = sourceMaterial.CullingMode;

        foreach (var parameter in sourceMaterial.parameters)
        {
            parameters.AddOrSetKey(parameter.Key, parameter.Value.Clone());
        }
    }

    ~Material()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys this material's resources.
    /// </summary>
    internal void Destroy()
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

        shaderKeywords.Add(name.GetHashCode());

        UpdateVariantKey();
    }

    /// <summary>
    /// Disables a shader keyword
    /// </summary>
    /// <param name="name">The keyword</param>
    public void DisableShaderKeyword(string name)
    {
        shaderKeywords.Remove(name.GetHashCode());

        UpdateVariantKey();
    }

    /// <summary>
    /// Sets a color property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The color value</param>
    public void SetColor(string name, Color value)
    {
        var hash = name.GetHashCode();

        if(parameters.TryGetValue(hash, out var parameter))
        {
            if(parameter.type == MaterialParameterType.Color)
            {
                parameter.value = value;
            }
        }
        else
        {
            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                type = MaterialParameterType.Color,
                value = value,
                shaderHandle = shader?.GetUniformHandle(hash),
            });
        }
    }

    /// <summary>
    /// Sets a float property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The float value</param>
    public void SetFloat(string name, float value)
    {
        var hash = name.GetHashCode();

        if (parameters.TryGetValue(hash, out var parameter))
        {
            if (parameter.type == MaterialParameterType.Float)
            {
                parameter.value = value;
            }
        }
        else
        {
            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Float,
                value = value,
                shaderHandle = shader?.GetUniformHandle(hash),
            });
        }
    }

    /// <summary>
    /// Sets a Vector2 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Vector2 value</param>
    public void SetVector2(string name, Vector2 value)
    {
        var hash = name.GetHashCode();

        if (parameters.TryGetValue(hash, out var parameter))
        {
            if (parameter.type == MaterialParameterType.Vector2)
            {
                parameter.value = value;
            }
        }
        else
        {
            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Vector2,
                value = value,
                shaderHandle = shader?.GetUniformHandle(hash),
            });
        }
    }

    /// <summary>
    /// Sets a Vector3 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Vector3 value</param>
    public void SetVector3(string name, Vector3 value)
    {
        var hash = name.GetHashCode();

        if (parameters.TryGetValue(hash, out var parameter))
        {
            if (parameter.type == MaterialParameterType.Vector3)
            {
                parameter.value = value;
            }
        }
        else
        {
            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Vector3,
                value = value,
                shaderHandle = shader?.GetUniformHandle(hash),
            });
        }
    }

    /// <summary>
    /// Sets a Vector4 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Vector4 value</param>
    public void SetVector4(string name, Vector4 value)
    {
        var hash = name.GetHashCode();

        if (parameters.TryGetValue(hash, out var parameter))
        {
            if (parameter.type == MaterialParameterType.Vector4)
            {
                parameter.value = value;
            }
        }
        else
        {
            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Vector4,
                value = value,
                shaderHandle = shader?.GetUniformHandle(hash),
            });
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

        if (parameters.TryGetValue(hash, out var parameter))
        {
            if (parameter.type == MaterialParameterType.Texture)
            {
                parameter.value = value;
            }
        }
        else
        {
            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Texture,
                value = value,
                shaderHandle = shader?.GetUniformHandle(hash),
                relatedShaderHandles = [
                    shader?.GetUniformHandle($"{name}Set".GetHashCode()),
                ]
            });
        }
    }

    /// <summary>
    /// Sets a Matrix3x3 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Matrix3x3 value</param>
    public void SetMatrix3x3(string name, Matrix3x3 value)
    {
        var hash = name.GetHashCode();

        if (parameters.TryGetValue(hash, out var parameter))
        {
            if (parameter.type == MaterialParameterType.Matrix3x3)
            {
                parameter.value = value;
            }
        }
        else
        {
            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Matrix3x3,
                value = value,
                shaderHandle = shader?.GetUniformHandle(hash),
            });
        }
    }

    /// <summary>
    /// Sets a Matrix4x4 property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Matrix4x4 value</param>
    public void SetMatrix4x4(string name, Matrix4x4 value)
    {
        var hash = name.GetHashCode();

        if (parameters.TryGetValue(hash, out var parameter))
        {
            if (parameter.type == MaterialParameterType.Matrix4x4)
            {
                parameter.value = value;
            }
        }
        else
        {
            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Matrix4x4,
                value = value,
                shaderHandle = shader?.GetUniformHandle(hash),
            });
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

        shaderHandle = shader?.GetUniformHandle(hash);

        if(shaderHandle != null)
        {
            shaderHandles.Add(hash, shaderHandle);
        }

        return shaderHandle;
    }

    /// <summary>
    /// Applies the default properties of this material to the shader
    /// </summary>
    internal void ApplyProperties()
    {
        var t = MainTexture;

        if(t == null || t.Disposed)
        {
            if(WhiteTexture == null)
            {
                var pixels = Enumerable.Repeat((byte)255, 64 * 64 * 4).ToArray();

                WhiteTexture = Texture.CreatePixels("WHITE", pixels, 64, 64, new TextureMetadata()
                {
                    filter = TextureFilter.Linear,
                    format = TextureMetadataFormat.RGBA8,
                    type = TextureType.Texture,
                    useMipmaps = false,
                }, Bgfx.bgfx.TextureFormat.RGBA8);
            }

            SetTexture(MainTextureProperty, WhiteTexture);
        }

        foreach(var parameter in parameters.Values)
        {
            var key = parameter.name;

            switch (parameter.type)
            {
                case MaterialParameterType.Texture:

                    {
                        shader?.SetFloat(parameter.relatedShaderHandles[0], parameter.value == null ? 0 : 1);

                        var texture = (Texture)parameter.value;

                        var overrideFlags = (TextureFlags)uint.MaxValue;

                        if(texture != null)
                        {
                            overrideFlags = 0;

                            Texture.ProcessFlags(ref overrideFlags, texture.metadata, true);

                            if(parameter.relatedParameters.Length == 0)
                            {
                                parameter.relatedParameters =
                                [
                                    parameters.TryGetValue($"{key}_UMapping".GetHashCode(), out var p) &&
                                        p.type == MaterialParameterType.TextureWrap &&
                                        p.value is TextureWrap ? p : null,

                                    parameters.TryGetValue($"{key}_VMapping".GetHashCode(), out p) &&
                                        p.type == MaterialParameterType.TextureWrap &&
                                        p.value is TextureWrap ? p : null,
                                ];
                            }

                            if (parameter.relatedParameters[0]?.value is TextureWrap UWrap &&
                                parameter.relatedParameters[1]?.value is TextureWrap VWrap)
                            {
                                overrideFlags |=
                                    UWrap switch
                                    {
                                        TextureWrap.Mirror => TextureFlags.SamplerUMirror,
                                        TextureWrap.Repeat => 0,
                                        _ => TextureFlags.SamplerUClamp,
                                    };
                                
                                overrideFlags |=
                                    VWrap switch
                                    {
                                        TextureWrap.Mirror => TextureFlags.SamplerUMirror,
                                        TextureWrap.Repeat => 0,
                                        _ => TextureFlags.SamplerUClamp,
                                    };
                            }
                        }

                        shader?.SetTexture(parameter.shaderHandle, texture, overrideFlags);
                    }

                    break;

                case MaterialParameterType.Matrix3x3:

                    shader?.SetMatrix3x3(parameter.shaderHandle, (Matrix3x3)parameter.value);

                    break;

                case MaterialParameterType.Matrix4x4:

                    shader?.SetMatrix4x4(parameter.shaderHandle, (Matrix4x4)parameter.value);

                    break;

                case MaterialParameterType.Float:

                    shader?.SetVector4(parameter.shaderHandle, new Vector4((float)parameter.value, 0, 0, 0));

                    break;

                case MaterialParameterType.Vector2:

                    shader?.SetVector4(parameter.shaderHandle, new Vector4((Vector2)parameter.value, 0, 0));

                    break;

                case MaterialParameterType.Vector3:

                    shader?.SetVector4(parameter.shaderHandle, new Vector4((Vector3)parameter.value, 0));

                    break;

                case MaterialParameterType.Vector4:

                    shader?.SetVector4(parameter.shaderHandle, (Vector4)parameter.value);

                    break;

                case MaterialParameterType.Color:

                    shader?.SetColor(parameter.shaderHandle, (Color)parameter.value);

                    break;
            }
        }
    }
}
