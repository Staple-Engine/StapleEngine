using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Staple.Editor;

[CustomEditor(typeof(MaterialMetadata))]
internal class MaterialEditor : Editor
{
    private Dictionary<string, Shader> cachedShaders = new();
    private Dictionary<string, Texture> cachedTextures = new();

    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var material = target as MaterialMetadata;

        switch(name)
        {
            case nameof(MaterialMetadata.guid):
            case nameof(MaterialMetadata.typeName):

                return true;

            case nameof(MaterialMetadata.parameters):

                foreach (var parameter in material.parameters)
                {
                    var label = parameter.Key.ExpandCamelCaseName();

                    switch (parameter.Value.type)
                    {
                        case MaterialParameterType.Texture:

                            {
                                var key = parameter.Value.textureValue ?? "";

                                if(cachedTextures.ContainsKey(key) == false)
                                {
                                    var t = ResourceManager.instance.LoadTexture(key);

                                    if (t != null)
                                    {
                                        cachedTextures.AddOrSetKey(key, t);
                                    }
                                }

                                cachedTextures.TryGetValue(key, out var texture);

                                var newValue = EditorGUI.ObjectPicker(typeof(Texture), label, texture);

                                if(newValue != texture)
                                {
                                    if(newValue is Texture t)
                                    {
                                        var guid = t.Guid;

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

                                var newValue = EditorGUI.Vector2Field(label, current);

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

                                var newValue = EditorGUI.Vector3Field(label, current);

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

                                var newValue = EditorGUI.Vector4Field(label, current);

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

                            parameter.Value.colorValue = EditorGUI.ColorField(label, parameter.Value.colorValue);

                            break;

                        case MaterialParameterType.Float:

                            parameter.Value.floatValue = EditorGUI.FloatField(label, parameter.Value.floatValue);

                            break;

                        case MaterialParameterType.TextureWrap:

                            parameter.Value.textureWrapValue = EditorGUI.EnumDropdown(label, parameter.Value.textureWrapValue);

                            break;
                    }
                }

                return true;

            case nameof(MaterialMetadata.shader):

                {
                    var key = material.shader;
                    Shader shader = null;

                    if(key != null)
                    {
                        if (cachedShaders.TryGetValue(key, out shader) == false)
                        {
                            if(key.Length > 0)
                            {
                                shader = ResourceManager.instance.LoadShader(material.shader);

                                cachedShaders.AddOrSetKey(key, shader);

                                if(shader != null)
                                {
                                    material.shader = shader.Guid;
                                }
                            }
                        }
                    }

                    var newValue = EditorGUI.ObjectPicker(typeof(Shader), "Shader: ", shader);

                    if (newValue != shader)
                    {
                        if (newValue is Shader s)
                        {
                            cachedShaders.AddOrSetKey(s.metadata.guid, s);

                            material.shader = s.Guid;
                        }
                        else
                        {
                            material.shader = "";
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

        var metadata = (MaterialMetadata)target;
        var originalMetadata = (MaterialMetadata)original;

        var hasChanges = metadata != originalMetadata;

        if (hasChanges)
        {
            EditorGUI.Button("Apply", () =>
            {
                try
                {
                    var text = JsonConvert.SerializeObject(metadata, Formatting.Indented, new JsonSerializerSettings()
                    {
                        Converters =
                        {
                            new StringEnumConverter(),
                        }
                    });

                    File.WriteAllText(path, text);
                }
                catch (Exception)
                {
                }

                var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(original, field.GetValue(metadata));
                }

                EditorUtils.RefreshAssets(false, null);
            });

            EditorGUI.SameLine();

            EditorGUI.Button("Revert", () =>
            {
                metadata.shader = originalMetadata.shader;
                metadata.parameters.Clear();

                foreach (var parameter in originalMetadata.parameters)
                {
                    metadata.parameters.Add(parameter.Key, parameter.Value.Clone());
                }

                EditorGUI.pendingObjectPickers.Clear();
            });
        }
        else
        {
            EditorGUI.ButtonDisabled("Apply", null);

            EditorGUI.SameLine();

            EditorGUI.ButtonDisabled("Revert", null);
        }
    }
}
