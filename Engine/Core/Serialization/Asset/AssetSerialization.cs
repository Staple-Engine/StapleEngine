using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Staple.Internal;

/// <summary>
/// Handles serialization for Staple Assets
/// </summary>
internal static partial class AssetSerialization
{
    private static Regex cachePathRegex = CachePathRegex();
    private static Regex assetPathRegex = AssetPathRegex();

    public static readonly string[] TextureExtensions =
    [
        "bmp",
        "dds",
        "exr",
        "gif",
        "jpg",
        "jpeg",
        "hdr",
        "ktx",
        "png",
        "psd",
        "pvr",
        "tga"
    ];

    public static readonly string[] ResizableTextureExtensions =
    [
        "jpg",
        "jpeg",
        "png",
        "tga",
        "bmp",
        "gif",
        "hdr",
    ];

    public static readonly string[] MeshExtensions =
    [
        "3ds",
        "ase",
        "bvh",
        "dae",
        "fbx",
        "glb",
        "gltf",
        "ms3d",
        "obj",
        "ply",
        "stl",
    ];

    public static readonly string[] AudioExtensions =
    [
        "mp3",
        "ogg",
        "wav",
    ];

    public static string GetAssetPathFromCache(string path)
    {
        var matches = cachePathRegex.Matches(path);

        if (matches.Count > 0)
        {
            return path.Substring(matches[0].Value.Length).Replace(Path.DirectorySeparatorChar, '/');
        }

        matches = assetPathRegex.Matches(path);

        if (matches.Count > 0)
        {
            return path.Substring(matches[0].Value.Length).Replace(Path.DirectorySeparatorChar, '/');
        }

        return path;
    }

    public static object GetGuidAsset(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        Type type, string guid)
    {
        var methods = type.GetMethods();

        foreach (var method in methods)
        {
            if (method.IsStatic && method.IsPublic && method.Name == "Create")
            {
                var parameters = method.GetParameters();

                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                {
                    continue;
                }

                try
                {
                    var result = method.Invoke(null, new object[] { guid });

                    if (result == null || (result.GetType() != type && result.GetType().GetInterface(type.FullName) == null))
                    {
                        break;
                    }

                    return result;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }

        return null;
    }

    [GeneratedRegex("(.*?)(\\\\|\\/)Cache(\\\\|\\/)Staging(\\\\|\\/)(.*?)(\\\\|\\/)")]
    private static partial Regex CachePathRegex();

    [GeneratedRegex("(.*?)(\\\\|\\/)Assets(\\\\|\\/)(.*?)")]
    private static partial Regex AssetPathRegex();

    private static bool IsDirectParameter(Type type)
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
    private static bool IsValidType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        if(IsDirectParameter(type))
        {
            return true;
        }

        if(type.GetInterface(typeof(IGuidAsset).FullName) != null || type == typeof(IGuidAsset))
        {
            return true;
        }

        if (type.GetCustomAttribute<MessagePackObjectAttribute>() != null)
        {
            return true;
        }

        if(type.GetCustomAttribute<SerializableAttribute>() != null)
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

    private static SerializableStapleAssetContainer SerializeContainer(object instance)
    {
        if (instance == null)
        {
            return default;
        }

        var outValue = new SerializableStapleAssetContainer()
        {
            typeName = instance.GetType().FullName,
        };


        var fields = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
            {
                continue;
            }

            if (field.IsPublic == false && field.GetCustomAttribute<SerializableAttribute>() == null)
            {
                continue;
            }

            if (IsValidType(field.FieldType))
            {
                var value = field.GetValue(instance);

                if (value == null)
                {
                    continue;
                }

                if (value is IGuidAsset guidAsset)
                {
                    outValue.parameters.Add(field.Name, new SerializableStapleAssetParameter()
                    {
                        typeName = value.GetType().FullName,
                        value = guidAsset.Guid,
                    });

                    continue;
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
                                }

                                outValue.parameters.Add(field.Name, new SerializableStapleAssetParameter()
                                {
                                    typeName = value.GetType().FullName,
                                    value = newList,
                                });

                                continue;
                            }
                            else if (listType.GetCustomAttribute<SerializableAttribute>() != null)
                            {
                                try
                                {
                                    var newList = new List<SerializableStapleAssetContainer>();

                                    var inList = (IList)value;

                                    foreach(var item in inList)
                                    {
                                        try
                                        {
                                            var container = SerializeContainer(item);

                                            if(container != null)
                                            {
                                                newList.Add(container);
                                            }
                                        }
                                        catch(Exception)
                                        {
                                        }
                                    }

                                    outValue.parameters.Add(field.Name, new SerializableStapleAssetParameter()
                                    {
                                        typeName = value.GetType().FullName,
                                        value = newList,
                                    });
                                }
                                catch (Exception)
                                {
                                }

                                continue;
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
                        var container = SerializeContainer(value);

                        if(container != null)
                        {
                            outValue.parameters.Add(field.Name, new SerializableStapleAssetParameter()
                            {
                                typeName = value.GetType().FullName,
                                value = container,
                            });
                        }
                    }
                    catch(Exception)
                    {
                    }

                    continue;
                }

                outValue.parameters.Add(field.Name, new SerializableStapleAssetParameter()
                {
                    typeName = value.GetType().FullName,
                    value = value,
                });
            }
        }

        return outValue;
    }

    /// <summary>
    /// Attempts to serialize a Staple Asset into a SerializableStapleAsset
    /// </summary>
    /// <param name="instance">The object's instance. The object must implement IStapleAsset</param>
    /// <returns>The SerializableStapleAsset, or null</returns>
    public static SerializableStapleAsset Serialize(object instance)
    {
        if(instance == null || instance.GetType().GetInterface(typeof(IStapleAsset).FullName) == null)
        {
            return default;
        }

        try
        {
            var container = SerializeContainer(instance);

            if (container == null)
            {
                return default;
            }

            var outValue = new SerializableStapleAsset()
            {
                typeName = instance.GetType().FullName,
                parameters = container.parameters,
            };

            return outValue;
        }
        catch(Exception e)
        {
            Log.Debug($"[AssetSerialization] Failed to serialize {instance.GetType().FullName}: {e}");

            return default;
        }
    }

    private static object DeserializeContainer(SerializableStapleAssetContainer container)
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
                            var result = GetGuidAsset(valueType, guid);

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
                                            var v = GetGuidAsset(fieldType, guid);

                                            if (v != null)
                                            {
                                                list.Add(v);
                                            }
                                        }
                                    }
                                    else if(fieldType.GetCustomAttribute<SerializableAttribute>() != null)
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

                                                    if(containerPair.Key is string &&
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

                                                    if(containerKey != null &&
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

                    if(field.FieldType == typeof(SerializableStapleAssetContainer) &&
                        field.GetValue(pair.Value.value) is SerializableStapleAssetContainer innerContainer)
                    {
                        try
                        {
                            var value = DeserializeContainer(innerContainer);

                            if(value != null)
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

    public static IStapleAsset Deserialize(SerializableStapleAsset asset)
    {
        if(asset == null)
        {
            return null;
        }

        var instance = DeserializeContainer(new SerializableStapleAssetContainer()
        {
            parameters = asset.parameters,
            typeName = asset.typeName,
        });

        if(instance is IStapleAsset stapleAsset)
        {
            if(stapleAsset is IGuidAsset guidAsset)
            {
                guidAsset.Guid = asset.guid;
            }

            return stapleAsset;
        }

        return null;
    }
}
