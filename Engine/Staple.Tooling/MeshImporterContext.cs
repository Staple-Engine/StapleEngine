using Staple.Internal;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Staple.Tooling;

public class MeshImporterContext
{
    public delegate bool ShaderHasParameterCallback(string name);
    public delegate string ResolveTexturePathCallback(string path, string meshFileName);

    public MeshAssetMetadata metadata;
    public string meshFileName;
    public string inputPath;
    public SerializableShader standardShader;
    public ShaderHasParameterCallback shaderHasParameter;
    public Lock materialLock;
    public Dictionary<string, string> processedTextures;
    public ResolveTexturePathCallback resolveTexturePath;

    /// <summary>
    /// Fills missing material parameters for a shader
    /// </summary>
    /// <param name="metadata">The metadata for the material</param>
    /// <param name="shader">The shader</param>
    public static void FillMaterialParameters(MaterialMetadata metadata, SerializableShader shader)
    {
        if (shader == null)
        {
            return;
        }

        foreach (var parameter in shader.metadata.uniforms)
        {
            if (metadata.parameters.ContainsKey(parameter.name))
            {
                continue;
            }

            switch (parameter.type)
            {
                case ShaderUniformType.Int:

                    if (!string.IsNullOrEmpty(parameter.defaultValue) &&
                        int.TryParse(parameter.defaultValue, CultureInfo.InvariantCulture, out var intValue))
                    {
                        metadata.parameters.Add(parameter.name, new()
                        {
                            type = MaterialParameterType.Int,
                            source = MaterialParameterSource.Uniform,
                            intValue = intValue,
                        });
                    }
                    else
                    {
                        metadata.parameters.Add(parameter.name, new()
                        {
                            type = MaterialParameterType.Int,
                            source = MaterialParameterSource.Uniform,
                            intValue = 0,
                        });
                    }

                    break;

                case ShaderUniformType.Float:

                    if (!string.IsNullOrEmpty(parameter.defaultValue) &&
                        float.TryParse(parameter.defaultValue, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        metadata.parameters.Add(parameter.name, new()
                        {
                            type = MaterialParameterType.Float,
                            source = MaterialParameterSource.Uniform,
                            floatValue = floatValue,
                        });
                    }
                    else
                    {
                        metadata.parameters.Add(parameter.name, new()
                        {
                            type = MaterialParameterType.Float,
                            source = MaterialParameterSource.Uniform,
                            floatValue = 0,
                        });
                    }

                    break;

                case ShaderUniformType.Color:

                    if (!string.IsNullOrEmpty(parameter.defaultValue))
                    {
                        metadata.parameters.Add(parameter.name, new()
                        {
                            type = MaterialParameterType.Color,
                            source = MaterialParameterSource.Uniform,
                            colorValue = new Color(parameter.defaultValue),
                        });
                    }
                    else
                    {
                        metadata.parameters.Add(parameter.name, new()
                        {
                            type = MaterialParameterType.Color,
                            source = MaterialParameterSource.Uniform,
                            colorValue = Color.White,
                        });
                    }

                    break;

                case ShaderUniformType.Vector2:

                    //TODO: DefaultValue
                    metadata.parameters.Add(parameter.name, new()
                    {
                        type = MaterialParameterType.Vector2,
                        source = MaterialParameterSource.Uniform,
                    });

                    break;

                case ShaderUniformType.Vector3:

                    //TODO: DefaultValue
                    metadata.parameters.Add(parameter.name, new()
                    {
                        type = MaterialParameterType.Vector3,
                        source = MaterialParameterSource.Uniform,
                    });

                    break;

                case ShaderUniformType.Vector4:

                    //TODO: DefaultValue
                    metadata.parameters.Add(parameter.name, new()
                    {
                        type = MaterialParameterType.Vector4,
                        source = MaterialParameterSource.Uniform,
                    });

                    break;
            }
        }
    }
}
