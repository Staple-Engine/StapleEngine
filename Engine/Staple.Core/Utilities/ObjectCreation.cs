using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple.Internal;

/// <summary>
/// Safely creates an object without needing to deal with exceptions
/// </summary>
public static class ObjectCreation
{
    /// <summary>
    /// Safely creates an object for an expected object type
    /// </summary>
    /// <typeparam name="T">The expected object type</typeparam>
    /// <param name="type">The type to create</param>
    /// <returns>The object, or null</returns>
    public static T CreateObject<T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        if(type.IsAssignableTo(typeof(T)) == false)
        {
            return default;
        }

        try
        {
            return (T)Activator.CreateInstance(type);
        }
        catch(Exception e)
        {
            Log.Error(e.ToString());
        }

        return default;
    }

    /// <summary>
    /// Safely creates an object for an expected object type
    /// </summary>
    /// <param name="type">The type to create</param>
    /// <returns>The object, or null</returns>
    public static object CreateObject([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }

        return default;
    }
}
