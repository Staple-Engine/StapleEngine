using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Staple.Tooling;

/// <summary>
/// Special JsonConvert resolver that allows you to ignore properties.  See https://stackoverflow.com/a/13588192/1037948
/// </summary>
public class IgnorableSerializerContractResolver : DefaultContractResolver
{
    protected readonly Dictionary<Type, HashSet<string>> Ignores = [];

    /// <summary>
    /// Explicitly ignore the given property(s) for the given type
    /// </summary>
    /// <param name="type"></param>
    /// <param name="propertyName">one or more properties to ignore.  Leave empty to ignore the type entirely.</param>
    public void Ignore(Type type, params string[] propertyName)
    {
        // start bucket if DNE
        if (Ignores.TryGetValue(type, out var value) == false)
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

    /// <summary>
    /// The decision logic goes here
    /// </summary>
    /// <param name="member"></param>
    /// <param name="memberSerialization"></param>
    /// <returns></returns>
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        if (IsIgnored(property.DeclaringType, property.PropertyName)
        // need to check basetype as well for EF -- @per comment by user576838
        || IsIgnored(property.DeclaringType.BaseType, property.PropertyName))
        {
            property.ShouldSerialize = instance => { return false; };
        }

        return property;
    }
}
