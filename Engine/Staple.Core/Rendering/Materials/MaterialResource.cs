using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

internal class MaterialResource
{
    internal Shader shader;
    internal MaterialMetadata metadata;

    internal readonly Dictionary<MaterialResourceParameter, int> vertexTextureBindings = [];
    internal readonly Dictionary<MaterialResourceParameter, int> fragmentTextureBindings = [];

    internal Texture[] vertexSamplers;

    internal Texture[] fragmentSamplers;

    internal Dictionary<StringID, MaterialResourceParameter> parameters = [];

    internal Dictionary<StringID, MaterialResourceParameter> instanceParameters = [];

    public GuidHasher Guid = new();

    public MaterialResource Clone()
    {
        var outValue = new MaterialResource()
        {
            shader = shader,
            metadata = metadata,
            vertexSamplers = MemoryUtils.SafeCloneArray(vertexSamplers),
            fragmentSamplers = MemoryUtils.SafeCloneArray(fragmentSamplers),
        };

        outValue.Guid.Guid = Guid.Guid;

        foreach(var pair in vertexTextureBindings)
        {
            outValue.vertexTextureBindings.Add(pair.Key, pair.Value);
        }

        foreach (var pair in fragmentTextureBindings)
        {
            outValue.fragmentTextureBindings.Add(pair.Key, pair.Value);
        }

        foreach (var parameter in parameters)
        {
            outValue.parameters.Add(parameter.Key, parameter.Value.Clone());
        }

        foreach (var parameter in instanceParameters)
        {
            outValue.instanceParameters.Add(parameter.Key, parameter.Value.Clone());
        }

        return outValue;
    }

    public void SetColor(string name, Color value, MaterialParameterSource source, StringID shaderVariantKey)
    {
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
                    shaderHandle = shader.GetUniformHandle(name, shaderVariantKey),
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

    public void SetInt(string name, int value, MaterialParameterSource source, StringID shaderVariantKey,
        Action<string> enableShaderKeyword, Action<string> disableShaderKeyword)
    {
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
                    disableShaderKeyword(parameter.shaderHandle.Variant);
                }
                else
                {
                    enableShaderKeyword(parameter.shaderHandle.Variant);
                }
            }
            else
            {
                parameter = new MaterialResourceParameter()
                {
                    name = name,
                    type = MaterialParameterType.Int,
                    intValue = value,
                    shaderHandle = shader.GetUniformHandle(name, shaderVariantKey),
                };

                parameters.AddOrSetKey(name, parameter);

                if (parameter.shaderHandle.Variant == null)
                {
                    return;
                }

                if (parameter.intValue <= 0)
                {
                    disableShaderKeyword(parameter.shaderHandle.Variant);
                }
                else
                {
                    enableShaderKeyword(parameter.shaderHandle.Variant);
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

    public void SetFloat(string name, float value, MaterialParameterSource source, StringID shaderVariantKey,
        Action<string> enableShaderKeyword, Action<string> disableShaderKeyword)
    {
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
                    disableShaderKeyword(parameter.shaderHandle.Variant);
                }
                else
                {
                    enableShaderKeyword(parameter.shaderHandle.Variant);
                }
            }
            else
            {
                parameter = new MaterialResourceParameter()
                {
                    name = name,
                    type = MaterialParameterType.Float,
                    floatValue = value,
                    shaderHandle = shader.GetUniformHandle(name, shaderVariantKey),
                };

                parameters.AddOrSetKey(name, parameter);

                if (parameter.shaderHandle.Variant == null)
                {
                    return;
                }

                if (parameter.floatValue <= 0)
                {
                    disableShaderKeyword(parameter.shaderHandle.Variant);
                }
                else
                {
                    enableShaderKeyword(parameter.shaderHandle.Variant);
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

    public void SetVector2(string name, Vector2 value, MaterialParameterSource source, StringID shaderVariantKey)
    {
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
                    shaderHandle = shader.GetUniformHandle(name, shaderVariantKey),
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

    public void SetVector3(string name, Vector3 value, MaterialParameterSource source, StringID shaderVariantKey)
    {
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
                    shaderHandle = shader.GetUniformHandle(name, shaderVariantKey),
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
    public void SetVector4(string name, Vector4 value, MaterialParameterSource source, StringID shaderVariantKey)
    {
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
                    shaderHandle = shader.GetUniformHandle(name, shaderVariantKey),
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

    public void SetTexture(string name, Texture value, StringID shaderVariantKey,
        Action<string> enableShaderKeyword, Action<string> disableShaderKeyword, Texture whiteTexture)
    {
        if (parameters.TryGetValue(name, out var parameter))
        {
            if (parameter.type != MaterialParameterType.Texture)
            {
                return;
            }

            if (value == null)
            {
                if (!string.IsNullOrEmpty(parameter.shaderHandle.DefaultValue))
                {
                    if (parameter.shaderHandle.DefaultValue == whiteTexture.Guid.Guid)
                    {
                        value = whiteTexture;
                    }
                    else
                    {
                        value = ResourceManager.instance.LoadTexture(parameter.shaderHandle.DefaultValue);
                    }
                }
                else
                {
                    value = whiteTexture;
                }
            }

            parameter.textureValue = value;
            parameter.hasTexture = value != null;
        }
        else
        {
            var original = value;

            value ??= whiteTexture;

            parameter = new MaterialResourceParameter()
            {
                name = name,
                type = MaterialParameterType.Texture,
                textureValue = value,
                hasTexture = value != null,
                shaderHandle = shader.GetUniformHandle(name, shaderVariantKey),
            };

            parameters.AddOrSetKey(name, parameter);

            if (parameter.shaderHandle.Variant == null)
            {
                return;
            }

            //Textures are replaced with White by default - Ensure we only enable variants if it's not White
            if (parameter.hasTexture && (original != null || value.Guid.Guid != "WHITE"))
            {
                enableShaderKeyword(parameter.shaderHandle.Variant);
            }
            else
            {
                disableShaderKeyword(parameter.shaderHandle.Variant);
            }
        }
    }

    public void SetMatrix3x3(string name, Matrix3x3 value, MaterialParameterSource source, StringID shaderVariantKey)
    {
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
                    shaderHandle = shader.GetUniformHandle(name, shaderVariantKey),
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

    public void SetMatrix4x4(string name, Matrix4x4 value, MaterialParameterSource source, StringID shaderVariantKey)
    {
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
                    shaderHandle = shader.GetUniformHandle(name, shaderVariantKey),
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
}
