using MessagePack;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Reflection;
using System.Collections;

namespace Staple.Internal;

internal static class StapleSerializer
{
    /// <summary>
    /// Checks whether this is a parameter that can be directly serialized
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>Whether the type can be directly serialized</returns>
    public static bool IsDirectParameter(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
        {
            if (type == typeof(IntPtr) || type == typeof(UIntPtr))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a type is valid for serialization
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>Whether it can be serialized</returns>
    public static bool IsValidType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        if(type == null)
        {
            return false;
        }

        if (IsDirectParameter(type))
        {
            return true;
        }

        if (type.GetInterface(typeof(IGuidAsset).FullName) != null || type == typeof(IGuidAsset))
        {
            return true;
        }

        if (type.GetCustomAttribute<MessagePackObjectAttribute>() != null)
        {
            return true;
        }

        if (type.GetCustomAttribute<SerializableAttribute>() != null)
        {
            return true;
        }

        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var listType = type.GetGenericArguments()[0];

                if (listType != null)
                {
                    return IsValidType(listType);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Serializes a field into an asset container
    /// </summary>
    /// <param name="field">The field to serialize</param>
    /// <param name="instance">The instance of the object we're handling</param>
    /// <param name="container">The container to store into</param>
    public static void SerializeField(FieldInfo field, object instance, SerializableStapleAssetContainer container)
    {
        if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
        {
            return;
        }

        if (field.IsPublic == false && field.GetCustomAttribute<SerializableAttribute>() == null)
        {
            return;
        }

        if (IsValidType(field.FieldType))
        {
            var value = field.GetValue(instance);

            if (value == null)
            {
                return;
            }

            if (value is IGuidAsset guidAsset)
            {
                container.parameters.Add(field.Name, new SerializableStapleAssetParameter()
                {
                    typeName = value.GetType().FullName,
                    value = guidAsset.Guid,
                });

                return;
            }

            if (value.GetType().IsGenericType)
            {
                if (value.GetType().GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = value.GetType().GetGenericArguments()[0];

                    if (listType != null)
                    {
                        if (listType.GetInterface(typeof(IGuidAsset).FullName) != null)
                        {
                            var newList = new List<string>();

                            var inList = (IList)value;

                            foreach (var item in inList)
                            {
                                if (item is IGuidAsset g)
                                {
                                    newList.Add(g.Guid);
                                }
                                else
                                {
                                    newList.Add(null);
                                }
                            }

                            container.parameters.Add(field.Name, new()
                            {
                                typeName = value.GetType().FullName,
                                value = newList,
                            });

                            return;
                        }
                        else if (listType.GetCustomAttribute<SerializableAttribute>() != null)
                        {
                            try
                            {
                                var newList = new List<SerializableStapleAssetContainer>();

                                var inList = (IList)value;

                                foreach (var item in inList)
                                {
                                    try
                                    {
                                        var innerContainer = SerializeContainer(item);

                                        if (innerContainer != null)
                                        {
                                            newList.Add(innerContainer);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }

                                container.parameters.Add(field.Name, new()
                                {
                                    typeName = value.GetType().FullName,
                                    value = newList,
                                });
                            }
                            catch (Exception)
                            {
                            }

                            return;
                        }
                    }
                }
            }

            //Need to describe the item
            if (IsDirectParameter(value.GetType()) == false &&
                value.GetType().GetCustomAttribute<SerializableAttribute>() != null)
            {
                try
                {
                    var innerContainer = SerializeContainer(value);

                    if (innerContainer != null)
                    {
                        container.parameters.Add(field.Name, new()
                        {
                            typeName = value.GetType().FullName,
                            value = innerContainer,
                        });
                    }
                }
                catch (Exception)
                {
                }

                return;
            }

            container.parameters.Add(field.Name, new()
            {
                typeName = value.GetType().FullName,
                value = value,
            });
        }
    }

    /// <summary>
    /// Serializes an object into an asset container
    /// </summary>
    /// <param name="instance">The object instance we're handling</param>
    /// <returns>The container, or null</returns>
    public static SerializableStapleAssetContainer SerializeContainer(object instance)
    {
        if (instance == null)
        {
            return null;
        }

        var outValue = new SerializableStapleAssetContainer()
        {
            typeName = instance.GetType().FullName,
        };

        var fields = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            SerializeField(field, instance, outValue);
        }

        return outValue;
    }

    /// <summary>
    /// Deserializes a container into an object instance
    /// </summary>
    /// <param name="container">The container to deserialize</param>
    /// <returns>the object instance, or null</returns>
    public static object DeserializeContainer(SerializableStapleAssetContainer container)
    {
        var type = TypeCache.GetType(container.typeName);

        if (type == null)
        {
            return null;
        }

        object instance;

        try
        {
            instance = Activator.CreateInstance(type);

            foreach (var pair in container.parameters)
            {
                try
                {
                    var field = type.GetField(pair.Key);
                    var valueType = TypeCache.GetType(pair.Value.typeName);

                    if (valueType == null ||
                        field == null ||
                        (field.FieldType.FullName != pair.Value.typeName && valueType.GetInterface(field.FieldType.FullName) == null))
                    {
                        continue;
                    }

                    if (valueType.GetInterface(typeof(IGuidAsset).FullName) != null)
                    {
                        if (pair.Value.value is string guid)
                        {
                            var result = AssetSerialization.GetGuidAsset(valueType, guid);

                            if (result == null || (result.GetType() != field.FieldType && result.GetType().GetInterface(field.FieldType.FullName) == null))
                            {
                                break;
                            }

                            field.SetValue(instance, result);
                        }

                        continue;
                    }

                    if (pair.Value.value != null && field.FieldType == pair.Value.value.GetType())
                    {
                        field.SetValue(instance, pair.Value.value);

                        continue;
                    }

                    if (field.FieldType.IsGenericType)
                    {
                        if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var o = Activator.CreateInstance(field.FieldType);

                            if (o == null)
                            {
                                continue;
                            }

                            var list = (IList)o;

                            if (list == null)
                            {
                                continue;
                            }

                            var fail = false;

                            if (pair.Value.value is object[] array)
                            {
                                foreach (var item in array)
                                {
                                    var fieldType = field.FieldType.GenericTypeArguments[0];

                                    if (fieldType.GetInterface(typeof(IGuidAsset).FullName) != null)
                                    {
                                        if (item is string guid)
                                        {
                                            var v = AssetSerialization.GetGuidAsset(fieldType, guid);

                                            if (v != null)
                                            {
                                                list.Add(v);
                                            }
                                        }
                                    }
                                    else if (fieldType.GetCustomAttribute<SerializableAttribute>() != null)
                                    {
                                        try
                                        {
                                            string innerTypeName = null;
                                            Dictionary<object, object> parameters = null;

                                            if (item is Dictionary<object, object> itemData &&
                                                itemData.Count == 2 &&
                                                itemData.ContainsKey("typeName") &&
                                                itemData.ContainsKey("parameters") &&
                                                itemData["typeName"] is string &&
                                                itemData["parameters"] is Dictionary<object, object>)
                                            {
                                                innerTypeName = (string)itemData["typeName"];
                                                parameters = (Dictionary<object, object>)itemData["parameters"];
                                            }
                                            //TODO: Check if still usable. Probably not.
                                            else if (item is object[] containers &&
                                                containers.Length == 2 &&
                                                containers[0] is string &&
                                                containers[1] is Dictionary<object, object>) //Name and parameters
                                            {
                                                innerTypeName = (string)containers[0];
                                                parameters = (Dictionary<object, object>)containers[1];
                                            }

                                            if (innerTypeName != null && parameters != null)
                                            {
                                                var itemContainer = new SerializableStapleAssetContainer()
                                                {
                                                    typeName = innerTypeName,
                                                };

                                                var containerParameters = new Dictionary<string, SerializableStapleAssetParameter>();

                                                foreach (var containerPair in parameters)
                                                {
                                                    string containerKey = null;
                                                    string parameterTypeName = null;
                                                    object parameterValue = null;

                                                    if (containerPair.Key is string &&
                                                        containerPair.Value is Dictionary<object, object> containerData &&
                                                        containerData.Count == 2 &&
                                                        containerData.ContainsKey("typeName") &&
                                                        containerData.ContainsKey("value") &&
                                                        containerData["typeName"] is string)
                                                    {
                                                        containerKey = (string)containerPair.Key;
                                                        parameterTypeName = (string)containerData["typeName"];
                                                        parameterValue = containerData["value"];
                                                    }
                                                    //TODO: Check if still usable. Probably not.
                                                    else if (containerPair.Key is string &&
                                                        containerPair.Value is object[] pieces &&
                                                        pieces.Length == 2 &&
                                                        pieces[0] is string)
                                                    {
                                                        containerKey = (string)containerPair.Key;
                                                        parameterTypeName = (string)((object[])containerPair.Value)[0];
                                                        parameterValue = (string)((object[])containerPair.Value)[1];
                                                    }

                                                    if (containerKey != null &&
                                                        parameterTypeName != null &&
                                                        parameterValue != null)
                                                    {
                                                        var tempParameter = new SerializableStapleAssetParameter()
                                                        {
                                                            typeName = parameterTypeName,
                                                            value = parameterValue,
                                                        };

                                                        containerParameters.Add(containerKey, tempParameter);
                                                    }
                                                }

                                                itemContainer.parameters = containerParameters;

                                                var itemInstance = DeserializeContainer(itemContainer);

                                                if (itemInstance != null)
                                                {
                                                    list.Add(itemInstance);
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            var value = Convert.ChangeType(item, field.FieldType.GenericTypeArguments[0]);

                                            list.Add(value);
                                        }
                                        catch (Exception)
                                        {
                                            fail = true;

                                            break;
                                        }
                                    }
                                }

                                if (fail)
                                {
                                    continue;
                                }

                                field.SetValue(instance, list);
                            }

                            continue;
                        }
                    }

                    if (field.FieldType == typeof(SerializableStapleAssetContainer) &&
                        field.GetValue(pair.Value.value) is SerializableStapleAssetContainer innerContainer)
                    {
                        try
                        {
                            var value = DeserializeContainer(innerContainer);

                            if (value != null)
                            {
                                field.SetValue(instance, value);
                            }
                        }
                        catch (Exception)
                        {
                        }

                        continue;
                    }

                    field.SetValue(instance, pair.Value.value);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to load an asset of type {container.typeName}: {e}");
                }
            }
        }
        catch (Exception)
        {
            return null;
        }

        return instance;
    }
}
