using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Staple.Tooling;

/// <summary>
/// Special JsonConvert resolver that allows you to ignore properties. See https://stackoverflow.com/a/13588192/1037948
/// </summary>
/// <remarks>Ignores all properties (only fields), to conform to how System.Text.Json works</remarks>
public class IgnorableSerializerContractResolver : DefaultContractResolver
{
    protected readonly Dictionary<Type, HashSet<string>> Ignores = [];

    public void Ignore(Type type, params string[] propertyName)
    {
        if (!Ignores.TryGetValue(type, out var value))
        {
            value = [];

            Ignores[type] = value;
        }

        foreach (var prop in propertyName)
        {
            value.Add(prop);
        }
    }

    /// <summary>
    /// Is the given property for the given type ignored?
    /// </summary>
    /// <param name="type"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public bool IsIgnored(Type type, string propertyName)
    {
        if (!Ignores.TryGetValue(type, out var value))
        {
            return false;
        }

        // if no properties provided, ignore the type entirely
        if (value.Count == 0)
        {
            return true;
        }

        return value.Contains(propertyName);
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        if (member.MemberType == MemberTypes.Property)
        {
            property.ShouldSerialize = instance => false;

            return property;
        }

        // need to check basetype as well for EF -- @per comment by user576838
        if (IsIgnored(property.DeclaringType, property.PropertyName) || IsIgnored(property.DeclaringType.BaseType, property.PropertyName))
        {
            property.ShouldSerialize = instance => false;
        }

        return property;
    }
}
