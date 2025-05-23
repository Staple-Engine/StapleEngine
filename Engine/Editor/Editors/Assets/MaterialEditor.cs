﻿using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Editor;

[CustomEditor(typeof(MaterialMetadata))]
internal class MaterialEditor : AssetEditor
{
    private readonly Dictionary<string, Shader> cachedShaders = [];
    private readonly Dictionary<string, Texture> cachedTextures = [];
    private Shader activeShader;
    private bool needsShaderUpdate = true;

    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var material = target as MaterialMetadata;

        var shaderPath = material.shader;
        Shader shader = null;

        if (shaderPath != null)
        {
            if (cachedShaders.TryGetValue(shaderPath, out shader) == false)
            {
                if (shaderPath.Length > 0)
                {
                    var guid = AssetDatabase.GetAssetGuid(shaderPath, ResourceManager.ShaderPrefix);

                    if (guid != null)
                    {
                        shaderPath = guid;
                    }

                    shader = ResourceManager.instance.LoadShader(shaderPath);

                    cachedShaders.AddOrSetKey(shaderPath, shader);

                    if (shader != null)
                    {
                        material.shader = shader.Guid.Guid;
                    }

                    if(activeShader != shader)
                    {
                        needsShaderUpdate = true;
                    }

                    activeShader = shader;
                }
            }
        }

        if(activeShader != null && needsShaderUpdate)
        {
            needsShaderUpdate = false;

            foreach(var uniform in activeShader.metadata.uniforms)
            {
                var isValidType = uniform.type switch
                {
                     ShaderUniformType.ReadOnlyBuffer or
                     ShaderUniformType.ReadWriteBuffer or
                     ShaderUniformType.WriteOnlyBuffer or
                     ShaderUniformType.Matrix3x3 or
                     ShaderUniformType.Matrix4x4 => false,
                     _ => true,
                };

                if(isValidType == false)
                {
                    continue;
                }

                if(material.parameters.TryGetValue(uniform.name, out var parameter) == false)
                {
                    parameter = new()
                    {
                        source = MaterialParameterSource.Uniform,
                    };

                    material.parameters.Add(uniform.name, parameter);
                }

                parameter.type = uniform.type switch
                {
                    ShaderUniformType.Vector2 => MaterialParameterType.Vector2,
                    ShaderUniformType.Vector3 => MaterialParameterType.Vector3,
                    ShaderUniformType.Vector4 => MaterialParameterType.Vector4,
                    ShaderUniformType.Texture => MaterialParameterType.Texture,
                    ShaderUniformType.Color => MaterialParameterType.Color,
                    ShaderUniformType.Int => MaterialParameterType.Int,
                    ShaderUniformType.Float => MaterialParameterType.Float,
                    _ => throw new ArgumentOutOfRangeException($"Material Autocomplete: Uniform type for {uniform.name}")
                };
            }
        }

        switch (name)
        {
            case nameof(MaterialMetadata.parameters):

                foreach (var parameter in material.parameters)
                {
                    var label = parameter.Key.ExpandCamelCaseName();

                    switch (parameter.Value.type)
                    {
                        case MaterialParameterType.Texture:

                            {
                                var key = parameter.Value.textureValue ?? "";

                                if(key.Length > 0)
                                {
                                    var guid = AssetDatabase.GetAssetGuid(key);

                                    if(guid != null)
                                    {
                                        key = parameter.Value.textureValue = guid;
                                    }
                                }

                                if(cachedTextures.ContainsKey(key) == false && key.Length > 0)
                                {
                                    var t = ResourceManager.instance.LoadTexture(key);

                                    if (t != null)
                                    {
                                        cachedTextures.AddOrSetKey(key, t);
                                    }
                                }

                                cachedTextures.TryGetValue(key, out var texture);

                                var newValue = EditorGUI.ObjectPicker(typeof(Texture), label, texture, $"{material.guid}.{key}.Picker");

                                if(newValue != texture)
                                {
                                    if(newValue is Texture t)
                                    {
                                        var guid = t.Guid?.Guid;

                                        if(guid != null)
                                        {
                                            parameter.Value.textureValue = guid;

                                            cachedTextures.AddOrSetKey(guid, t);
                                        }
                                    }
                                    else
                                    {
                                        parameter.Value.textureValue = null;
                                    }
                                }
                            }

                            break;

                        case MaterialParameterType.Vector2:

                            {
                                if (parameter.Value.vec2Value == null)
                                {
                                    parameter.Value.vec2Value = new();
                                }

                                var current = parameter.Value.vec2Value.ToVector2();

                                var newValue = EditorGUI.Vector2Field(label, parameter.Key, current);

                                if (newValue != current)
                                {
                                    parameter.Value.vec2Value.x = newValue.X;
                                    parameter.Value.vec2Value.y = newValue.Y;
                                }
                            }

                            break;

                        case MaterialParameterType.Vector3:

                            {
                                if (parameter.Value.vec3Value == null)
                                {
                                    parameter.Value.vec3Value = new();
                                }

                                var current = parameter.Value.vec3Value.ToVector3();

                                var newValue = EditorGUI.Vector3Field(label, parameter.Key, current);

                                if (newValue != current)
                                {
                                    parameter.Value.vec3Value.x = newValue.X;
                                    parameter.Value.vec3Value.y = newValue.Y;
                                    parameter.Value.vec3Value.z = newValue.Z;
                                }
                            }

                            break;

                        case MaterialParameterType.Vector4:

                            {
                                if (parameter.Value.vec4Value == null)
                                {
                                    parameter.Value.vec4Value = new();
                                }

                                var current = parameter.Value.vec4Value.ToVector4();

                                var newValue = EditorGUI.Vector4Field(label, parameter.Key, current);

                                if (newValue != current)
                                {
                                    parameter.Value.vec4Value.x = newValue.X;
                                    parameter.Value.vec4Value.y = newValue.Y;
                                    parameter.Value.vec4Value.z = newValue.Z;
                                    parameter.Value.vec4Value.w = newValue.W;
                                }
                            }

                            break;

                        case MaterialParameterType.Color:

                            parameter.Value.colorValue = EditorGUI.ColorField(label, parameter.Key, parameter.Value.colorValue);

                            break;

                        case MaterialParameterType.Float:

                            parameter.Value.floatValue = EditorGUI.FloatField(label, parameter.Key, parameter.Value.floatValue);

                            break;

                        case MaterialParameterType.Int:

                            parameter.Value.intValue = EditorGUI.IntField(label, parameter.Key, parameter.Value.intValue);

                            break;

                        case MaterialParameterType.TextureWrap:

                            parameter.Value.textureWrapValue = EditorGUI.EnumDropdown(label, parameter.Key, parameter.Value.textureWrapValue);

                            break;
                    }
                }

                return true;

            case nameof(MaterialMetadata.shader):

                {
                    var newValue = EditorGUI.ObjectPicker(typeof(Shader), "Shader: ", shader, $"{material.guid}.{shaderPath}.Picker");

                    if (newValue != shader)
                    {
                        if (newValue is Shader s)
                        {
                            cachedShaders.AddOrSetKey(s.metadata.guid, s);

                            material.shader = s.Guid.Guid;

                            material.parameters = [];

                            static MaterialParameterType ParameterType(ShaderUniformType type)
                            {
                                return type switch
                                {
                                    ShaderUniformType.Texture => MaterialParameterType.Texture,
                                    ShaderUniformType.Int => MaterialParameterType.Int,
                                    ShaderUniformType.Float => MaterialParameterType.Float,
                                    ShaderUniformType.Vector2 => MaterialParameterType.Vector2,
                                    ShaderUniformType.Vector3 => MaterialParameterType.Vector3,
                                    ShaderUniformType.Vector4 => MaterialParameterType.Vector4,
                                    ShaderUniformType.Color => MaterialParameterType.Color,
                                    /*
                                    ShaderUniformType.Matrix3x3 => MaterialParameterType.Matrix3x3,
                                    ShaderUniformType.Matrix4x4 => MaterialParameterType.Matrix4x4,
                                    */
                                    _ => (MaterialParameterType)(-1),
                                };
                            }

                            foreach(var parameter in s.metadata.uniforms)
                            {
                                if(parameter.type == ShaderUniformType.Float &&
                                    parameter.name.EndsWith("Set") && s.metadata.uniforms.Any(x => x.type == ShaderUniformType.Texture &&
                                    x.name == parameter.name[..^"Set".Length]))
                                {
                                    continue;
                                }

                                var type = ParameterType(parameter.type);

                                if(type == (MaterialParameterType)(-1))
                                {
                                    continue;
                                }

                                var p = new MaterialParameter()
                                {
                                    type = type,
                                    source = MaterialParameterSource.Uniform,
                                };

                                material.parameters.Add(parameter.name, p);
                            }

                            foreach (var parameter in s.metadata.instanceParameters)
                            {
                                if (parameter.type == ShaderUniformType.Float &&
                                    parameter.name.EndsWith("Set") && s.metadata.uniforms.Any(x => x.type == ShaderUniformType.Texture &&
                                    x.name == parameter.name[..^"Set".Length]))
                                {
                                    continue;
                                }

                                var type = ParameterType(parameter.type);

                                if (type == (MaterialParameterType)(-1))
                                {
                                    continue;
                                }

                                var p = new MaterialParameter()
                                {
                                    type = type,
                                    source = MaterialParameterSource.Instance,
                                };

                                material.parameters.Add(parameter.name, p);
                            }

                            activeShader = s;
                        }
                        else
                        {
                            material.shader = "";

                            activeShader = null;
                        }
                    }
                }

                return true;

            case nameof(MaterialMetadata.enabledShaderVariants):

                if(activeShader != null && activeShader.metadata.variants.Count > 0)
                {
                    EditorGUI.Label("Enabled Variants");

                    EditorGUI.SameLine();

                    EditorGUI.Button("+", "MaterialVariantAdd", () =>
                    {
                        material.enabledShaderVariants.Add("");
                    });

                    var skip = false;

                    for(var i = 0; i < material.enabledShaderVariants.Count; i++)
                    {
                        var currentIndex = activeShader.metadata.variants.IndexOf(material.enabledShaderVariants[i]);
                        var index = EditorGUI.Dropdown("", $"MaterialVariant{i}", activeShader.metadata.variants.ToArray(), currentIndex);

                        if(currentIndex != index && index >= 0)
                        {
                            material.enabledShaderVariants[i] = activeShader.metadata.variants[index];
                        }

                        EditorGUI.SameLine();

                        EditorGUI.Button("-", $"MaterialVariantRemove{i}", () =>
                        {
                            skip = true;

                            material.enabledShaderVariants.RemoveAt(i);
                        });

                        if(skip)
                        {
                            break;
                        }
                    }
                }

                return true;
        }

        return false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ShowAssetUI(null);
    }
}
