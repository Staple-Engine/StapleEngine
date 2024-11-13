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
    /// <summary>
    /// Deserializes a property into a scene component
    /// </summary>
    /// <param name="fieldType">The property field type</param>
    /// <param name="setter">Setter for the property's value</param>
    /// <param name="parameter">The scene component to get data from</param>
    public static void DeserializeProperty(Type fieldType, Action<object> setter, JsonElement element)
    {
        if (fieldType.IsGenericType)
        {
            if (fieldType.GetGenericTypeDefinition() == typeof(List<>) && element.ValueKind == JsonValueKind.Array)
            {
                var listType = fieldType.GetGenericArguments()[0];

                if (listType != null)
                {
                    var o = Activator.CreateInstance(fieldType);

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

                    setter(list);
                }
            }
        }

        if (fieldType == typeof(bool) && (element.ValueKind == JsonValueKind.False || element.ValueKind == JsonValueKind.True))
        {
            setter(element.GetBoolean());
        }
        else if (fieldType == typeof(float) && element.ValueKind == JsonValueKind.Number)
        {
            setter(element.GetSingle());
        }
        else if (fieldType == typeof(int) && element.ValueKind == JsonValueKind.Number)
        {
            setter(element.GetInt32());
        }
        else if (fieldType == typeof(string) && element.ValueKind == JsonValueKind.String)
        {
            setter(element.GetString());
        }
        else if (fieldType.IsEnum && element.ValueKind == JsonValueKind.String)
        {
            if (Enum.TryParse(fieldType, element.GetString(), true, out var value))
            {
                setter(value);
            }
        }
        else if (fieldType.GetInterface(typeof(IGuidAsset).FullName) != null && element.ValueKind == JsonValueKind.String)
        {
            var path = element.GetString();

            var value = AssetSerialization.GetGuidAsset(fieldType, path);

            if (value != null)
            {
                setter(value);
            }
        }
        else if (fieldType == typeof(Vector2) && element.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var x = element.GetProperty("x").GetDouble();
                var y = element.GetProperty("y").GetDouble();

                setter(new Vector2((float)x, (float)y));
            }
            catch (Exception e)
            {
                return;
            }
        }
        else if (fieldType == typeof(Vector3) && element.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var x = element.GetProperty("x").GetDouble();
                var y = element.GetProperty("y").GetDouble();
                var z = element.GetProperty("z").GetDouble();

                setter(new Vector3((float)x, (float)y, (float)z));
            }
            catch (Exception e)
            {
                return;
            }
        }
        else if ((fieldType == typeof(Vector4) || fieldType == typeof(Quaternion)) && element.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var x = element.GetProperty("x").GetDouble();
                var y = element.GetProperty("y").GetDouble();
                var z = element.GetProperty("z").GetDouble();
                var w = element.GetProperty("w").GetDouble();

                if (fieldType == typeof(Quaternion))
                {
                    setter(new Quaternion((float)x, (float)y, (float)z, (float)w));
                }
                else
                {
                    setter(new Vector4((float)x, (float)y, (float)z, (float)w));
                }
            }
            catch (Exception e)
            {
                return;
            }
        }
        else if (fieldType == typeof(Vector2Int) && element.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var x = element.GetProperty("x").GetDouble();
                var y = element.GetProperty("y").GetDouble();

                setter(new Vector2Int((int)x, (int)y));
            }
            catch (Exception e)
            {
                return;
            }
        }
        else if ((fieldType == typeof(Color32) || fieldType == typeof(Color)))
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

            if (fieldType == typeof(Color32))
            {
                setter(color);
            }
            else
            {
                setter((Color)color);
            }
        }
        else if (fieldType == typeof(LayerMask) && element.ValueKind == JsonValueKind.Number)
        {
            var mask = new LayerMask()
            {
                value = element.GetUInt32(),
            };

            setter(mask);
        }
        else if(fieldType == typeof(EntityCallback) && element.ValueKind == JsonValueKind.Object)
        {
            var persistentCallbacks = element.GetProperty("persistentCallbacks");

            if(persistentCallbacks.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var value = new EntityCallback();

            foreach(var e in element.EnumerateArray())
            {
                if(e.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                try
                {
                    var id = e.GetProperty("entityID").GetInt32();
                    var className = e.GetProperty("className").GetString();
                    var methodName = e.GetProperty("methodName").GetString();

                    value.AddPersistentCallback(new()
                    {
                        entityID = id,
                        className = className,
                        methodName = methodName,
                    });
                }
                catch(Exception)
                {
                }
            }

            setter(value);
        }
    }

    /// <summary>
    /// Deserializes a property into a scene component
    /// </summary>
    /// <param name="fieldType">The property field type</param>
    /// <param name="setter">Setter for the property's value</param>
    /// <param name="parameter">The scene component to get data from</param>
    public static void DeserializeProperty(Type fieldType, Action<object> setter, SceneComponentParameter parameter)
    {
        switch (parameter.type)
        {
            case SceneComponentParameterType.Array:

                if (fieldType.IsGenericType &&
                    fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var o = Activator.CreateInstance(fieldType);

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
                                switch (fieldType.GetGenericArguments()[0])
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

                    setter(list);
                }

                break;

            case SceneComponentParameterType.Bool:

                if (fieldType == typeof(bool))
                {
                    setter(parameter.boolValue);
                }

                break;

            case SceneComponentParameterType.Float:

                if (fieldType == typeof(float))
                {
                    setter(parameter.floatValue);
                }
                else if (fieldType == typeof(int))
                {
                    setter((int)parameter.floatValue);
                }

                break;

            case SceneComponentParameterType.Int:

                if (fieldType == typeof(LayerMask))
                {
                    var mask = new LayerMask()
                    {
                        value = (uint)parameter.intValue,
                    };

                    setter(mask);
                }
                else
                {
                    try
                    {
                        var value = System.Convert.ChangeType(parameter.intValue, fieldType);

                        setter(value);
                    }
                    catch (Exception e)
                    {
                        return;
                    }
                }

                break;

            case SceneComponentParameterType.Vector2:

                if (fieldType == typeof(Vector2))
                {
                    setter(parameter.vector2Value.ToVector2());
                }
                else if(fieldType == typeof(Vector2Int))
                {
                    var value = parameter.vector2Value.ToVector2();

                    setter(new Vector2Int((int)value.X, (int)value.Y));
                }
                else if (fieldType == typeof(Vector3))
                {
                    setter(parameter.vector2Value.ToVector3());
                }
                else if (fieldType == typeof(Vector4))
                {
                    setter(parameter.vector2Value.ToVector4());
                }

                break;

            case SceneComponentParameterType.Vector3:

                if (fieldType == typeof(Vector3))
                {
                    setter(parameter.vector3Value.ToVector3());
                }
                else if (fieldType == typeof(Vector4))
                {
                    setter(parameter.vector3Value.ToVector4());
                }
                else if (fieldType == typeof(Quaternion))
                {
                    setter(parameter.vector3Value.ToQuaternion());
                }

                break;

            case SceneComponentParameterType.Vector4:

                if (fieldType == typeof(Vector4))
                {
                    setter(parameter.vector4Value.ToVector4());
                }

                break;

            case SceneComponentParameterType.String:

                if (fieldType == typeof(string))
                {
                    setter(parameter.stringValue);
                }
                else if (fieldType.IsEnum)
                {
                    try
                    {
                        var value = Enum.Parse(fieldType, parameter.stringValue);

                        if (value != null)
                        {
                            setter(value);
                        }
                    }
                    catch (Exception e)
                    {
                        return;
                    };
                }
                else if ((fieldType == typeof(Color32) || fieldType == typeof(Color)))
                {
                    var color = new Color32(parameter.stringValue);

                    if (fieldType == typeof(Color32))
                    {
                        setter(color);
                    }
                    else
                    {
                        setter((Color)color);
                    }
                }
                else if (fieldType.GetInterface(typeof(IGuidAsset).FullName) != null)
                {
                    var path = parameter.stringValue;

                    var value = AssetSerialization.GetGuidAsset(fieldType, path);

                    if (value != null)
                    {
                        setter(value);
                    }
                }
                else if(fieldType == typeof(EntityCallback))
                {
                    var pieces = parameter.stringValue.Split(":");

                    var value = new EntityCallback();

                    foreach(var piece in pieces)
                    {
                        var parts = piece.Split("|");

                        if(parts.Length != 3 ||
                            int.TryParse(parts[0], out var id) == false)
                        {
                            continue;
                        }

                        var className = parts[1];
                        var methodName = parts[2];

                        value.AddPersistentCallback(new()
                        {
                            entityID = id,
                            className = className,
                            methodName = methodName,
                        });
                    }

                    setter(value);
                }

                break;
        }
    }

    /// <summary>
    /// Serializes a property into a scene component
    /// </summary>
    /// <param name="fieldType">The property field type</param>
    /// <param name="name">The property name</param>
    /// <param name="getter">Getter for the property's value</param>
    /// <param name="sceneComponent">The scene component to fill</param>
    /// <param name="parameters">Whether we want parameters or data</param>
    public static void SerializeProperty(Type fieldType, string name, Func<object> getter, SceneComponent sceneComponent, bool parameters)
    {
        if (fieldType.IsGenericType)
        {
            if (fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var listType = fieldType.GetGenericArguments()[0];

                if (listType != null)
                {
                    if (listType.GetInterface(typeof(IGuidAsset).FullName) != null)
                    {
                        var newList = new List<string>();

                        var inList = (IList)getter();

                        foreach (var item in inList)
                        {
                            if (item is IGuidAsset g)
                            {
                                newList.Add(g.Guid);
                            }
                        }

                        if (parameters)
                        {
                            sceneComponent.parameters.Add(new SceneComponentParameter()
                            {
                                name = name,
                                arrayType = SceneComponentParameterType.String,
                                type = SceneComponentParameterType.Array,
                                arrayValue = newList,
                            });
                        }
                        else
                        {
                            sceneComponent.data.Add(name, newList);
                        }
                    }
                }

                return;
            }
        }

        if (fieldType == typeof(bool) ||
            fieldType == typeof(float) ||
            fieldType == typeof(double) ||
            fieldType == typeof(int) ||
            fieldType == typeof(uint) ||
            fieldType == typeof(string))
        {
            if (parameters)
            {
                switch (fieldType)
                {
                    case Type t when t == typeof(bool):

                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = name,
                            type = SceneComponentParameterType.Bool,
                            boolValue = (bool)getter(),
                        });

                        break;

                    case Type t when t == typeof(float):

                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = name,
                            type = SceneComponentParameterType.Float,
                            floatValue = (float)getter(),
                        });

                        break;

                    case Type t when t == typeof(double):

                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = name,
                            type = SceneComponentParameterType.Float,
                            floatValue = (float)getter(),
                        });

                        break;

                    case Type t when t == typeof(int):

                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = name,
                            type = SceneComponentParameterType.Int,
                            intValue = (int)getter(),
                        });

                        break;

                    case Type t when t == typeof(uint):

                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = name,
                            type = SceneComponentParameterType.Int,
                            intValue = (int)getter(),
                        });

                        break;


                    case Type t when t == typeof(string):

                        sceneComponent.parameters.Add(new SceneComponentParameter()
                        {
                            name = name,
                            type = SceneComponentParameterType.String,
                            stringValue = (string)getter(),
                        });

                        break;
                }
            }
            else
            {
                sceneComponent.data.Add(name, getter());
            }
        }
        else if(fieldType == typeof(Entity))
        {
            var value = getter();

            if(value is Entity entity && entity.IsValid)
            {
                if(parameters)
                {
                    sceneComponent.parameters.Add(new()
                    {
                        name = name,
                        type = SceneComponentParameterType.Int,
                        intValue = entity.Identifier.ID,
                    });
                }
                else
                {
                    sceneComponent.data.Add(name, entity.Identifier.ID);
                }
            }
        }
        else if(fieldType == typeof(IComponent) ||
            fieldType.GetInterface(typeof(IComponent).FullName) != null)
        {
            var value = getter();

            if(value is IComponent component && World.Current.TryGetComponentEntity(component, out var entity))
            {
                if (parameters)
                {
                    sceneComponent.parameters.Add(new()
                    {
                        name = name,
                        type = SceneComponentParameterType.String,
                        stringValue = $"{entity.Identifier.ID}:{component.GetType().FullName}",
                    });
                }
                else
                {
                    sceneComponent.data.Add(name, $"{entity.Identifier.ID}:{component.GetType().FullName}");
                }
            }
        }
        else if (fieldType.IsEnum)
        {
            if (parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
                    type = SceneComponentParameterType.String,
                    stringValue = ((Enum)getter()).ToString()
                });
            }
            else
            {
                sceneComponent.data.Add(name, ((Enum)getter()).ToString());
            }
        }
        else if (fieldType.GetInterface(typeof(IGuidAsset).FullName) != null)
        {
            var guidAsset = (IGuidAsset)getter();

            if (guidAsset != null && (guidAsset.Guid?.Length ?? 0) > 0)
            {
                if (parameters)
                {
                    sceneComponent.parameters.Add(new SceneComponentParameter()
                    {
                        name = name,
                        type = SceneComponentParameterType.String,
                        stringValue = guidAsset.Guid,
                    });
                }
                else
                {
                    sceneComponent.data.Add(name, guidAsset.Guid);
                }
            }
        }
        else if (fieldType == typeof(Vector2))
        {
            var value = (Vector2)getter();

            if (parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
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
                sceneComponent.data.Add(name, new Vector2Holder()
                {
                    x = value.X,
                    y = value.Y,
                });
            }
        }
        else if (fieldType == typeof(Vector3))
        {
            var value = (Vector3)getter();

            if (parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
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
                sceneComponent.data.Add(name, new Vector3Holder()
                {
                    x = value.X,
                    y = value.Y,
                    z = value.Z,
                });
            }
        }
        else if (fieldType == typeof(Vector4))
        {
            var value = (Vector4)getter();

            if (parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
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
                sceneComponent.data.Add(name, new Vector4Holder()
                {
                    x = value.X,
                    y = value.Y,
                    z = value.Z,
                    w = value.W,
                });
            }
        }
        else if (fieldType == typeof(Quaternion))
        {
            var value = (Quaternion)getter();

            if (parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
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
                sceneComponent.data.Add(name, new Vector4Holder()
                {
                    x = value.X,
                    y = value.Y,
                    z = value.Z,
                    w = value.W,
                });
            }
        }
        else if (fieldType == typeof(Vector2Int))
        {
            var value = (Vector2Int)getter();

            if (parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
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
                sceneComponent.data.Add(name, new Vector2Holder()
                {
                    x = value.X,
                    y = value.Y,
                });
            }
        }
        else if (fieldType == typeof(Color32))
        {
            var color = (Color32)getter();

            if (parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
                    type = SceneComponentParameterType.String,
                    stringValue = $"#{color.HexValue}",
                });
            }
            else
            {
                sceneComponent.data.Add(name, $"#{color.HexValue}");
            }
        }
        else if (fieldType == typeof(Color))
        {
            var color = (Color)getter();

            if (parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
                    type = SceneComponentParameterType.String,
                    stringValue = $"#{color.HexValue}",
                });
            }
            else
            {
                sceneComponent.data.Add(name, $"#{color.HexValue}");
            }
        }
        else if (fieldType == typeof(LayerMask))
        {
            var mask = (LayerMask)getter();

            if (parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
                    type = SceneComponentParameterType.Int,
                    intValue = (int)mask.value,
                });
            }
            else
            {
                sceneComponent.data.Add(name, mask.value);
            }
        }
        else if(fieldType == typeof(EntityCallback))
        {
            var callback = (EntityCallback)getter();

            if(callback == null)
            {
                return;
            }

            var pieces = new List<string>();

            foreach(var c in callback.PersistentCallbacks())
            {
                pieces.Add($"{c.entityID}|{c.className}|{c.methodName}");
            }

            var compacted = string.Join(":", pieces);

            if(parameters)
            {
                sceneComponent.parameters.Add(new SceneComponentParameter()
                {
                    name = name,
                    type = SceneComponentParameterType.String,
                    stringValue = compacted,
                });
            }
            else
            {
                sceneComponent.data.Add(name, compacted);
            }
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

            /*
            var properties = component.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    continue;
                }

                try
                {
                    property.SetValue(localComponent, property.GetValue(component));
                }
                catch (Exception)
                {
                }
            }
            */
        });
    }

    /// <summary>
    /// Serializes the components of an entity into a SceneObject
    /// </summary>
    /// <param name="entity">The entity to serialize</param>
    /// <param name="parameters">Whether to store in parameters or data</param>
    /// <returns>The new scene object</returns>
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
            };

            var fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    continue;
                }

                var c = component;

                SerializeProperty(field.FieldType, field.Name, () => field.GetValue(c), sceneComponent, parameters);
            }

            /*
            var properties = component.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                if (property.CanWrite == false || property.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    continue;
                }

                var c = component;

                SerializeProperty(property.PropertyType, property.Name, () => property.GetValue(c), sceneComponent, parameters);
            }
            */

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

        if(entity.TryGetPrefab(out var prefabGuid, out var localID))
        {
            outEntity.prefabGuid = prefabGuid;
            outEntity.prefabLocalID = localID;
        }

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

    /// <summary>
    /// Serializes an entity into a prefab
    /// </summary>
    /// <param name="entity">The entity to serialize</param>
    /// <returns>The prefab data, or null</returns>
    public static SerializablePrefab SerializeIntoPrefab(Entity entity)
    {
        if(entity.IsValid == false ||
            entity.TryGetComponent<Transform>(out var entityTransform) == false)
        {
            return null;
        }

        var outValue = new SerializablePrefab
        {
            mainObject = SerializeEntity(entity, false)
        };

        outValue.mainObject.ID = 0;

        var localIDs = new Dictionary<int, int>();

        void GatherIDs(Transform transform)
        {
            localIDs.Add(transform.entity.Identifier.ID, localIDs.Count);

            foreach(var child in transform.Children)
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

            foreach (var child in transform.Children)
            {
                GatherSceneObjects(child, false);
            }
        }

        GatherSceneObjects(entityTransform, true);

        return outValue;
    }

    /// <summary>
    /// Instantiates a prefab
    /// </summary>
    /// <param name="parent">The parent entity, if any</param>
    /// <param name="prefab">The prefab to instantiate</param>
    /// <returns>The new entity, or Invalid</returns>
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

        newEntity.SetPrefab(prefab.guid, prefab.mainObject.ID);

        var transform = newEntity.GetComponent<Transform>();

        transform.SetParent(parentTransform);

        transform.LocalPosition = prefab.mainObject.transform.position.ToVector3();
        transform.LocalRotation = prefab.mainObject.transform.rotation.ToQuaternion();
        transform.LocalScale = prefab.mainObject.transform.scale.ToVector3();

        var localIDs = new Dictionary<int, int>();

        localIDs.AddOrSetKey(0, newEntity.Identifier.ID);

        var counter = 1;

        var localEntities = new Dictionary<int, Entity>();

        foreach(var sceneObject in prefab.children)
        {
            var childEntity = Scene.Instantiate(sceneObject, out _, true);

            if(childEntity.IsValid)
            {
                localEntities.Add(counter, childEntity);
                localIDs.Add(counter++, childEntity.Identifier.ID);

                if(localIDs.TryGetValue(sceneObject.parent, out var localParentID))
                {
                    var childTransform = childEntity.GetComponent<Transform>();
                    var targetTransform = Scene.FindEntity(localParentID).GetComponent<Transform>();

                    childEntity.SetPrefab(prefab.guid, sceneObject.ID);

                    if (targetTransform != null)
                    {
                        childTransform.SetParent(targetTransform);
                    }
                }
            }
        }

        void HandleReferences(Entity entity, SceneObject sceneObject)
        {
            foreach(var component in sceneObject.components)
            {
                var componentType = TypeCache.GetType(component.type);

                if(componentType == null || entity.TryGetComponent(out var componentInstance, componentType) == false)
                {
                    continue;
                }

                foreach(var parameter in component.parameters)
                {
                    try
                    {
                        var field = componentType.GetField(parameter.name, BindingFlags.Public | BindingFlags.Instance);

                        if (field == null)
                        {
                            continue;
                        }

                        if(field.FieldType == typeof(Entity) && parameter.type == SceneComponentParameterType.Int)
                        {
                            Entity targetEntity = default;

                            if (localIDs.TryGetValue(parameter.intValue, out var localEntityID))
                            {
                                targetEntity = Scene.FindEntity(localEntityID);
                            }

                            if(targetEntity.IsValid)
                            {
                                field.SetValue(componentInstance, targetEntity);
                            }
                        }
                        else if((field.FieldType == typeof(IComponent) ||
                            field.FieldType.GetInterface(typeof(IComponent).FullName) != null) &&
                            parameter.type == SceneComponentParameterType.String)
                        {
                            var pieces = parameter.stringValue.Split(":");

                            if(pieces.Length == 2 &&
                                int.TryParse(pieces[0], out var entityID))
                            {
                                var targetComponentType = TypeCache.GetType(pieces[1]);

                                if(targetComponentType == null ||
                                    targetComponentType.IsAssignableTo(field.FieldType) == false)
                                {
                                    continue;
                                }

                                Entity targetEntity = default;

                                if(localIDs.TryGetValue(entityID, out var localEntityID))
                                {
                                    targetEntity = Scene.FindEntity(localEntityID);
                                }

                                if(targetEntity.IsValid == false ||
                                    targetEntity.TryGetComponent(out var targetComponent, targetComponentType) == false)
                                {
                                    continue;
                                }

                                field.SetValue(componentInstance, targetComponent);
                            }
                        }
                    }
                    catch(Exception e)
                    {
                    }
                }
            }
        }

        HandleReferences(newEntity, prefab.mainObject);

        for(var i = 0; i < prefab.children.Count; i++)
        {
            var sceneObject = prefab.children[i];

            if(localEntities.TryGetValue(i + 1, out var entity) && entity.IsValid)
            {
                HandleReferences(entity, sceneObject);
            }
        }

        return newEntity;
    }
}
