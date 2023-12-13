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
                                    var key = parameter.Key;

                                    if(cachedTextures.ContainsKey(key) == false)
                                    {
                                        var t = ResourceManager.instance.LoadTexture(parameter.Value.textureValue);

                                        if(t != null)
                                        {
                                            cachedTextures.AddOrSetKey(key, t);
                                        }
                                    }

                                    cachedTextures.TryGetValue(key, out var texture);

                                    var newValue = EditorGUI.ObjectPicker(typeof(Texture), "Value: ", texture);

                                    if(newValue != texture)
                                    {
                                        if(newValue is Texture t)
                                        {
                                            cachedTextures.AddOrSetKey(key, t);

                                            var localPath = AssetSerialization.GetAssetPathFromCache(t.Path);

                                            parameter.Value.textureValue = localPath;
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
                        }
                    }

                    return true;

                case nameof(MaterialMetadata.shaderPath):

                    {
                        var key = material.shaderPath;
                        Shader shader = null;

                        if(key != null)
                        {
                            if (cachedShaders.TryGetValue(key, out shader) == false)
                            {
                                if(key.Length > 0)
                                {
                                    shader = ResourceManager.instance.LoadShader(key);

                                    if (shader != null)
                                    {
                                        cachedShaders.AddOrSetKey(key, shader);
                                    }
                                }
                            }
                        }

                        var newValue = EditorGUI.ObjectPicker(typeof(Shader), "Shader: ", shader);

                        if (newValue != shader)
                        {
                            if (newValue is Shader s)
                            {
                                cachedShaders.AddOrSetKey(key, s);

                                var localPath = AssetSerialization.GetAssetPathFromCache(s.Path);

                                material.shaderPath = localPath;
                            }
                            else
                            {
                                material.shaderPath = "";
                            }
                        }
                    }


                    return true;
            }


            return base.RenderField(field);
        }
    }
}
