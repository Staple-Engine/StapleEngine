﻿using Bgfx;
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

    private static Texture whiteTexture;

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
    internal string guid;
    internal MaterialMetadata metadata;

    internal Dictionary<int, ParameterInfo> parameters = [];

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

                hasMainColor = mainColorHandle != null;
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

                hasMainTexture = mainTextureHandle != null;
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
    public IEnumerable<string> Keywords => shader.metadata?.variants ?? Enumerable.Empty<string>();

    /// <summary>
    /// Gets the current shader program, if valid.
    /// </summary>
    internal bgfx.ProgramHandle ShaderProgram => shader != null &&
        shader.Disposed == false &&
        shader.instances.TryGetValue(ShaderVariantKey, out var instance) ?
        instance.program : new()
        {
            idx = ushort.MaxValue,
        };

    public Material()
    {
    }

    public Material(Material sourceMaterial)
    {
        foreach (var parameter in sourceMaterial.parameters)
        {
            parameters.AddOrSetKey(parameter.Key, parameter.Value.Clone());
        }

        metadata = sourceMaterial.metadata;
        guid = sourceMaterial.guid;
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

    /// <summary>
    /// Sets a Texture property's value
    /// </summary>
    /// <param name="name">Property name</param>
    /// <param name="value">The Texture value</param>
    public void SetTexture(string name, Texture value)
    {
        var hash = name.GetHashCode();

        value = value ?? WhiteTexture;

        if (parameters.TryGetValue(hash, out var parameter))
        {
            if (parameter.type == MaterialParameterType.Texture)
            {
                parameter.textureValue = value;
                parameter.hasTexture = value != null;
            }
        }
        else
        {
            parameters.AddOrSetKey(hash, new ParameterInfo()
            {
                name = name,
                type = MaterialParameterType.Texture,
                textureValue = value,
                hasTexture = value != null,
                shaderHandle = shader.GetUniformHandle(hash),
                relatedShaderHandles = [
                    shader.GetUniformHandle($"{name}Set".GetHashCode()),
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

        if(shaderHandle != null)
        {
            shaderHandles.Add(hash, shaderHandle);
        }

        return shaderHandle;
    }

    /// <summary>
    /// Applies the default properties of this material to the shader
    /// </summary>
    /// <param name="applyMode">How to apply the properties</param>
    internal void ApplyProperties(ApplyMode applyMode)
    {
        if(shader == null)
        {
            return;
        }

        if(hasMainTexture && applyMode != ApplyMode.IgnoreTextures)
        {
            var t = mainTexture;

            if (t?.Disposed ?? true)
            {
                MainTexture = WhiteTexture;
            }

            shader.SetTexture(mainTextureHandle, mainTexture);
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
                    case MaterialParameterType.Texture:

                        if(applyMode == ApplyMode.IgnoreTextures)
                        {
                            continue;
                        }

                        applyPropertiesCallbacks[counter++] = () =>
                        {
                            if (parameter.hasTexture == false)
                            {
                                return;
                            }

                            var overrideFlags = (TextureFlags)uint.MaxValue;

                            var texture = parameter.textureValue;

                            overrideFlags = 0;

                            Texture.ProcessFlags(ref overrideFlags, texture.metadata, true);

                            if (parameter.relatedParameters.Length == 0)
                            {
                                parameter.relatedParameters =
                                [
                                    parameters.TryGetValue($"{key}_UMapping".GetHashCode(), out var p) &&
                                        p.type == MaterialParameterType.TextureWrap ? p : null,

                                    parameters.TryGetValue($"{key}_VMapping".GetHashCode(), out p) &&
                                        p.type == MaterialParameterType.TextureWrap ? p : null,
                                ];
                            }

                            if (parameter.relatedParameters[0] != null &&
                                parameter.relatedParameters[1] != null)
                            {

                                overrideFlags |=
                                    parameter.relatedParameters[0].textureWrapValue switch
                                    {
                                        TextureWrap.Mirror => TextureFlags.SamplerUMirror,
                                        TextureWrap.Repeat => 0,
                                        _ => TextureFlags.SamplerUClamp,
                                    };

                                overrideFlags |=
                                    parameter.relatedParameters[1].textureWrapValue switch
                                    {
                                        TextureWrap.Mirror => TextureFlags.SamplerVMirror,
                                        TextureWrap.Repeat => 0,
                                        _ => TextureFlags.SamplerVClamp,
                                    };
                            }

                            shader.SetTexture(parameter.shaderHandle, texture, overrideFlags);
                        };

                        break;

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
