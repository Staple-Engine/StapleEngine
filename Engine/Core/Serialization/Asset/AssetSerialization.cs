using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Staple.Internal
{
    /// <summary>
    /// Handles serialization for Staple Assets
    /// </summary>
    internal static partial class AssetSerialization
    {
        private static Regex cachePathRegex = CachePathRegex();
        private static Regex assetPathRegex = AssetPathRegex();

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

                    if (parameters.Length != 1 || parameters[0].Name != "path")
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

        /// <summary>
        /// Checks whether a type is valid for serialization
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>Whether it can be serialized</returns>
        private static bool IsValidType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            {
                if (type == typeof(IntPtr) || type == typeof(UIntPtr))
                {
                    return false;
                }

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

            var outValue = new SerializableStapleAsset()
            {
                typeName = instance.GetType().FullName,
            };

            var fields = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if(field.GetCustomAttribute<NonSerializedAttribute>() != null)
                {
                    continue;
                }

                if(field.IsPublic == false && field.GetCustomAttribute<SerializableAttribute>() == null)
                {
                    continue;
                }

                if(IsValidType(field.FieldType))
                {
                    var value = field.GetValue(instance);

                    if(value == null)
                    {
                        continue;
                    }

                    if(value is IGuidAsset guidAsset)
                    {
                        outValue.parameters.Add(field.Name, new SerializableStapleAssetParameter()
                        {
                            typeName = value.GetType().FullName,
                            value = guidAsset.Guid,
                        });

                        continue;
                    }

                    if(value.GetType().IsGenericType)
                    {
                        if (value.GetType().GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var listType = value.GetType().GetGenericArguments()[0];

                            if (listType != null)
                            {
                                if(listType.GetInterface(typeof(IGuidAsset).FullName) != null)
                                {
                                    var newList = new List<string>();

                                    var inList = (IList)value;

                                    foreach(var item in inList)
                                    {
                                        if(item is IGuidAsset g)
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
                            }
                        }
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

        public static IStapleAsset Deserialize(SerializableStapleAsset asset)
        {
            if(asset == null)
            {
                return null;
            }

            var type = TypeCache.GetType(asset.typeName);

            if(type == null)
            {
                return null;
            }

            IStapleAsset instance;

            try
            {
                instance = (IStapleAsset)Activator.CreateInstance(type);
            }
            catch(Exception)
            {
                return null;
            }

            foreach(var pair in asset.parameters)
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

                    if(valueType.GetInterface(typeof(IGuidAsset).FullName) != null)
                    {
                        if(pair.Value.value is string guid)
                        {
                            var result = GetGuidAsset(valueType, guid);

                            if(result == null || (result.GetType() != field.FieldType && result.GetType().GetInterface(field.FieldType.FullName) == null))
                            {
                                break;
                            }

                            field.SetValue(instance, result);
                        }

                        continue;
                    }

                    if(pair.Value.value != null && field.FieldType == pair.Value.value.GetType())
                    {
                        field.SetValue(instance, pair.Value.value);

                        continue;
                    }

                    if(field.FieldType.IsGenericType)
                    {
                        if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var o = Activator.CreateInstance(field.FieldType);

                            if(o == null)
                            {
                                continue;
                            }

                            var list = (IList)o;

                            if(list == null)
                            {
                                continue;
                            }

                            var fail = false;

                            if(pair.Value.value is object[] array)
                            {
                                foreach(var item in array)
                                {
                                    var fieldType = field.FieldType.GenericTypeArguments[0];

                                    if(fieldType.GetInterface(typeof(IGuidAsset).FullName) != null)
                                    {
                                        if(item is string guid)
                                        {
                                            var v = GetGuidAsset(fieldType, guid);

                                            if(v != null)
                                            {
                                                list.Add(v);
                                            }
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

                                if(fail)
                                {
                                    continue;
                                }

                                field.SetValue(instance, list);
                            }

                            continue;
                        }
                    }

                    field.SetValue(instance, pair.Value.value);
                }
                catch(Exception e)
                {
                    Log.Error($"Failed to load an asset of type {asset.typeName}: {e}");
                }
            }

            return instance;
        }
    }
}
