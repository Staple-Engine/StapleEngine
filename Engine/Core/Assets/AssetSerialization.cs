using MessagePack;
using System;
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

        public static string GetAssetPathFromCache(string cachePath)
        {
            var matches = cachePathRegex.Matches(cachePath);

            if (matches.Count > 0)
            {
                return cachePath.Substring(matches[0].Value.Length).Replace(Path.DirectorySeparatorChar, '/');
            }

            return cachePath;
        }

        [GeneratedRegex("(.*?)(\\\\|\\/)Cache(\\\\|\\/)Staging(\\\\|\\/)(.*?)(\\\\|\\/)")]
        private static partial Regex CachePathRegex();

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

            if(type.GetInterface(typeof(IPathAsset).FullName) != null || type == typeof(IPathAsset))
            {
                return true;
            }

            if (type.GetCustomAttribute<MessagePackObjectAttribute>() != null)
            {
                return true;
            }

            if(type.GetInterface(typeof(IPathAsset).FullName) != null)
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

                    if(value is IPathAsset pathAsset)
                    {
                        var path = GetAssetPathFromCache(pathAsset.Path);

                        if ((path?.Length ?? 0) > 0)
                        {
                            outValue.parameters.Add(field.Name, new SerializableStapleAssetParameter()
                            {
                                typeName = value.GetType().FullName,
                                value = path,
                            });
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

        public static object Deserialize(SerializableStapleAsset asset)
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

            object instance;

            try
            {
                instance = Activator.CreateInstance(type);
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

                    if(valueType.GetInterface(typeof(IPathAsset).FullName) != null)
                    {
                        if(pair.Value.value is string path)
                        {
                            var methods = valueType.GetMethods();

                            foreach(var method in methods)
                            {
                                if(method.IsStatic && method.IsPublic && method.Name == "Create")
                                {
                                    var parameters = method.GetParameters();

                                    if(parameters.Length != 1 || parameters[0].Name != "path")
                                    {
                                        continue;
                                    }

                                    try
                                    {
                                        var result = method.Invoke(null, new object[] { path });

                                        if(result == null || (result.GetType() != field.FieldType && result.GetType().GetInterface(field.FieldType.FullName) == null))
                                        {
                                            break;
                                        }

                                        field.SetValue(instance, result);
                                    }
                                    catch(Exception e)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        continue;
                    }

                    field.SetValue(instance, pair.Value.value);
                }
                catch(Exception)
                {
                }
            }

            return instance;
        }
    }
}
