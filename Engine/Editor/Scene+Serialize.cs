using System;
using System.Collections.Generic;
using System.Reflection;

namespace Staple.Internal
{
    internal static class SceneSerialize
    {
        public static SerializableScene Serialize(this Scene scene)
        {
            var outValue = new SerializableScene();

            scene.world.Iterate((Entity entity) =>
            {
                SceneObjectTransform transform = null;
                var components = new List<SceneComponent>();

                var entityTransform = scene.GetComponent<Transform>(entity);

                var parent = entityTransform.parent?.entity ?? Entity.Empty;

                if (entityTransform != null)
                {
                    transform = new SceneObjectTransform()
                    {
                        position = new Vector3Holder(entityTransform.LocalPosition),
                        rotation = new Vector3Holder(entityTransform.LocalRotation),
                        scale = new Vector3Holder(entityTransform.LocalScale),
                    };
                }

                scene.world.IterateComponents(entity, (ref IComponent component) =>
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
                        else if(field.FieldType.GetInterface(typeof(IPathAsset).FullName) != null)
                        {
                            var pathAsset = (IPathAsset)field.GetValue(component);

                            if(pathAsset != null && (pathAsset.Path?.Length ?? 0) > 0)
                            {
                                var path = AssetSerialization.GetAssetPathFromCache(pathAsset.Path);

                                sceneComponent.data.Add(field.Name, path);
                            }
                        }
                        else if (field.FieldType == typeof(Color32))
                        {
                            var color = (Color32)field.GetValue(component);

                            sceneComponent.data.Add(field.Name, "#" + color.UIntValue.ToString("X2"));
                        }
                        else if (field.FieldType == typeof(Color))
                        {
                            var color = (Color)field.GetValue(component);

                            sceneComponent.data.Add(field.Name, "#" + color.UIntValue.ToString("X2"));
                        }
                        else if(field.FieldType == typeof(LayerMask))
                        {
                            var mask = (LayerMask)field.GetValue(component);

                            sceneComponent.data.Add(field.Name, mask.value);
                        }
                    }

                    components.Add(sceneComponent);
                });

                var outEntity = new SceneObject()
                {
                    ID = entity.ID,
                    name = scene.world.GetEntityName(entity),
                    enabled = scene.world.IsEntityEnabled(entity),
                    kind = SceneObjectKind.Entity,
                    parent = parent.ID,
                    transform = transform,
                    components = components,
                };

                outValue.objects.Add(outEntity);
            });

            return outValue;
        }
    }
}
