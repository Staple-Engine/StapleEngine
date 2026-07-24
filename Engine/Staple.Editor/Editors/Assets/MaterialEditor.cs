using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Staple.Editor;

[CustomEditor(typeof(MaterialMetadata))]
internal class MaterialEditor : AssetEditor
{
    private readonly Dictionary<string, Shader> cachedShaders = [];
    private readonly Dictionary<string, Texture> cachedTextures = [];
    private Shader activeShader;
    private bool needsShaderUpdate = true;

    private static void InitializeMaterialParameter(MaterialParameter parameter, ShaderUniform uniform)
    {
        if (string.IsNullOrEmpty(uniform.defaultValue))
        {
            return;
        }

        switch (uniform.type)
        {
            case ShaderUniformType.Texture:

                parameter.textureValue = uniform.defaultValue;

                break;

            case ShaderUniformType.Int:

                if (int.TryParse(uniform.defaultValue, out var intValue))
                {
                    parameter.intValue = intValue;
                }

                break;

            case ShaderUniformType.Float:

                if (float.TryParse(uniform.defaultValue, out var floatValue))
                {
                    parameter.floatValue = floatValue;
                }

                break;

            case ShaderUniformType.Color:

                parameter.colorValue = Color32.TryParse(uniform.defaultValue, out var colorValue) ? colorValue : Color32.White;

                break;
        }
    }

    private static void ApplyMaterialInstanceParameter(string name, MaterialParameter parameter, Material instance)
    {
        if(instance?.materialResource == null)
        {
            return;
        }

        switch(parameter.type)
        {
            case MaterialParameterType.Color:

                instance.SetColor(name, parameter.colorValue, parameter.source);

                break;

            case MaterialParameterType.Float:

                instance.SetFloat(name, parameter.floatValue, parameter.source);

                break;

            case MaterialParameterType.Int:

                instance.SetInt(name, parameter.intValue, parameter.source);

                break;

            case MaterialParameterType.Texture:

                instance.SetTexture(name, ResourceManager.instance.LoadTexture(parameter.textureValue));

                break;

            case MaterialParameterType.TextureWrap:

                if(instance.materialResource.parameters.TryGetValue(name, out var p))
                {
                    p.textureWrapValue = parameter.textureWrapValue;
                }
                else
                {
                    instance.materialResource.parameters.Add(name, new()
                    {
                        type = parameter.type,
                        textureWrapValue = parameter.textureWrapValue,
                    });
                }

                break;

            case MaterialParameterType.Vector2:

                instance.SetVector2(name, parameter.vec2Value.ToVector2(), parameter.source);

                break;

            case MaterialParameterType.Vector3:

                instance.SetVector3(name, parameter.vec3Value.ToVector3(), parameter.source);

                break;

            case MaterialParameterType.Vector4:

                instance.SetVector4(name, parameter.vec4Value.ToVector4(), parameter.source);

                break;
        }
    }

    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var material = target as MaterialMetadata;

        var shaderPath = material.shader;
        Shader shader = null;

        if (shaderPath != null)
        {
            if (!cachedShaders.TryGetValue(shaderPath, out shader))
            {
                if (shaderPath.Length > 0)
                {
                    var guid = AssetDatabase.GetAssetGuid(shaderPath);

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

        if(!(activeShader?.Disposed ?? true) && needsShaderUpdate)
        {
            needsShaderUpdate = false;

            foreach(var uniform in activeShader.shaderResource.metadata.uniforms)
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

                if(!isValidType)
                {
                    continue;
                }

                if(!material.parameters.TryGetValue(uniform.name, out var parameter))
                {
                    parameter = new()
                    {
                        source = MaterialParameterSource.Uniform,
                    };

                    material.parameters.Add(uniform.name, parameter);

                    InitializeMaterialParameter(parameter, uniform);
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

                    var attribute = ShaderUniformAttributeType.None;

                    if (activeShader.TryGetUniformAttributeType(parameter.Key, out attribute) &&
                        attribute == ShaderUniformAttributeType.HideInInspector)
                    {
                        continue;
                    }

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

                                if(!cachedTextures.ContainsKey(key) && key.Length > 0)
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

                                    if (ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                                    {
                                        materialInstance.SetTexture(parameter.Key, (Texture)newValue);
                                    }
                                }
                            }

                            break;

                        case MaterialParameterType.Vector2:

                            {
                                var current = parameter.Value.vec2Value.ToVector2();

                                var newValue = EditorGUI.Vector2Field(label, parameter.Key, current);

                                if (newValue != current)
                                {
                                    parameter.Value.vec2Value.x = newValue.X;
                                    parameter.Value.vec2Value.y = newValue.Y;

                                    if (ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                                    {
                                        materialInstance.SetVector2(parameter.Key, newValue);
                                    }
                                }
                            }

                            break;

                        case MaterialParameterType.Vector3:

                            {
                                var current = parameter.Value.vec3Value.ToVector3();

                                var newValue = EditorGUI.Vector3Field(label, parameter.Key, current);

                                if (newValue != current)
                                {
                                    parameter.Value.vec3Value.x = newValue.X;
                                    parameter.Value.vec3Value.y = newValue.Y;
                                    parameter.Value.vec3Value.z = newValue.Z;

                                    if (ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                                    {
                                        materialInstance.SetVector3(parameter.Key, newValue);
                                    }
                                }
                            }

                            break;

                        case MaterialParameterType.Vector4:

                            {
                                var current = parameter.Value.vec4Value.ToVector4();

                                var newValue = EditorGUI.Vector4Field(label, parameter.Key, current);

                                if (newValue != current)
                                {
                                    parameter.Value.vec4Value.x = newValue.X;
                                    parameter.Value.vec4Value.y = newValue.Y;
                                    parameter.Value.vec4Value.z = newValue.Z;
                                    parameter.Value.vec4Value.w = newValue.W;

                                    if (ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                                    {
                                        materialInstance.SetVector4(parameter.Key, newValue);
                                    }
                                }
                            }

                            break;

                        case MaterialParameterType.Color:

                            {
                                var previous = parameter.Value.colorValue;

                                var newValue = (Color32)EditorGUI.ColorField(label, parameter.Key, parameter.Value.colorValue);

                                if(previous != newValue)
                                {
                                    parameter.Value.colorValue = newValue;

                                    if (ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                                    {
                                        materialInstance.SetColor(parameter.Key, newValue);
                                    }
                                }
                            }

                            break;

                        case MaterialParameterType.Float:

                            {
                                var previous = parameter.Value.floatValue;

                                switch (attribute)
                                {
                                    case ShaderUniformAttributeType.None:

                                        parameter.Value.floatValue = EditorGUI.FloatField(label, parameter.Key, parameter.Value.floatValue);

                                        break;

                                    case ShaderUniformAttributeType.Toggle:

                                        parameter.Value.floatValue = EditorGUI.Toggle(label, parameter.Key, parameter.Value.floatValue == 1) ? 1 : 0;

                                        break;
                                }

                                var newValue = parameter.Value.floatValue;

                                if(previous != newValue &&
                                    ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                                {
                                    materialInstance.SetFloat(parameter.Key, newValue);
                                }
                            }

                            break;

                        case MaterialParameterType.Int:

                            {
                                var previous = parameter.Value.intValue;

                                switch (attribute)
                                {
                                    case ShaderUniformAttributeType.None:

                                        parameter.Value.intValue = EditorGUI.IntField(label, parameter.Key, parameter.Value.intValue);

                                        break;

                                    case ShaderUniformAttributeType.Toggle:

                                        parameter.Value.intValue = EditorGUI.Toggle(label, parameter.Key, parameter.Value.intValue == 1) ? 1 : 0;

                                        break;
                                }

                                var newValue = parameter.Value.intValue;

                                if (previous != newValue &&
                                    ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                                {
                                    materialInstance.SetInt(parameter.Key, newValue);
                                }
                            }

                            break;

                        case MaterialParameterType.TextureWrap:

                            {
                                var previous = parameter.Value.textureWrapValue;

                                var newValue = EditorGUI.EnumDropdown(label, parameter.Key, parameter.Value.textureWrapValue);

                                if(newValue != previous)
                                {
                                    parameter.Value.textureWrapValue = newValue;

                                    if(ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance) &&
                                        materialInstance.materialResource.parameters.TryGetValue(parameter.Key, out var p))
                                    {
                                        p.textureWrapValue = newValue;
                                    }
                                }
                            }

                            break;
                    }
                }

                return true;

            case nameof(MaterialMetadata.shader):

                {
                    var newValue = EditorGUI.ObjectPicker(typeof(Shader), "Shader: ", shader, $"{material.guid}.{shaderPath}.Picker");

                    if (newValue != shader)
                    {
                        var materialInstance = ResourceManager.instance.GetMaterial(material.guid);

                        if (newValue is Shader s && !(s?.Disposed ?? true))
                        {
                            cachedShaders.AddOrSetKey(s.shaderResource.metadata.guid, s);

                            material.shader = s.Guid.Guid;

                            material.parameters = [];

                            materialInstance?.materialResource?.parameters.Clear();

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

                            foreach(var uniform in s.shaderResource.metadata.uniforms)
                            {
                                if(uniform.type == ShaderUniformType.Float &&
                                    uniform.name.EndsWith("Set") && s.shaderResource.metadata.uniforms
                                        .Any(x => x.type == ShaderUniformType.Texture &&
                                            x.name == uniform.name[..^"Set".Length]))
                                {
                                    continue;
                                }

                                var type = ParameterType(uniform.type);

                                if(type == (MaterialParameterType)(-1))
                                {
                                    continue;
                                }

                                var parameter = new MaterialParameter()
                                {
                                    type = type,
                                    source = MaterialParameterSource.Uniform,
                                };

                                InitializeMaterialParameter(parameter, uniform);

                                material.parameters.Add(uniform.name, parameter);

                                ApplyMaterialInstanceParameter(uniform.name, parameter, materialInstance);
                            }

                            foreach (var uniform in s.shaderResource.metadata.instanceParameters)
                            {
                                if (uniform.type == ShaderUniformType.Float &&
                                    uniform.name.EndsWith("Set") && s.shaderResource.metadata.uniforms
                                        .Any(x => x.type == ShaderUniformType.Texture &&
                                            x.name == uniform.name[..^"Set".Length]))
                                {
                                    continue;
                                }

                                var type = ParameterType(uniform.type);

                                if (type == (MaterialParameterType)(-1))
                                {
                                    continue;
                                }

                                var parameter = new MaterialParameter()
                                {
                                    type = type,
                                    source = MaterialParameterSource.Instance,
                                };

                                material.parameters.Add(uniform.name, parameter);

                                ApplyMaterialInstanceParameter(uniform.name, parameter, materialInstance);
                            }

                            activeShader = s;

                            materialInstance?.materialResource?.shader = s;
                        }
                        else
                        {
                            material.shader = "";

                            activeShader = null;

                            materialInstance?.materialResource?.shader = null;
                        }
                    }
                }

                return true;

            case nameof(MaterialMetadata.enabledShaderVariants):

                if(!(activeShader?.Disposed ?? true) && activeShader.shaderResource.metadata.variants.Length > 0)
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
                        var currentIndex = activeShader.shaderResource.metadata.variants.IndexOf(material.enabledShaderVariants[i]);
                        var index = EditorGUI.Dropdown("", $"MaterialVariant{i}", activeShader.shaderResource.metadata.variants.ToArray(),
                            currentIndex);

                        if(currentIndex != index && index >= 0)
                        {
                            material.enabledShaderVariants[i] = activeShader.shaderResource.metadata.variants[index];

                            if(ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                            {
                                materialInstance.EnableShaderKeyword(material.enabledShaderVariants[i]);
                            }
                        }

                        EditorGUI.SameLine();

                        EditorGUI.Button("-", $"MaterialVariantRemove{i}", () =>
                        {
                            skip = true;

                            var variant = material.enabledShaderVariants[i];

                            if (ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                            {
                                materialInstance.DisableShaderKeyword(variant);
                            }

                            material.enabledShaderVariants.RemoveAt(i);
                        });

                        if(skip)
                        {
                            break;
                        }
                    }
                }

                return true;

            case nameof(MaterialMetadata.cullingMode):

                if(getter() is CullingMode cullingMode)
                {
                    var newValue = EditorGUI.EnumDropdown(name.ExpandCamelCaseName(), "MaterialCullingMode", cullingMode);

                    if(newValue != cullingMode)
                    {
                        material.cullingMode = newValue;

                        if(ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                        {
                            materialInstance.materialResource.metadata.cullingMode = newValue;
                        }
                    }
                }

                return true;

            case nameof(MaterialMetadata.overrideShaderRenderQueue):

                if (getter() is bool value)
                {
                    var newValue = EditorGUI.Toggle(name.ExpandCamelCaseName(), "MaterialOverrideRenderQueue", value);

                    if (newValue != value)
                    {
                        material.overrideShaderRenderQueue = newValue;

                        if (ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                        {
                            materialInstance.materialResource.metadata.overrideShaderRenderQueue = newValue;
                        }
                    }
                }

                return true;

            case nameof(MaterialMetadata.renderQueue):

                if (!material.overrideShaderRenderQueue)
                {
                    return true;
                }

                if(getter() is MaterialRenderQueue renderQueue)
                {
                    var newValue = EditorGUI.EnumDropdown(name.ExpandCamelCaseName(), "MaterialRenderQueue", renderQueue);

                    if (newValue != renderQueue)
                    {
                        material.renderQueue = newValue;

                        if (ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                        {
                            materialInstance.materialResource.metadata.renderQueue = newValue;
                        }
                    }
                }

                return true;

            case nameof(MaterialMetadata.renderQueueOffset):

                if(!material.overrideShaderRenderQueue)
                {
                    return true;
                }

                if (getter() is int offset)
                {
                    var newValue = EditorGUI.IntField(name.ExpandCamelCaseName(), "MaterialRenderOffset", offset);

                    if (newValue != offset)
                    {
                        material.renderQueueOffset = newValue;

                        if (ResourceManager.instance.TryGetMaterial(material.guid, out var materialInstance))
                        {
                            materialInstance.materialResource.metadata.renderQueueOffset = newValue;
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

        ShowAssetUI(null, () =>
        {
            if(target is MaterialMetadata material)
            {
                ResourceManager.instance.ReloadMaterial(material.guid);
            }
        });
    }
}
