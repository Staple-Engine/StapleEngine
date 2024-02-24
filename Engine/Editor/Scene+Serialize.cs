using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Staple.Internal;

internal static class SceneSerialize
{
    public static SerializableScene Serialize(this Scene scene)
    {
        var outValue = new SerializableScene();

        Scene.IterateEntities((Entity entity) =>
        {
            SceneObjectTransform transform = null;

            var components = new List<SceneComponent>();

            var entityTransform = entity.GetComponent<Transform>();

            var parent = entityTransform.parent?.entity ?? default;

            if (entityTransform != null)
            {
                transform = new()
                {
                    position = new Vector3Holder(entityTransform.LocalPosition),
                    rotation = new Vector3Holder(entityTransform.LocalRotation),
                    scale = new Vector3Holder(entityTransform.LocalScale),
                };
            }

            entity.IterateComponents((ref IComponent component) =>
            {
                if (component == null || component.GetType() == typeof(Transform))
                {
                    return;
                }

                var sceneComponent = new SceneComponent()
                {
                    type = component.GetType().FullName,
                    data = new Dictionary<string, object>(),
                };

                var fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    if(field.GetCustomAttribute<NonSerializedAttribute>() != null)
                    {
                        continue;
                    }

                    if (field.FieldType.IsGenericType)
                    {
                        if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var listType = field.FieldType.GetGenericArguments()[0];

                            if (listType != null)
                            {
                                if (listType.GetInterface(typeof(IGuidAsset).FullName) != null)
                                {
                                    var newList = new List<string>();

                                    var inList = (IList)field.GetValue(component);

                                    foreach (var item in inList)
                                    {
                                        if (item is IGuidAsset g)
                                        {
                                            newList.Add(g.Guid);
                                        }
                                    }

                                    sceneComponent.data.Add(field.Name, newList);
                                }
                            }

                            continue;
                        }
                    }

                    if (field.FieldType == typeof(bool) ||
                        field.FieldType == typeof(float) ||
                        field.FieldType == typeof(double) ||
                        field.FieldType == typeof(int) ||
                        field.FieldType == typeof(uint) ||
                        field.FieldType == typeof(string))
                    {
                        sceneComponent.data.Add(field.Name, field.GetValue(component));
                    }
                    else if (field.FieldType.IsEnum)
                    {
                        sceneComponent.data.Add(field.Name, ((Enum)field.GetValue(component)).ToString());
                    }
                    else if(field.FieldType.GetInterface(typeof(IGuidAsset).FullName) != null)
                    {
                        var guidAsset = (IGuidAsset)field.GetValue(component);

                        if(guidAsset != null && (guidAsset.Guid?.Length ?? 0) > 0)
                        {
                            sceneComponent.data.Add(field.Name, guidAsset.Guid);
                        }
                    }
                    else if(field.FieldType == typeof(Vector2))
                    {
                        var value = (Vector2)field.GetValue(component);

                        sceneComponent.data.Add(field.Name, new Vector2Holder()
                        {
                            x = value.X,
                            y = value.Y,
                        });
                    }
                    else if (field.FieldType == typeof(Vector3))
                    {
                        var value = (Vector3)field.GetValue(component);

                        sceneComponent.data.Add(field.Name, new Vector3Holder()
                        {
                            x = value.X,
                            y = value.Y,
                            z = value.Z,
                        });
                    }
                    else if (field.FieldType == typeof(Vector4))
                    {
                        var value = (Vector4)field.GetValue(component);

                        sceneComponent.data.Add(field.Name, new Vector4Holder()
                        {
                            x = value.X,
                            y = value.Y,
                            z = value.Z,
                            w = value.W,
                        });
                    }
                    else if (field.FieldType == typeof(Quaternion))
                    {
                        var value = (Quaternion)field.GetValue(component);

                        sceneComponent.data.Add(field.Name, new Vector4Holder()
                        {
                            x = value.X,
                            y = value.Y,
                            z = value.Z,
                            w = value.W,
                        });
                    }
                    else if (field.FieldType == typeof(Color32))
                    {
                        var color = (Color32)field.GetValue(component);

                        sceneComponent.data.Add(field.Name, $"#{color.HexValue}");
                    }
                    else if (field.FieldType == typeof(Color))
                    {
                        var color = (Color)field.GetValue(component);

                        sceneComponent.data.Add(field.Name, $"#{color.HexValue}");
                    }
                    else if(field.FieldType == typeof(LayerMask))
                    {
                        var mask = (LayerMask)field.GetValue(component);

                        sceneComponent.data.Add(field.Name, mask.value);
                    }
                }

                components.Add(sceneComponent);
            });

            string entityLayer;

            var index = entity.Layer;

            if(index < LayerMask.AllLayers.Count)
            {
                entityLayer = LayerMask.AllLayers[(int)index];
            }
            else
            {
                entityLayer = LayerMask.AllLayers.FirstOrDefault();
            }

            var outEntity = new SceneObject()
            {
                ID = entity.Identifier.ID,
                name = entity.Name,
                enabled = entity.Enabled,
                kind = SceneObjectKind.Entity,
                parent = parent.Identifier.ID,
                transform = transform,
                components = components,
                layer = entityLayer,
            };

            outValue.objects.Add(outEntity);
        });

        return outValue;
    }
}
