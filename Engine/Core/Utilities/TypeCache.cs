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
    public class ComponentCallbacks
    {
        public Func<Entity, IComponent> add;
        public Action<Entity> remove;
        public Func<Entity, IComponent> get;
    }

    internal static readonly Dictionary<string, Type> types = [];

    internal static readonly Dictionary<string, Func<int, Array>> arrayConstructors = [];

    internal static readonly Dictionary<string, Func<int>> sizeOfs = [];

    private static readonly Dictionary<string, ComponentCallbacks> componentCallbacks = [];

    private static readonly Dictionary<string, Type[]> subclassCaches = [];

    internal static readonly Dictionary<string, HashSet<string>> inheritance = [];

    /// <summary>
    /// Clears the type cache
    /// </summary>
    public static void Clear()
    {
        types.Clear();
        arrayConstructors.Clear();
        sizeOfs.Clear();
        componentCallbacks.Clear();
        subclassCaches.Clear();
        inheritance.Clear();
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
        if(subclassCaches.TryGetValue(type.FullName, out var cache))
        {
            return cache;
        }

        var outValue = new List<Type>();

        foreach(var pair in types)
        {
            if(pair.Value.IsInterface == false &&
                type.IsAssignableFrom(pair.Value))
            {
                outValue.Add(pair.Value);
            }
        }

        var v = outValue.ToArray();

        subclassCaches.Add(type.FullName, v);

        return v;
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
    /// Attempts to create a component for an entity
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="typeName">The component type name</param>
    /// <returns>The component or default</returns>
    public static IComponent AddComponent(Entity entity, string typeName)
    {
        if(componentCallbacks.TryGetValue(typeName, out var callback) == false)
        {
            return default;
        }

        try
        {
            return callback.add?.Invoke(entity);
        }
        catch(Exception e)
        {
            Log.Debug($"[TypeCache] Failed to add a component '{typeName}: {e}");
        }

        return default;
    }

    /// <summary>
    /// Attempts to remove a component for an entity
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="typeName">The component type name</param>
    public static void RemoveComponent(Entity entity, string typeName)
    {
        if (componentCallbacks.TryGetValue(typeName, out var callback) == false)
        {
            return;
        }

        try
        {
            callback.remove?.Invoke(entity);
        }
        catch (Exception e)
        {
            Log.Debug($"[TypeCache] Failed to add a component '{typeName}: {e}");
        }
    }

    /// <summary>
    /// Attempts to get a component in an entity
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="typeName">The component type name</param>
    /// <returns>The component or default</returns>
    public static IComponent GetComponent(Entity entity, string typeName)
    {
        if (componentCallbacks.TryGetValue(typeName, out var callback) == false)
        {
            return default;
        }

        try
        {
            return callback.get?.Invoke(entity);
        }
        catch (Exception e)
        {
            Log.Debug($"[TypeCache] Failed to add a component '{typeName}: {e}");
        }

        return default;
    }

    /// <summary>
    /// Attempts to create an array of a specific type
    /// </summary>
    /// <param name="typeName">The type name</param>
    /// <param name="length">The size of the array</param>
    /// <returns>An array instance, or default</returns>
    public static Array CreateArray(string typeName, int length)
    {
        if(arrayConstructors.TryGetValue(typeName, out var fn) == false)
        {
            return default;
        }

        try
        {
            return fn(length);
        }
        catch(Exception e)
        {
            Log.Debug($"[TypeCache] Failed to create array of {typeName}: {e}");

            return default;
        }
    }

    /// <summary>
    /// Attempts to get the size of a type
    /// </summary>
    /// <param name="typeName">The type name</param>
    /// <returns>The size, or 0</returns>
    public static int SizeOf(string typeName)
    {
        if(sizeOfs.TryGetValue(typeName, out var fn) == false)
        {
            return 0;
        }

        try
        {
            return fn();
        }
        catch (Exception e)
        {
            Log.Debug($"[TypeCache] Failed to get size of {typeName}: {e}");

            return default;
        }
    }

    /// <summary>
    /// Registers a type in the type cache
    /// </summary>
    /// <param name="type">The type to register</param>
    public static void RegisterType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type type,
        Func<int> sizeOf,
        Func<int, Array> createArray,
        ComponentCallbacks callbacks)
    {
        if(types.ContainsKey(type.FullName))
        {
            return;
        }

        types.Add(type.FullName, type);
        sizeOfs.Add(type.FullName, sizeOf);

        HashSet<string> inheritanceInfo = [];

        void GatherInheritance(Type t)
        {
            inheritanceInfo.Add(t.FullName);

            foreach(var i in t.GetInterfaces())
            {
                inheritanceInfo.Add(i.FullName);
            }

            if(t.BaseType != null)
            {
                GatherInheritance(t.BaseType);
            }
        }

        GatherInheritance(type);

        inheritance.Add(type.FullName, inheritanceInfo);

        if(createArray != null)
        {
            arrayConstructors.Add(type.FullName, createArray);
        }

        if (callbacks != null && type.IsAssignableTo(typeof(IComponent)))
        {
            componentCallbacks.Add(type.FullName, callbacks);
        }
    }
}
