using System;
using System.Collections.Frozen;
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

    internal static bool useFrozenCollections = false;

    internal static FrozenDictionary<string, Type> frozenTypes;

    internal static FrozenDictionary<string, Func<int, Array>> frozenArrayConstructors;

    internal static FrozenDictionary<string, Func<int>> frozenSizeOfs;

    internal static FrozenDictionary<string, ComponentCallbacks> frozenComponentCallbacks;

    internal static FrozenDictionary<string, HashSet<string>> frozenInheritance;

    internal static Type[] frozenTypesArray;

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

        frozenArrayConstructors = null;
        frozenComponentCallbacks = null;
        frozenInheritance = null;
        frozenSizeOfs = null;
        frozenTypes = null;
        frozenTypesArray = null;

        useFrozenCollections = false;
    }

    /// <summary>
    /// Freezes the type cache
    /// </summary>
    public static void Freeze()
    {
        useFrozenCollections = true;

        frozenArrayConstructors = arrayConstructors.ToFrozenDictionary();
        frozenComponentCallbacks = componentCallbacks.ToFrozenDictionary();
        frozenInheritance = inheritance.ToFrozenDictionary();
        frozenSizeOfs = sizeOfs.ToFrozenDictionary();
        frozenTypes = types.ToFrozenDictionary();

        frozenTypesArray = types.Values.ToArray();
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
        if(subclassCaches.TryGetValue(type.ToString(), out var cache))
        {
            return cache;
        }

        var outValue = new List<Type>();

        if(useFrozenCollections)
        {
            foreach (var pair in frozenTypes)
            {
                if (!pair.Value.IsInterface &&
                    type.IsAssignableFrom(pair.Value))
                {
                    outValue.Add(pair.Value);
                }
            }
        }
        else
        {
            foreach (var pair in types)
            {
                if (!pair.Value.IsInterface &&
                    type.IsAssignableFrom(pair.Value))
                {
                    outValue.Add(pair.Value);
                }
            }
        }

        var v = outValue.ToArray();

        subclassCaches.Add(type.ToString(), v);

        return v;
    }

    /// <summary>
    /// Gets a type from a type ToString()
    /// </summary>
    /// <param name="name">The name of the type</param>
    /// <returns>The type, or null</returns>
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public static Type GetType(string name)
    {
        Type type;

        if (useFrozenCollections)
        {
            return frozenTypes.TryGetValue(name, out type) ? type : null;
        }

        if (types.TryGetValue(name, out type))
        {
            return type;
        }

        type = Type.GetType(name);

        if(type != null && !types.ContainsKey(type.ToString()))
        {
            types.Add(type.ToString(), type);
        }

        return type;
    }

    /// <summary>
    /// Gets all registered types
    /// </summary>
    /// <returns>All registered types</returns>
    public static Type[] AllTypes()
    {
        return useFrozenCollections ? frozenTypesArray : types.Values.ToArray();
    }

    /// <summary>
    /// Attempts to create a component for an entity
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="typeName">The component type name</param>
    /// <returns>The component or default</returns>
    public static IComponent AddComponent(Entity entity, string typeName)
    {
        if(useFrozenCollections ? !frozenComponentCallbacks.TryGetValue(typeName, out var callback) :
            !componentCallbacks.TryGetValue(typeName, out callback))
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
        if (useFrozenCollections ? !frozenComponentCallbacks.TryGetValue(typeName, out var callback) :
            !componentCallbacks.TryGetValue(typeName, out callback))
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
        if (useFrozenCollections ? !frozenComponentCallbacks.TryGetValue(typeName, out var callback) :
            !componentCallbacks.TryGetValue(typeName, out callback))
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
        if(useFrozenCollections ? !frozenArrayConstructors.TryGetValue(typeName, out var fn) :
            !arrayConstructors.TryGetValue(typeName, out fn))
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
        if (useFrozenCollections ? !frozenSizeOfs.TryGetValue(typeName, out var fn) :
            !sizeOfs.TryGetValue(typeName, out fn))
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
        if(useFrozenCollections || types.ContainsKey(type.ToString()))
        {
            return;
        }

        types.Add(type.ToString(), type);
        sizeOfs.Add(type.ToString(), sizeOf);

        HashSet<string> inheritanceInfo = [];

        void GatherInheritance(Type t)
        {
            inheritanceInfo.Add(t.ToString());

            foreach(var i in t.GetInterfaces())
            {
                inheritanceInfo.Add(i.ToString());
            }

            if(t.BaseType != null)
            {
                GatherInheritance(t.BaseType);
            }
        }

        GatherInheritance(type);

        inheritance.Add(type.ToString(), inheritanceInfo);

        if(createArray != null)
        {
            arrayConstructors.Add(type.ToString(), createArray);
        }

        if (callbacks != null && type.IsAssignableTo(typeof(IComponent)))
        {
            componentCallbacks.Add(type.ToString(), callbacks);
        }
    }
}
