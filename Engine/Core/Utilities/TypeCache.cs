using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Staple.Internal;

/// <summary>
/// Container for all relevant types used by Staple in one way or another
/// </summary>
public static class TypeCache
{
    internal static Dictionary<string, Type> types = new();

    /// <summary>
    /// Clears the type cache
    /// </summary>
    public static void Clear()
    {
        types.Clear();
    }

    /// <summary>
    /// Finds all types that are in one way or another derivative of a type
    /// </summary>
    /// <typeparam name="T">The type to check</typeparam>
    /// <returns>A list of types</returns>
    public static Type[] AllTypesSubclassingOrImplementing<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        T>()
    {
        return AllTypesSubclassingOrImplementing(typeof(T));
    }

    /// <summary>
    /// Finds all types that are in one way or another derivative of a type
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>A list of types</returns>
    public static Type[] AllTypesSubclassingOrImplementing(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type)
    {
        var outValue = new List<Type>();

        foreach(var pair in types)
        {
            if(pair.Value.IsInterface == false &&
                type.IsAssignableFrom(pair.Value))
            {
                outValue.Add(pair.Value);
            }
        }

        return outValue.ToArray();
    }

    /// <summary>
    /// Gets a type from a type FullName
    /// </summary>
    /// <param name="name">The name of the type</param>
    /// <returns>The type, or null</returns>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public static Type GetType(string name)
    {
        if(types.TryGetValue(name, out var type))
        {
            return type;
        }

        type = Type.GetType(name);

        if(type != null)
        {
            types.Add(name, type);
        }

        return type;
    }

    /// <summary>
    /// Gets all registered types
    /// </summary>
    /// <returns>All registered types</returns>
    public static Type[] AllTypes()
    {
        return types.Values.ToArray();
    }

    /// <summary>
    /// Registers a type in the type cache
    /// </summary>
    /// <param name="type">The type to register</param>
    public static void RegisterType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type)
    {
        if(types.ContainsKey(type.FullName))
        {
            return;
        }

        types.Add(type.FullName, type);
    }
}
