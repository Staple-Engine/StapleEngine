using Staple.Internal;
using System.Collections.Generic;
using System.Reflection;

namespace Staple.Editor
{
    [CustomEditor(typeof(MaterialMetadata))]
    internal class MaterialEditor : Editor
    {
        private Dictionary<string, Shader> cachedShaders = new();
        private Dictionary<string, Texture> cachedTextures = new();

        public override bool RenderField(FieldInfo field)
        {
            var material = target as MaterialMetadata;

            switch(field.Name)
            {
                case nameof(MaterialMetadata.guid):
                case nameof(MaterialMetadata.typeName):

                    return true;

                case nameof(MaterialMetadata.parameters):

                    foreach (var parameter in material.parameters)
                    {
                        var name = parameter.Key;

                        name = EditorGUI.TextField($"Name: ##{parameter.Key}", name);

                        if (name != parameter.Key && material.parameters.ContainsKey(name) == false)
                        {
                            material.parameters.Remove(parameter.Key);

                            material.parameters.Add(name, parameter.Value);

                            break;
                        }

                        parameter.Value.type = EditorGUI.EnumDropdown($"Type: ##{parameter.Key}", parameter.Value.type);

                        var label = $"Value: ##{parameter.Key}";

                        switch (parameter.Value.type)
                        {
                            case MaterialParameterType.Texture:

                                {
                                    var key = $"{parameter.Key}|{parameter.Value.textureValue}";

                                    cachedTextures.TryGetValue(key, out var texture);

                                    var newValue = EditorGUI.ObjectPicker(typeof(Texture), label, texture);

                                    if(newValue != texture)
                                    {
                                        if(newValue is Texture t)
                                        {
                                            cachedTextures.AddOrSetKey(key, t);

                                            parameter.Value.textureValue = t.Path;
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

                                    var newValue = EditorGUI.Vector2Field($"Value: ##{parameter.Key}", current);

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

                                    var newValue = EditorGUI.Vector3Field($"Default Value: ##{parameter.Key}", current);

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

                                    var newValue = EditorGUI.Vector4Field($"Default Value: ##{parameter.Key}", current);

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

                                parameter.Value.colorValue = EditorGUI.ColorField($"Default Value: ##{parameter.Key}", parameter.Value.colorValue);

                                break;

                            case MaterialParameterType.Float:

                                parameter.Value.floatValue = EditorGUI.FloatField($"Default Value: ##{parameter.Key}", parameter.Value.floatValue);

                                break;
                        }
                    }

                    return true;

                case nameof(MaterialMetadata.shaderPath):



                    return true;
            }


            return base.RenderField(field);
        }
    }
}
