using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace Staple.Internal;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
internal interface IStapleTypeSerializer
{
    bool HandlesType(Type type);

    object DeserializeField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo, StapleSerializationMode mode);

    object DeserializeJsonField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo, JsonElement element, StapleSerializationMode mode);

    object SerializeField(object instance, Type type, FieldInfo field, Type fieldType, StapleSerializationMode mode);

    object SerializeJsonField(object instance, Type type, FieldInfo field, Type fieldType, StapleSerializationMode mode);
}
