using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System;
using System.Collections;
using System.Linq;

namespace Staple.Internal;

internal static class SceneSerialization
{
    public static void DeserializeField(FieldInfo field, ref IComponent componentInstance, JsonElement element)
    {
        if (field.FieldType.IsGenericType)
        {
            if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>) && element.ValueKind == JsonValueKind.Array)
            {
                var listType = field.FieldType.GetGenericArguments()[0];

                if (listType != null)
                {
                    var o = Activator.CreateInstance(field.FieldType);

                    if (o == null)
                    {
                        return;
                    }

                    var list = (IList)o;

                    if (list == null)
                    {
                        return;
                    }

                    if (listType.GetInterface(typeof(IGuidAsset).FullName) != null)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                var path = item.GetString();

                                var value = AssetSerialization.GetGuidAsset(listType, path);

                                if (value != null)
                                {
                                    list.Add(value);
                                }
                            }
                        }
                    }

                    field.SetValue(componentInstance, list);
                }
            }
        }

        if (field.FieldType == typeof(bool) && (element.ValueKind == JsonValueKind.False || element.ValueKind == JsonValueKind.True))
        {
            field.SetValue(componentInstance, element.GetBoolean());
        }
        else if (field.FieldType == typeof(float) && element.ValueKind == JsonValueKind.Number)
        {
            field.SetValue(componentInstance, element.GetSingle());
        }
        else if (field.FieldType == typeof(int) && element.ValueKind == JsonValueKind.Number)
        {
            field.SetValue(componentInstance, element.GetInt32());
        }
        else if (field.FieldType == typeof(string) && element.ValueKind == JsonValueKind.String)
        {
            field.SetValue(componentInstance, element.GetString());
        }
        else if (field.FieldType.IsEnum && element.ValueKind == JsonValueKind.String)
        {
            if (Enum.TryParse(field.FieldType, element.GetString(), true, out var value))
            {
                field.SetValue(componentInstance, value);
            }
        }
        else if (field.FieldType.GetInterface(typeof(IGuidAsset).FullName) != null && element.ValueKind == JsonValueKind.String)
        {
            var path = element.GetString();

            var value = AssetSerialization.GetGuidAsset(field.FieldType, path);

            if (value != null)
            {
                field.SetValue(componentInstance, value);
            }
        }
        else if (field.FieldType == typeof(Vector2) && element.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var x = element.GetProperty("x").GetDouble();
                var y = element.GetProperty("y").GetDouble();

                field.SetValue(componentInstance, new Vector2((float)x, (float)y));
            }
            catch (Exception e)
            {
                return;
            }
        }
        else if (field.FieldType == typeof(Vector3) && element.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var x = element.GetProperty("x").GetDouble();
                var y = element.GetProperty("y").GetDouble();
                var z = element.GetProperty("z").GetDouble();

                field.SetValue(componentInstance, new Vector3((float)x, (float)y, (float)z));
            }
            catch (Exception e)
            {
                return;
            }
        }
        else if ((field.FieldType == typeof(Vector4) || field.FieldType == typeof(Quaternion)) && element.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var x = element.GetProperty("x").GetDouble();
                var y = element.GetProperty("y").GetDouble();
                var z = element.GetProperty("z").GetDouble();
                var w = element.GetProperty("w").GetDouble();

                if (field.FieldType == typeof(Quaternion))
                {
                    field.SetValue(componentInstance, new Quaternion((float)x, (float)y, (float)z, (float)w));
                }
                else
                {
                    field.SetValue(componentInstance, new Vector4((float)x, (float)y, (float)z, (float)w));
                }
            }
            catch (Exception e)
            {
                return;
            }
        }
        else if ((field.FieldType == typeof(Color32) || field.FieldType == typeof(Color)))
        {
            var color = Color32.White;

            if (element.ValueKind == JsonValueKind.String)
            {
                color = new Color32(element.GetString());
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                try
                {
                    var r = element.GetProperty("r").GetInt32();
                    var g = element.GetProperty("g").GetInt32();
                    var b = element.GetProperty("b").GetInt32();
                    var a = element.GetProperty("a").GetInt32();

                    color = new Color32((byte)r, (byte)g, (byte)b, (byte)a);
                }
                catch (Exception e)
                {
                    return;
                }
            }

            if (field.FieldType == typeof(Color32))
            {
                field.SetValue(componentInstance, color);
            }
            else
            {
                field.SetValue(componentInstance, (Color)color);
            }
        }
    }

    public static void DeserializeField(FieldInfo field, ref IComponent componentInstance, SceneComponentParameter parameter)
    {
        switch (parameter.type)
        {
            case SceneComponentParameterType.Array:

                if (field.FieldType.IsGenericType &&
                    field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var o = Activator.CreateInstance(field.FieldType);

                    if (o == null)
                    {
                        return;
                    }

                    var list = (IList)o;

                    if (list == null)
                    {
                        return;
                    }

                    switch (parameter.arrayType)
                    {
                        case SceneComponentParameterType.String:

                            if (parameter.arrayValue is object[] objectArray)
                            {
                                switch (field.FieldType.GetGenericArguments()[0])
                                {
                                    case Type t when t == typeof(string):

                                        foreach (var obj in objectArray)
                                        {
                                            if (obj is string str)
                                            {
                                                list.Add(str);
                                            }
                                        }

                                        break;

                                    case Type t when t.GetInterface(typeof(IGuidAsset).FullName) != null:

                                        foreach (var obj in objectArray)
                                        {
                                            if (obj is string str)
                                            {
                                                var asset = AssetSerialization.GetGuidAsset(t, str);

                                                list.Add(asset);
                                            }
                                        }

                                        break;
                                }
                            }

                            break;
                    }

                    field.SetValue(componentInstance, list);
                }

                break;

            case SceneComponentParameterType.Bool:

                if (field.FieldType == typeof(bool))
                {
                    field.SetValue(componentInstance, parameter.boolValue);
                }

                break;

            case SceneComponentParameterType.Float:

                if (field.FieldType == typeof(float))
                {
                    field.SetValue(componentInstance, parameter.floatValue);
                }
                else if (field.FieldType == typeof(int))
                {
                    field.SetValue(componentInstance, (int)parameter.floatValue);
                }

                break;

            case SceneComponentParameterType.Int:

                try
                {
                    var value = System.Convert.ChangeType(parameter.intValue, field.FieldType);

                    field.SetValue(componentInstance, value);
                }
                catch (Exception e)
                {
                    return;
                }

                break;

            case SceneComponentParameterType.Vector2:

                if (field.FieldType == typeof(Vector2))
                {
                    field.SetValue(componentInstance, parameter.vector2Value.ToVector2());
                }
                else if (field.FieldType == typeof(Vector3))
                {
                    field.SetValue(componentInstance, parameter.vector2Value.ToVector3());
                }
                else if (field.FieldType == typeof(Vector4))
                {
                    field.SetValue(componentInstance, parameter.vector2Value.ToVector4());
                }

                break;

            case SceneComponentParameterType.Vector3:

                if (field.FieldType == typeof(Vector3))
                {
                    field.SetValue(componentInstance, parameter.vector3Value.ToVector3());
                }
                else if (field.FieldType == typeof(Vector4))
                {
                    field.SetValue(componentInstance, parameter.vector3Value.ToVector4());
                }
                else if (field.FieldType == typeof(Quaternion))
                {
                    field.SetValue(componentInstance, parameter.vector3Value.ToQuaternion());
                }

                break;

            case SceneComponentParameterType.Vector4:

                if (field.FieldType == typeof(Vector4))
                {
                    field.SetValue(componentInstance, parameter.vector4Value.ToVector4());
                }

                break;

            case SceneComponentParameterType.String:

                if (field.FieldType == typeof(string))
                {
                    field.SetValue(componentInstance, parameter.stringValue);
                }
                else if (field.FieldType.IsEnum)
                {
                    try
                    {
                        var value = Enum.Parse(field.FieldType, parameter.stringValue);

                        if (value != null)
                        {
                            field.SetValue(componentInstance, value);
                        }
                    }
                    catch (Exception e)
                    {
                        return;
                    };
                }
                else if ((field.FieldType == typeof(Color32) || field.FieldType == typeof(Color)))
                {
                    var color = new Color32(parameter.stringValue);

                    if (field.FieldType == typeof(Color32))
                    {
                        field.SetValue(componentInstance, color);
                    }
                    else
                    {
                        field.SetValue(componentInstance, (Color)color);
                    }
                }
                else if (field.FieldType.GetInterface(typeof(IGuidAsset).FullName) != null)
                {
                    var path = parameter.stringValue;

                    var value = AssetSerialization.GetGuidAsset(field.FieldType, path);

                    if (value != null)
                    {
                        field.SetValue(componentInstance, value);
                    }
                }
                else if (field.FieldType == typeof(LayerMask))
                {
                    var mask = new LayerMask();

                    if (parameter.stringValue.ToUpperInvariant() == "EVERYTHING")
                    {
                        mask = LayerMask.Everything;
                    }
                    else
                    {
                        var layers = LayerMask.GetMask(parameter.stringValue.Split(" ".ToCharArray()));

                        mask.value = layers;
                    }

                    field.SetValue(componentInstance, mask);
                }

                break;
        }
    }

    /// <summary>
    /// Instantiates all components in an entity into another entity
    /// </summary>
    /// <param name="source">The source entity</param>
    /// <param name="target">The target entity</param>
    public static void InstantiateEntityComponents(Entity source, Entity target)
    {
        source.IterateComponents((ref IComponent component) =>
        {
            if(component is Transform)
            {
                return;
            }

            var localComponent = target.AddComponent(component.GetType());

            if (localComponent == null)
            {
                return;
            }

            var fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    continue;
                }

                try
                {
                    field.SetValue(localComponent, field.GetValue(component));
                }
                catch (Exception)
                {
                }
            }
        });
    }

    public static SceneObject SerializeEntityComponents(Entity entity, bool parameters)
    {
        var components = new List<SceneComponent>();

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
                if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
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

                                if(parameters)
                                {
                                    sceneComponent.parameters.Add(new SceneComponentParameter()
                                    {
                                        name = field.Name,
                                        arrayType = SceneComponentParameterType.String,
                                        type = SceneComponentParameterType.Array,
                                        arrayValue = newList,
                                    });
                                }
                                else
                                {
                                    sceneComponent.data.Add(field.Name, newList);
                                }
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
                    if(parameters)
                    {
                        switch(field.FieldType)
                        {
                            case Type t when t == typeof(bool):

                                sceneComponent.parameters.Add(new SceneComponentParameter()
                                {
                                    name = field.Name,
                                    type = SceneComponentParameterType.Bool,
                                    boolValue = (bool)field.GetValue(component),
                                });

                                break;

                            case Type t when t == typeof(float):

                                sceneComponent.parameters.Add(new SceneComponentParameter()
                                {
                                    name = field.Name,
                                    type = SceneComponentParameterType.Float,
                                    floatValue = (float)field.GetValue(component),
                                });

                                break;

                            case Type t when t == typeof(double):

                                sceneComponent.parameters.Add(new SceneComponentParameter()
                                {
                                    name = field.Name,
                                    type = SceneComponentParameterType.Float,
                                    floatValue = (float)field.GetValue(component),
                                });

                                break;

                            case Type t when t == typeof(int):

                                sceneComponent.parameters.Add(new SceneComponentParameter()
                                {
                                    name = field.Name,
                                    type = SceneComponentParameterType.Int,
                                    intValue = (int)field.GetValue(component),
                                });

                                break;

                            case Type t when t == typeof(uint):

                                sceneComponent.parameters.Add(new SceneComponentParameter()
                                {
                                    name = field.Name,
                                    type = SceneComponentParameterType.Int,
                                    intValue = (int)field.GetValue(component),
                                });

                                break;


                            case Type t when t == typeof(string):

                                sceneComponent.parameters.Add(new SceneComponentParameter()
                                {
                                    name = field.Name,
                                    type = SceneComponentParameterType.String,
                                    stringValue = (string)field.GetValue(component),
                                });

                                break;
                        }
                    }
                    else
                    {
                        sceneComponent.data.Add(field.Name, field.GetValue(component));
                    }
                }
                else if (field.FieldType.IsEnum)
                {
                    if(parameters)
                    {
                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = field.Name,
                            type = SceneComponentParameterType.String,
                            stringValue = ((Enum)field.GetValue(component)).ToString()
                        });
                    }
                    else
                    {
                        sceneComponent.data.Add(field.Name, ((Enum)field.GetValue(component)).ToString());
                    }
                }
                else if (field.FieldType.GetInterface(typeof(IGuidAsset).FullName) != null)
                {
                    var guidAsset = (IGuidAsset)field.GetValue(component);

                    if (guidAsset != null && (guidAsset.Guid?.Length ?? 0) > 0)
                    {
                        if(parameters)
                        {
                            sceneComponent.parameters.Add(new SceneComponentParameter()
                            {
                                name = field.Name,
                                type = SceneComponentParameterType.String,
                                stringValue = guidAsset.Guid,
                            });
                        }
                        else
                        {
                            sceneComponent.data.Add(field.Name, guidAsset.Guid);
                        }
                    }
                }
                else if (field.FieldType == typeof(Vector2))
                {
                    var value = (Vector2)field.GetValue(component);

                    if (parameters)
                    {
                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = field.Name,
                            type = SceneComponentParameterType.Vector2,
                            vector2Value = new Vector2Holder()
                            {
                                x = value.X,
                                y = value.Y,
                            },
                        });
                    }
                    else
                    {
                        sceneComponent.data.Add(field.Name, new Vector2Holder()
                        {
                            x = value.X,
                            y = value.Y,
                        });
                    }
                }
                else if (field.FieldType == typeof(Vector3))
                {
                    var value = (Vector3)field.GetValue(component);

                    if (parameters)
                    {
                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = field.Name,
                            type = SceneComponentParameterType.Vector3,
                            vector3Value = new Vector3Holder()
                            {
                                x = value.X,
                                y = value.Y,
                                z = value.Z,
                            },
                        });
                    }
                    else
                    {
                        sceneComponent.data.Add(field.Name, new Vector3Holder()
                        {
                            x = value.X,
                            y = value.Y,
                            z = value.Z,
                        });
                    }
                }
                else if (field.FieldType == typeof(Vector4))
                {
                    var value = (Vector4)field.GetValue(component);

                    if (parameters)
                    {
                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = field.Name,
                            type = SceneComponentParameterType.Vector4,
                            vector4Value = new Vector4Holder()
                            {
                                x = value.X,
                                y = value.Y,
                                z = value.Z,
                                w = value.W,
                            },
                        });
                    }
                    else
                    {
                        sceneComponent.data.Add(field.Name, new Vector4Holder()
                        {
                            x = value.X,
                            y = value.Y,
                            z = value.Z,
                            w = value.W,
                        });
                    }
                }
                else if (field.FieldType == typeof(Quaternion))
                {
                    var value = (Quaternion)field.GetValue(component);

                    if (parameters)
                    {
                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = field.Name,
                            type = SceneComponentParameterType.Vector4,
                            vector4Value = new Vector4Holder()
                            {
                                x = value.X,
                                y = value.Y,
                                z = value.Z,
                                w = value.W,
                            },
                        });
                    }
                    else
                    {
                        sceneComponent.data.Add(field.Name, new Vector4Holder()
                        {
                            x = value.X,
                            y = value.Y,
                            z = value.Z,
                            w = value.W,
                        });
                    }
                }
                else if (field.FieldType == typeof(Color32))
                {
                    var color = (Color32)field.GetValue(component);

                    if (parameters)
                    {
                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = field.Name,
                            type = SceneComponentParameterType.String,
                            stringValue = $"#{color.HexValue}",
                        });
                    }
                    else
                    {
                        sceneComponent.data.Add(field.Name, $"#{color.HexValue}");
                    }
                }
                else if (field.FieldType == typeof(Color))
                {
                    var color = (Color)field.GetValue(component);


                    if (parameters)
                    {
                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = field.Name,
                            type = SceneComponentParameterType.String,
                            stringValue = $"#{color.HexValue}",
                        });
                    }
                    else
                    {
                        sceneComponent.data.Add(field.Name, $"#{color.HexValue}");
                    }
                }
                else if (field.FieldType == typeof(LayerMask))
                {
                    var mask = (LayerMask)field.GetValue(component);

                    if (parameters)
                    {
                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = field.Name,
                            type = SceneComponentParameterType.Int,
                            intValue = (int)mask.value,
                        });
                    }
                    else
                    {
                        sceneComponent.data.Add(field.Name, mask.value);
                    }
                }
            }

            components.Add(sceneComponent);
        });

        string entityLayer;

        var index = entity.Layer;

        if (index < LayerMask.AllLayers.Count)
        {
            entityLayer = LayerMask.AllLayers[(int)index];
        }
        else
        {
            entityLayer = LayerMask.AllLayers.FirstOrDefault();
        }

        return new SceneObject()
        {
            ID = entity.Identifier.ID,
            name = entity.Name,
            enabled = entity.Enabled,
            kind = SceneObjectKind.Entity,
            components = components,
            layer = entityLayer,
        };
    }

    /// <summary>
    /// Serializes an entity into a SceneObject
    /// </summary>
    /// <param name="entity">The entity to serialize</param>
    /// <param name="parameters">Whether to use parameters instead of data (MessagePack or JSON)</param>
    /// <returns>The entity, or null</returns>
    public static SceneObject SerializeEntity(Entity entity, bool parameters)
    {
        if(entity.IsValid == false)
        {
            return null;
        }

        SceneObjectTransform transform = null;

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

        var outEntity = SerializeEntityComponents(entity, parameters);

        outEntity.parent = parent.IsValid ? parent.Identifier.ID : 0;
        outEntity.transform = transform;

        return outEntity;
    }

    /// <summary>
    /// Serializes a scene into a SerializableScene
    /// </summary>
    /// <param name="scene">The scene to serialize to</param>
    /// <returns>The serialized scene</returns>
    public static SerializableScene Serialize(this Scene scene)
    {
        var outValue = new SerializableScene();

        Scene.IterateEntities((Entity entity) =>
        {
            var outEntity = SerializeEntity(entity, false);

            if(outEntity == null)
            {
                return;
            }

            outValue.objects.Add(outEntity);
        });

        return outValue;
    }

    public static SerializablePrefab SerializeIntoPrefab(Entity entity)
    {
        if(entity.IsValid == false ||
            entity.TryGetComponent<Transform>(out var entityTransform) == false)
        {
            return null;
        }

        var outValue = new SerializablePrefab();

        outValue.mainObject = SerializeEntity(entity, false);

        outValue.mainObject.ID = 0;

        var localIDs = new Dictionary<int, int>();

        void GatherIDs(Transform transform)
        {
            localIDs.Add(transform.entity.Identifier.ID, localIDs.Count);

            foreach(var child in transform)
            {
                GatherIDs(child);
            }
        }

        GatherIDs(entityTransform);

        void GatherSceneObjects(Transform transform, bool first)
        {
            if(first == false)
            {
                var entityObject = SerializeEntity(transform.entity, false);

                if(entityObject == null ||
                    localIDs.TryGetValue(transform.entity.Identifier.ID, out var localID) == false ||
                    localIDs.TryGetValue(transform.parent.entity.Identifier.ID, out var localParent) == false)
                {
                    return;
                }

                entityObject.ID = localID;
                entityObject.parent = localParent;

                outValue.children.Add(entityObject);
            }

            foreach (var child in transform)
            {
                GatherSceneObjects(child, false);
            }
        }

        GatherSceneObjects(entityTransform, true);

        return outValue;
    }

    public static Entity InstantiatePrefab(Entity parent, SerializablePrefab prefab)
    {
        if(prefab == null ||
            prefab.mainObject == null ||
            prefab.children == null)
        {
            return default;
        }

        var parentTransform = parent.GetComponent<Transform>();

        var newEntity = Scene.Instantiate(prefab.mainObject, out _, true);

        var transform = newEntity.GetComponent<Transform>();

        transform.SetParent(parentTransform);

        transform.LocalPosition = prefab.mainObject.transform.position.ToVector3();
        transform.LocalRotation = prefab.mainObject.transform.rotation.ToQuaternion();
        transform.LocalScale = prefab.mainObject.transform.scale.ToVector3();

        var localIDs = new Dictionary<int, int>();

        localIDs.AddOrSetKey(0, newEntity.Identifier.ID);

        var counter = 1;

        foreach(var sceneObject in prefab.children)
        {
            var childEntity = Scene.Instantiate(sceneObject, out var localID, true);

            if(childEntity.IsValid)
            {
                localIDs.Add(counter++, localID);

                if(localIDs.TryGetValue(sceneObject.parent, out var localParentID))
                {
                    var childTransform = childEntity.GetComponent<Transform>();
                    var targetTransform = Scene.FindEntity(localParentID).GetComponent<Transform>();

                    if(targetTransform != null)
                    {
                        childTransform.SetParent(targetTransform);
                    }
                }
            }
        }

        return newEntity;
    }
}