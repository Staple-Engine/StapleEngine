using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    internal static class Scene_Serialize
    {
        public static SerializableScene Serialize(this Scene scene)
        {
            var outValue = new SerializableScene();

            foreach(var entity in scene.entities)
            {
                Entity parent = null;
                SceneObjectTransform transform = null;
                var components = new List<SceneComponent>();

                entity.Transform?.parent?.entity.TryGetTarget(out parent);

                if(entity.Transform != null)
                {
                    transform = new SceneObjectTransform()
                    {
                        position = new Vector3Holder(entity.Transform.LocalPosition),
                        rotation = new Vector3Holder(entity.Transform.LocalRotation),
                        scale = new Vector3Holder(entity.Transform.LocalScale),
                    };
                }

                foreach(var component in entity.components)
                {
                    if(component == null)
                    {
                        continue;
                    }

                    var sceneComponent = new SceneComponent()
                    {
                        type = component.GetType().FullName,
                        data = new Dictionary<string, object>(),
                    };

                    var fields = component.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                    foreach(var field in fields)
                    {
                        if(field.FieldType == typeof(bool))
                        {
                            sceneComponent.data.Add(field.Name, (bool)field.GetValue(component));
                        }
                        else if(field.FieldType == typeof(float))
                        {
                            sceneComponent.data.Add(field.Name, (float)field.GetValue(component));
                        }
                        else if (field.FieldType == typeof(int))
                        {
                            sceneComponent.data.Add(field.Name, (int)field.GetValue(component));
                        }
                        else if (field.FieldType == typeof(string))
                        {
                            sceneComponent.data.Add(field.Name, (string)field.GetValue(component));
                        }
                        else if (field.FieldType.IsEnum)
                        {
                            sceneComponent.data.Add(field.Name, ((Enum)field.GetValue(component)).ToString());
                        }
                        else if(field.FieldType == typeof(Material))
                        {
                            var material = (Material)field.GetValue(component);

                            if(material != null && material.path != null)
                            {
                                sceneComponent.data.Add(field.Name, material.path);
                            }
                        }
                        else if (field.FieldType == typeof(Texture))
                        {
                            var texture = (Texture)field.GetValue(component);

                            if (texture != null && texture.path != null)
                            {
                                sceneComponent.data.Add(field.Name, texture.path);
                            }
                        }
                        else if(field.FieldType == typeof(Color32) || field.FieldType == typeof(Color))
                        {
                            var color = (Color32)field.GetValue(component);

                            sceneComponent.data.Add(field.Name, "#" + color.uintValue.ToString("X2"));
                        }
                    }

                    components.Add(sceneComponent);
                }

                var outEntity = new SceneObject()
                {
                    ID = entity.ID,
                    name = entity.Name,
                    kind = SceneObjectKind.Entity,
                    parent = parent?.ID,
                    transform = transform,
                    components = components,
                };

                outValue.objects.Add(outEntity);
            }

            return outValue;
        }
    }
}
