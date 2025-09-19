using MessagePack;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Reflection;
using System.Collections;
using System.Linq;
using System.Text.Json;
using System.Numerics;

namespace Staple.Internal;

internal static class StapleSerializer
{
    private static readonly IStapleTypeSerializer[] TypeSerializers = [

        new StapleBaseTypeSerializer(),
        new StapleTypesSerializer(),
    ];

    private static readonly string LogTag = "Serialization";

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
        if (type == null)
        {
            return false;
        }

        if(type.GetCustomAttribute<SerializeInEditorAttribute>() != null && Platform.IsEditor == false)
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

        if(type.IsAssignableTo(typeof(IComponent)))
        {
            return true;
        }

        if(type == typeof(Entity))
        {
            return true;
        }

        if(type == typeof(Sprite))
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

        if(type.IsArray && IsValidType(type.GetElementType()))
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

        return type switch
        {
            Type t when t == typeof(Vector2) => true,
            Type t when t == typeof(Vector3) => true,
            Type t when t == typeof(Vector4) => true,
            Type t when t == typeof(Quaternion) => true,
            _ => false,
        };
    }

    /// <summary>
    /// Serializes a primitive array
    /// </summary>
    /// <param name="array">The array to serialize</param>
    /// <returns>The serialized bytes</returns>
    public static byte[] SerializePrimitiveArray(Array array)
    {
        if(array.GetType().GetElementType() == typeof(bool))
        {
            bool[] boolArray = (bool[])array;

            return boolArray.Select(x => (byte)(x ? 1 : 0)).ToArray();
        }

        var size = TypeCache.SizeOf(array.GetType().GetElementType().ToString());

        if(size <= 0)
        {
            return null;
        }

        var buffer = new byte[size * array.Length];

        Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);

        return buffer;
    }

    /// <summary>
    /// Deserializes a buffer of bytes into a primitive array
    /// </summary>
    /// <param name="buffer">The buffer</param>
    /// <param name="target">The target array</param>
    public static void DeserializePrimitiveArray(byte[] buffer, Array target)
    {
        if (target.GetType().GetElementType() == typeof(bool))
        {
            if(buffer.Length != target.Length)
            {
                return;
            }

            for(var i = 0; i < buffer.Length; i++)
            {
                target.SetValue(buffer[i] == 1, i);
            }

            return;
        }

        var size = TypeCache.SizeOf(target.GetType().GetElementType().ToString());

        if (size <= 0)
        {
            return;
        }

        if(buffer.Length % size != 0)
        {
            return;
        }

        Buffer.BlockCopy(buffer, 0, target, 0, buffer.Length);
    }

    /// <summary>
    /// Serializes a primitive enumerable
    /// </summary>
    /// <param name="list">The array to serialize</param>
    /// <param name="elementType">The element type</param>
    /// <returns>The serialized bytes</returns>
    public static byte[] SerializePrimitiveList(IList list, Type elementType)
    {
        if (elementType == typeof(bool))
        {
            var boolArray = new List<byte>();

            foreach(bool value in list)
            {
                boolArray.Add((byte)(value ? 1 : 0));
            }

            return boolArray.ToArray();
        }

        var size = TypeCache.SizeOf(elementType.ToString());

        if (size <= 0)
        {
            return null;
        }

        switch (elementType)
        {
            case Type t when t == typeof(byte):

                {
                    var l = (List<byte>)list;

                    return l.ToArray();
                }

            case Type t when t == typeof(sbyte):

                {
                    var l = (List<sbyte>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }

            case Type t when t == typeof(ushort):

                {
                    var l = (List<ushort>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }

            case Type t when t == typeof(short):

                {
                    var l = (List<short>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }

            case Type t when t == typeof(uint):

                {
                    var l = (List<uint>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }

            case Type t when t == typeof(int):

                {
                    var l = (List<int>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }

            case Type t when t == typeof(ulong):

                {
                    var l = (List<ulong>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }

            case Type t when t == typeof(long):

                {
                    var l = (List<long>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }

            case Type t when t == typeof(char):

                {
                    var l = (List<char>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }

            case Type t when t == typeof(float):

                {
                    var l = (List<float>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }

            case Type t when t == typeof(double):

                {
                    var l = (List<double>)list;

                    var buffer = new byte[size * l.Count];

                    Buffer.BlockCopy(l.ToArray(), 0, buffer, 0, buffer.Length);

                    return buffer;
                }
        }

        return null;
    }

    /// <summary>
    /// Serializes a field into an asset container
    /// </summary>
    /// <param name="field">The field to serialize</param>
    /// <param name="instance">The instance of the object we're handling</param>
    /// <param name="context">The serializer context to store into</param>
    /// <param name="mode">The serialization mode we want to use</param>
    public static void SerializeField(FieldInfo field, object instance, StapleSerializerContext context, StapleSerializationMode mode)
    {
        if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
        {
            return;
        }

        if (field.IsPublic == false && field.GetCustomAttribute<SerializeFieldAttribute>() == null)
        {
            return;
        }

        if (field.FieldType.GetCustomAttribute<SerializeInEditorAttribute>() != null && Platform.IsEditor == false)
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

            foreach(var s in TypeSerializers)
            {
                if(s.HandlesType(field.FieldType))
                {
                    var o = mode != StapleSerializationMode.Binary ? s.SerializeJsonField(value, instance.GetType(), field, field.FieldType, mode) :
                        s.SerializeField(value, instance.GetType(), field, field.FieldType, mode);

                    if(o != null)
                    {
                        context.setField(field, value.GetType().ToString(), o);

                        return;
                    }
                }
            }

            if(field.FieldType.IsArray && IsValidType(field.FieldType.GetElementType()))
            {
                var array = (Array)value;

                var elementType = field.FieldType.GetElementType();

                if (elementType.GetInterface(typeof(IGuidAsset).FullName) != null ||
                    elementType == typeof(IGuidAsset))
                {
                    var assetList = new List<string>();

                    foreach(var item in array)
                    {
                        if(item is IGuidAsset asset)
                        {
                            //Normalize the asset path in case the guid is actually a path
                            assetList.Add(AssetSerialization.GetAssetPathFromCache(asset.Guid.Guid));
                        }
                        else
                        {
                            assetList.Add(null);
                        }
                    }

                    context.setField(field, field.FieldType.ToString(), assetList.ToArray());
                }
                else if(elementType == typeof(string))
                {
                    context.setField(field, field.FieldType.ToString(), array);
                }
                else if(elementType.IsPrimitive)
                {
                    if(mode != StapleSerializationMode.Binary && field.GetCustomAttribute<SerializeAsHexAttribute>() != null)
                    {
                        var buffer = SerializePrimitiveArray(array);

                        if (buffer != null)
                        {
                            context.setField(field, field.FieldType.ToString(), Convert.ToHexString(buffer));
                        }
                    }
                    else
                    {
                        context.setField(field, field.FieldType.ToString(), value);
                    }
                }
                else if(elementType.GetCustomAttribute<SerializableAttribute>() != null)
                {
                    try
                    {
                        var newList = new List<object>();

                        foreach (var item in array)
                        {
                            try
                            {
                                var container = SerializeContainer(item, mode);

                                newList.Add(container);
                            }
                            catch (Exception e)
                            {
                                Log.Debug($"[{LogTag}] Failed to deserialize a {elementType.ToString()} element: {e}");
                            }
                        }

                        context.setField(field, field.FieldType.ToString(), newList);
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"[{LogTag}] Failed to deserialize a {elementType.ToString()} list: {e}");
                    }
                }
                else if(elementType.IsEnum)
                {
                    if(elementType.GetCustomAttribute<FlagsAttribute>() != null)
                    {
                        var newList = new List<long>();

                        foreach(var item in array)
                        {
                            newList.Add((long)item);
                        }

                        context.setField(field, field.FieldType.ToString(), newList);
                    }
                    else
                    {
                        var newList = new List<string>();

                        foreach (var item in array)
                        {
                            newList.Add(item.ToString());
                        }

                        context.setField(field, field.FieldType.ToString(), newList);
                    }
                }
                else
                {
                    foreach(var s in TypeSerializers)
                    {
                        if(s.HandlesType(elementType))
                        {
                            var newList = new List<object>();

                            foreach(var item in array)
                            {
                                var o = mode != StapleSerializationMode.Binary ? s.SerializeJsonField(item, instance.GetType(), field, elementType, mode) :
                                    s.SerializeField(item, instance.GetType(), field, elementType, mode);

                                if(o != null)
                                {
                                    newList.Add(o);
                                }
                            }

                            context.setField(field, field.FieldType.ToString(), newList);

                            return;
                        }
                    }
                }

                return;
            }

            if (field.FieldType.IsGenericType)
            {
                if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = field.FieldType.GetGenericArguments()[0];

                    if (listType != null)
                    {
                        if (listType.GetInterface(typeof(IGuidAsset).FullName) != null ||
                            listType == typeof(IGuidAsset))
                        {
                            var newList = new List<string>();

                            var inList = (IList)value;

                            foreach (var item in inList)
                            {
                                if (item is IGuidAsset g)
                                {
                                    //Normalize the asset path in case the guid is actually a path
                                    newList.Add(AssetSerialization.GetAssetPathFromCache(g.Guid.Guid));
                                }
                                else
                                {
                                    newList.Add(null);
                                }
                            }

                            context.setField(field, field.FieldType.ToString(), newList);
                        }
                        else if (listType == typeof(string))
                        {
                            context.setField(field, field.FieldType.ToString(), value);
                        }
                        else if (listType.IsPrimitive)
                        {
                            if (mode != StapleSerializationMode.Binary && field.GetCustomAttribute<SerializeAsHexAttribute>() != null)
                            {
                                var list = (IList)value;

                                var buffer = SerializePrimitiveList(list, listType);

                                if (buffer != null)
                                {
                                    context.setField(field, field.FieldType.ToString(), Convert.ToHexString(buffer));
                                }
                            }
                            else
                            {
                                context.setField(field, field.FieldType.ToString(), value);
                            }
                        }
                        else if (listType.GetCustomAttribute<SerializableAttribute>() != null)
                        {
                            try
                            {
                                var newList = new List<object>();

                                var inList = (IList)value;

                                foreach (var item in inList)
                                {
                                    try
                                    {
                                        var container = SerializeContainer(item, mode);

                                        if(container != null)
                                        {
                                            if(mode == StapleSerializationMode.Scene)
                                            {
                                                newList.Add(container.ToRawContainer());
                                            }
                                            else
                                            {
                                                newList.Add(container);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Debug($"[{LogTag}] Failed to deserialize a {listType.ToString()} list element: {e}");
                                    }
                                }

                                context.setField(field, field.FieldType.ToString(), newList);
                            }
                            catch (Exception e)
                            {
                                Log.Debug($"[{LogTag}] Failed to deserialize a {listType.ToString()} list: {e}");
                            }
                        }
                        else if(listType.IsEnum)
                        {
                            if(listType.GetCustomAttribute<FlagsAttribute>() != null)
                            {
                                var newList = new List<long>();

                                var inList = (IList)value;

                                foreach (var item in inList)
                                {
                                    newList.Add((long)item);
                                }

                                context.setField(field, field.FieldType.ToString(), newList);
                            }
                            else
                            {
                                var newList = new List<string>();

                                var inList = (IList)value;

                                foreach (var item in inList)
                                {
                                    newList.Add(item.ToString());
                                }

                                context.setField(field, field.FieldType.ToString(), newList);
                            }
                        }

                        return;
                    }
                }
            }

            //Need to describe the item
            if (IsDirectParameter(value.GetType()) == false &&
                field.FieldType.GetCustomAttribute<SerializableAttribute>() != null)
            {
                try
                {
                    var innerContainer = SerializeContainer(value, mode);

                    if (innerContainer != null)
                    {
                        if (mode == StapleSerializationMode.Scene)
                        {
                            context.setField(field, field.FieldType.ToString(), innerContainer.ToRawContainer());
                        }
                        else
                        {
                            context.setField(field, field.FieldType.ToString(), innerContainer);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Debug($"[{LogTag}] Failed to deserialize a {field.FieldType.ToString()} container: {e}");
                }

                return;
            }

            context.setField(field, value.GetType().ToString(), value);
        }
    }

    /// <summary>
    /// Serializes an object into an asset container
    /// </summary>
    /// <param name="instance">The object instance we're handling</param>
    /// <param name="mode">The serialization mode we want to use</param>
    /// <returns>The container, or null</returns>
    public static StapleSerializerContainer SerializeContainer(object instance, StapleSerializationMode mode)
    {
        if (instance == null || instance.GetType().IsNestedFamORAssem ||
            (instance.GetType().GetCustomAttribute<SerializeInEditorAttribute>() != null && Platform.IsEditor == false))
        {
            return null;
        }

        var outValue = new StapleSerializerContainer()
        {
            typeName = instance.GetType().ToString(),
        };

        var context = new StapleSerializerContext(() => instance,
            (field, typeName, value) => outValue.fields.Add(field.Name, new()
            {
                typeName = typeName,
                value = value,
            }));

        var fields = instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if(field.IsFamilyOrAssembly ||
                field.FieldType.IsNestedFamORAssem ||
                field.FieldType.IsNestedAssembly)
            {
                continue;
            }

            SerializeField(field, instance, context, mode);
        }

        return outValue;
    }

    private static object GetArrayValue(StapleSerializerField fieldInfo, Type type, FieldInfo field, StapleSerializationMode mode)
    {
        var condensed = fieldInfo.value is JsonElement jsonElement ?
            GetJsonArray(type, field, TypeCache.GetType(fieldInfo.typeName), jsonElement, mode) : fieldInfo.value;

        object sourceValue = field.GetCustomAttribute<SerializeAsHexAttribute>() != null && condensed is string s ?
            s : condensed is object[] a ? a : condensed is List<string> l ? l : null;

        if (sourceValue is List<string> li)
        {
            var sourceArray = new object[li.Count];

            for (var i = 0; i < li.Count; i++)
            {
                sourceArray[i] = li[i];
            }

            return sourceArray;
        }

        return sourceValue;
    }

    private static object[] GetJsonArray(Type type, FieldInfo field, Type fieldType, JsonElement element, StapleSerializationMode mode)
    {
        if(element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var elementType = fieldType.IsArray ? fieldType.GetElementType() :
            fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>) ? fieldType.GetGenericArguments()[0] : null;

        if(elementType == null)
        {
            return null;
        }

        var array = new object[element.GetArrayLength()];

        for(var i = 0; i < element.GetArrayLength(); i++)
        {
            var o = GetJsonValue(type, field, elementType, element[i], mode);

            if(o != null)
            {
                array.SetValue(o, i);
            }
        }

        return array;
    }

    private static object GetJsonValue(Type type, FieldInfo fieldInfo, Type fieldType, JsonElement element, StapleSerializationMode mode)
    {
        foreach(var s in TypeSerializers)
        {
            if(s.HandlesType(fieldType))
            {
                return s.DeserializeJsonField(type, fieldInfo, fieldType, null, element, mode);
            }
        }

        if (IsDirectParameter(fieldType) == false &&
            fieldType.GetCustomAttribute<SerializableAttribute>() != null &&
            element.ValueKind == JsonValueKind.Object)
        {
            var container = new StapleSerializerContainer()
            {
                typeName = fieldType.ToString(),
            };

            var fields = new Dictionary<string, StapleSerializerField>();

            foreach(var p in element.EnumerateObject())
            {
                var localField = fieldType.GetField(p.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if(localField == null)
                {
                    continue;
                }

                fields.Add(p.Name, new()
                {
                    typeName = localField.FieldType.ToString(),
                    value = p.Value,
                });
            }

            container.fields = fields;

            return DeserializeContainer(container, mode);
        }

        return null;
    }

    private static object DeserializeField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo, StapleSerializationMode mode)
    {
        try
        {
            var valueType = TypeCache.GetType(fieldInfo.typeName);

            if (valueType == null ||
                field == null ||
                fieldInfo.value == null ||
                ((field.GetCustomAttribute<SerializeInEditorAttribute>() != null ||
                fieldType.GetCustomAttribute<SerializeInEditorAttribute>() != null) && Platform.IsEditor == false) ||
                (fieldType.ToString() != fieldInfo.typeName && valueType.GetInterface(fieldType.FullName) == null))
            {
                return null;
            }

            if (valueType.GetInterface(typeof(IGuidAsset).FullName) != null ||
                valueType == typeof(IGuidAsset))
            {
                string v = null;

                if (fieldInfo.value is string str)
                {
                    v = str;
                }
                else if (fieldInfo.value is JsonElement element && element.ValueKind == JsonValueKind.String)
                {
                    v = element.GetString();
                }

                if (v is string guid)
                {
                    var result = AssetSerialization.GetGuidAsset(valueType, guid);

                    if (result == null || (result.GetType() != fieldType && result.GetType().GetInterface(fieldType.FullName) == null))
                    {
                        return null;
                    }

                    return result;
                }

                return null;
            }

            if (fieldInfo.value.GetType().IsAssignableTo(fieldType))
            {
                return fieldInfo.value;
            }
            else if (fieldInfo.value is JsonElement jsonElement &&
                GetJsonValue(type, field, fieldType, jsonElement, mode) is object jsonObject &&
                (fieldType == jsonObject.GetType() ||
                jsonObject.GetType().IsAssignableTo(fieldType)))
            {
                return jsonObject;
            }
            else if (fieldType.IsPrimitive)
            {
                if (fieldInfo.value is JsonElement element)
                {
                    var o = GetJsonValue(type, field, fieldType, element, mode);

                    return o;
                }

                return Convert.ChangeType(fieldInfo.value, fieldType);
            }
            else if(TypeSerializers.Any(x => x.HandlesType(fieldType)))
            {
                foreach(var s in TypeSerializers)
                {
                    if(s.HandlesType(fieldType))
                    {
                        return fieldInfo.value is JsonElement element ? s.DeserializeJsonField(type, field, fieldType, fieldInfo, element, mode) :
                            s.DeserializeField(type, field, fieldType, fieldInfo, mode);
                    }
                }
            }
            else if (IsDirectParameter(fieldType.GetType()) == false &&
                fieldType.GetCustomAttribute<SerializableAttribute>() != null)
            {
                {
                    if (fieldInfo.value is Dictionary<object, object> pairs &&
                        pairs.TryGetValue(nameof(SerializableStapleAssetContainer.typeName), out var t) &&
                        t is string typeName &&
                        pairs.TryGetValue(nameof(SerializableStapleAssetContainer.fields), out var p) &&
                        p is Dictionary<object, object> fields)
                    {
                        try
                        {
                            var decodedContainer = DecodeContainer(typeName, fields);

                            var value = DeserializeContainer(decodedContainer, mode);

                            return value;
                        }
                        catch (Exception e)
                        {
                            Log.Debug($"[{LogTag}] Failed to deserialize a container of type {typeName}: {e}");

                            return null;
                        }
                    }
                }

                {
                    if (fieldInfo.value is JsonElement element &&
                        element.ValueKind == JsonValueKind.Object &&
                        element.TryGetProperty(nameof(SerializableStapleAssetContainer.typeName), out var typeNameProperty) &&
                        typeNameProperty.ValueKind == JsonValueKind.String &&
                        typeNameProperty.GetString() is string typeName &&
                        element.TryGetProperty(nameof(SerializableStapleAssetContainer.fields), out var fieldsProperty) &&
                        fieldsProperty.ValueKind == JsonValueKind.Object)
                    {
                        //TODO

                        return null;
                    }
                }
            }

            if (fieldType.IsArray && IsValidType(fieldType.GetElementType()))
            {
                Array newValue = null;

                var elementType = fieldType.GetElementType();

                var sourceValue = GetArrayValue(fieldInfo, type, field, mode);

                if (sourceValue is Array array)
                {
                    try
                    {
                        if (sourceValue is not string[] &&
                            fieldType.GetElementType().IsPrimitive &&
                            fieldType.GetElementType() != typeof(bool))
                        {
                            var size = TypeCache.SizeOf(elementType.ToString());

                            newValue = TypeCache.CreateArray(elementType.ToString(), array.Length / size);
                        }
                        else
                        {
                            newValue = TypeCache.CreateArray(elementType.ToString(), array.Length);
                        }

                        if (newValue != null)
                        {
                            if (elementType.GetInterface(typeof(IGuidAsset).FullName) != null ||
                                elementType == typeof(IGuidAsset))
                            {
                                for (var i = 0; i < array.Length; i++)
                                {
                                    if (array.GetValue(i) is string guid)
                                    {
                                        var asset = AssetSerialization.GetGuidAsset(elementType, guid);

                                        newValue.SetValue(asset, i);
                                    }
                                }
                            }
                            else if (elementType == typeof(string))
                            {
                                for (var i = 0; i < array.Length; i++)
                                {
                                    newValue.SetValue(array.GetValue(i), i);
                                }
                            }
                            else if (elementType.IsPrimitive)
                            {
                                if (array is byte[] buffer)
                                {
                                    DeserializePrimitiveArray(buffer, newValue);
                                }
                            }
                            else if (elementType.GetCustomAttribute<SerializableAttribute>() != null &&
                                array is object[] arrayData)
                            {
                                newValue = TypeCache.CreateArray(elementType.ToString(), arrayData.Length);

                                if (newValue != null)
                                {
                                    for (var i = 0; i < arrayData.Length; i++)
                                    {
                                        if (arrayData[i] is Dictionary<object, object> content)
                                        {
                                            if (content.TryGetValue(nameof(SerializableStapleAssetContainer.typeName), out var atn) &&
                                                atn is string arrayTypeName &&
                                                content.TryGetValue(nameof(SerializableStapleAssetContainer.fields), out var ap) &&
                                                ap is Dictionary<object, object> arrayParameters)
                                            {
                                                try
                                                {
                                                    var c = DecodeContainer(arrayTypeName, arrayParameters);

                                                    var itemValue = DeserializeContainer(c, mode);

                                                    if (itemValue != null)
                                                    {
                                                        newValue.SetValue(itemValue, i);
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    Log.Debug($"[{LogTag}] Failed to deserialize an item of type {arrayTypeName}: {e}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"[{LogTag}] Failed to deserialize {field.Name}: {e}");

                        return null;
                    }
                }
                else if (sourceValue is string hexString && field.GetCustomAttribute<SerializeAsHexAttribute>() != null)
                {
                    try
                    {
                        var bytes = Convert.FromHexString(hexString);

                        if (bytes != null)
                        {
                            if (fieldType.GetElementType().IsPrimitive &&
                                fieldType.GetElementType() != typeof(bool))
                            {
                                var size = TypeCache.SizeOf(elementType.ToString());

                                newValue = TypeCache.CreateArray(elementType.ToString(), bytes.Length / size);
                            }
                            else
                            {
                                newValue = TypeCache.CreateArray(elementType.ToString(), bytes.Length);
                            }

                            if (newValue != null)
                            {
                                DeserializePrimitiveArray(bytes, newValue);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"[{LogTag}] Failed to deserialize {field.Name}: {e}");

                        return null;
                    }
                }
                else if (fieldType.GetElementType().GetCustomAttribute<SerializableAttribute>() != null &&
                    fieldInfo.value is List<SerializableStapleAssetContainer> containers)
                {
                    try
                    {
                        newValue = TypeCache.CreateArray(fieldType.GetElementType().ToString(), containers.Count);

                        if (newValue != null)
                        {
                            for (var i = 0; i < containers.Count; i++)
                            {
                                try
                                {
                                    var itemValue = DeserializeContainer(containers[i].ToSerializerContainer(), mode);

                                    if (itemValue != null)
                                    {
                                        newValue.SetValue(itemValue, i);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.Debug($"[{LogTag}] Failed to decode an item for {fieldType.GetElementType().ToString()}: {e}");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"[{LogTag}] Failed to deserialize {field.Name}: {e}");

                        return null;
                    }
                }

                return newValue;
            }

            if (fieldType.IsGenericType)
            {
                if (fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    IList list = (IList)ObjectCreation.CreateObject(fieldType);

                    if (list == null)
                    {
                        return null;
                    }

                    var fail = false;
                    var elementType = fieldType.GenericTypeArguments[0];

                    if (IsValidType(elementType) == false)
                    {
                        return null;
                    }

                    var sourceValue = GetArrayValue(fieldInfo, type, field, mode);

                    if (sourceValue is object[] array)
                    {
                        foreach (var item in array)
                        {
                            if(item == null)
                            {
                                list.Add(null);
                            }
                            else if (item.GetType().IsAssignableTo(elementType) ||
                                elementType.IsPrimitive)
                            {
                                list.Add(item);
                            }
                            else if (elementType.GetInterface(typeof(IGuidAsset).FullName) != null)
                            {
                                if (item is string guid)
                                {
                                    var v = AssetSerialization.GetGuidAsset(elementType, guid);

                                    if (v != null)
                                    {
                                        list.Add(v);
                                    }
                                }
                            }
                            else if (elementType.GetCustomAttribute<SerializableAttribute>() != null)
                            {
                                try
                                {
                                    string innerTypeName = null;
                                    Dictionary<object, object> parameters = null;

                                    if (mode == StapleSerializationMode.Scene)
                                    {
                                        if(item is Dictionary<object, object> contents)
                                        {
                                            innerTypeName = elementType.ToString();

                                            parameters = [];

                                            foreach(var pair in contents)
                                            {
                                                if(pair.Key is string key)
                                                {
                                                    var elementField = elementType.GetField(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                                                    if(elementField == null)
                                                    {
                                                        continue;
                                                    }

                                                    parameters.Add(key, new Dictionary<object, object>()
                                                    {
                                                        { nameof(StapleSerializerContainer.typeName), elementField.FieldType.ToString() },
                                                        { nameof(StapleSerializerField.value), pair.Value },
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    else if (item is Dictionary<object, object> itemData &&
                                        itemData.Count == 2 &&
                                        itemData.ContainsKey(nameof(StapleSerializerContainer.typeName)) &&
                                        itemData.ContainsKey(nameof(StapleSerializerContainer.fields)) &&
                                        itemData[nameof(StapleSerializerContainer.typeName)] is string &&
                                        itemData[nameof(StapleSerializerContainer.fields)] is Dictionary<object, object>)
                                    {
                                        innerTypeName = (string)itemData[nameof(StapleSerializerContainer.typeName)];
                                        parameters = (Dictionary<object, object>)itemData[nameof(StapleSerializerContainer.fields)];
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
                                                containerData.ContainsKey(nameof(StapleSerializerContainer.typeName)) &&
                                                containerData.ContainsKey(nameof(StapleSerializerField.value)) &&
                                                containerData[nameof(StapleSerializerContainer.typeName)] is string)
                                            {
                                                containerKey = (string)containerPair.Key;
                                                parameterTypeName = (string)containerData[nameof(StapleSerializerContainer.typeName)];
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

                                        itemContainer.fields = containerParameters;

                                        var itemInstance = DeserializeContainer(itemContainer.ToSerializerContainer(), mode);

                                        if (itemInstance != null)
                                        {
                                            list.Add(itemInstance);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.Debug($"[{LogTag}] Failed to deserialize a {elementType.ToString()}: {e}");

                                    continue;
                                }
                            }
                            else
                            {
                                try
                                {
                                    var value = Convert.ChangeType(item, elementType.GenericTypeArguments[0]);

                                    list.Add(value);
                                }
                                catch (Exception e)
                                {
                                    fail = true;

                                    Log.Debug($"[{LogTag}] Failed to deserialize a {elementType.GenericTypeArguments[0].ToString()}: {e}");

                                    break;
                                }
                            }
                        }

                        if (fail)
                        {
                            return null;
                        }

                        return list;
                    }
                    else if (sourceValue is string hexString && field.GetCustomAttribute<SerializeAsHexAttribute>() != null)
                    {
                        Array newValue = null;

                        try
                        {
                            var bytes = Convert.FromHexString(hexString);

                            if (bytes != null)
                            {
                                if (elementType.IsPrimitive &&
                                    elementType != typeof(bool))
                                {
                                    var size = TypeCache.SizeOf(elementType.ToString());

                                    newValue = TypeCache.CreateArray(elementType.ToString(), bytes.Length / size);
                                }
                                else
                                {
                                    newValue = TypeCache.CreateArray(elementType.ToString(), bytes.Length);
                                }

                                if (newValue != null)
                                {
                                    DeserializePrimitiveArray(bytes, newValue);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Debug($"[{LogTag}] Failed to deserialize base64 array of {elementType.ToString()}: {e}");

                            return null;
                        }

                        if (newValue != null)
                        {
                            foreach (var element in newValue)
                            {
                                list.Add(element);
                            }

                            return list;
                        }
                    }

                    return null;
                }
            }

            if (fieldType == typeof(SerializableStapleAssetContainer) &&
                field.GetValue(fieldInfo.value) is SerializableStapleAssetContainer innerContainer)
            {
                var value = DeserializeContainer(innerContainer.ToSerializerContainer(), mode);

                return value;
            }

            {
                var v = fieldInfo.value is JsonElement element ? GetJsonValue(type, field, fieldType, element, mode) : fieldInfo.value;

                if (fieldType.IsEnum && v is string str)
                {
                    if (Enum.TryParse(fieldType, str, true, out var enumValue))
                    {
                        return enumValue;
                    }

                    return null;
                }
            }

            {
                var v = fieldInfo.value is JsonElement element ? GetJsonValue(type, field, fieldType, element, mode) : fieldInfo.value;

                return v;
            }
        }
        catch (Exception e)
        {
            Log.Debug($"[{LogTag}] Failed to deserialize field {field.Name} for {type.ToString()}: {e}");

            return null;
        }
    }

    /// <summary>
    /// Deserializes a container into an object instance
    /// </summary>
    /// <param name="container">The container to deserialize</param>
    /// <param name="mode">The serialization mode we want to use</param>
    /// <param name="instance">The existing object instance, if any</param>
    /// <returns>the object instance, or null</returns>
    public static object DeserializeContainer(StapleSerializerContainer container, StapleSerializationMode mode, object instance = null)
    {
        var type = TypeCache.GetType(container.typeName);

        if (type == null ||
            (type.GetCustomAttribute<SerializeInEditorAttribute>() != null && Platform.IsEditor == false))
        {
            return null;
        }

        try
        {
            instance ??= ObjectCreation.CreateObject(type);

            if (instance is null)
            {
                return null;
            }

            foreach (var pair in container.fields)
            {
                try
                {
                    var field = type.GetField(pair.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if(field is null)
                    {
                        continue;
                    }

                    var value = DeserializeField(type, field, field.FieldType, pair.Value, mode);

                    if (field.FieldType == typeof(Entity) && value is int i)
                    {
                        var localEntity = Scene.FindEntity(i);

                        if(localEntity.IsValid)
                        {
                            field.SetValue(instance, localEntity);
                        }

                        continue;
                    }

                    if (value is null || value.GetType().IsAssignableTo(field.FieldType) == false)
                    {
                        continue;
                    }

                    field.SetValue(instance, value);
                }
                catch (Exception e)
                {
                    Log.Debug($"[{LogTag}] Failed to deserialize field {pair.Key} for {container.typeName}: {e}");
                }
            }
        }
        catch (Exception e)
        {
            Log.Debug($"[{LogTag}] Failed to deserialize instance for {type.ToString()}: {e}");

            return null;
        }

        return instance;
    }

    public static StapleSerializerContainer DecodeContainer(string typeName, Dictionary<object, object> parameters)
    {
        var decodedContainer = new StapleSerializerContainer()
        {
            typeName = typeName,
        };

        foreach (var paramPair in parameters)
        {
            if (paramPair.Key is string parameterName && paramPair.Value is Dictionary<object, object> v &&
                v.TryGetValue(nameof(SerializableStapleAssetContainer.typeName), out var t) &&
                t is string tName)
            {
                var localType = TypeCache.GetType(tName);

                if (localType == null ||
                    (localType.GetCustomAttribute<SerializeInEditorAttribute>() != null && Platform.IsEditor == false))
                {
                    continue;
                }

                if (v.TryGetValue(nameof(SerializableStapleAssetContainer.fields), out var p) &&
                    p is Dictionary<object, object> pDictionary)
                {
                    var container = DecodeContainer(tName, pDictionary);

                    if (container != null)
                    {
                        decodedContainer.fields.Add(parameterName, new()
                        {
                            typeName = tName,
                            value = container,
                        });
                    }
                }
                else if(v.TryGetValue(nameof(SerializableStapleAssetParameter.value), out var value))
                {
                    decodedContainer.fields.Add(parameterName, new()
                    {
                        typeName = tName,
                        value = value,
                    });
                }
            }
        }

        return decodedContainer;
    }

    /// <summary>
    /// Attempts to serialize an object into a SerializableStapleAsset
    /// </summary>
    /// <param name="instance">The object's instance</param>
    /// <param name="mode">The serialization mode we want to use</param>
    /// <returns>The SerializableStapleAsset, or null</returns>
    public static SerializableStapleAsset SerializeAssetObject(object instance, StapleSerializationMode mode)
    {
        if (instance == null)
        {
            return default;
        }

        try
        {
            var container = SerializeContainer(instance, mode)?.ToSerializableContainer();

            if (container == null)
            {
                return default;
            }

            var outValue = new SerializableStapleAsset()
            {
                typeName = instance.GetType().ToString(),
                parameters = container.fields,
            };

            return outValue;
        }
        catch (Exception e)
        {
            Log.Debug($"[{LogTag}] Failed to serialize {instance.GetType().ToString()}: {e}");

            return default;
        }
    }

    /// <summary>
    /// Deserializes an asset into an object instance
    /// </summary>
    /// <param name="asset">The asset data</param>
    /// <param name="mode">The serialization mode we want to use</param>
    /// <returns>The instance, or null</returns>
    public static object DeserializeAssetObject(SerializableStapleAsset asset, StapleSerializationMode mode)
    {
        if (asset == null)
        {
            return null;
        }

        var innerContainer = asset.ToSerializerContainer(out var guid);

        var instance = DeserializeContainer(innerContainer, mode);

        if (instance is IGuidAsset guidAsset)
        {
            guidAsset.Guid.Guid = guid;
        }

        return instance;
    }
}
