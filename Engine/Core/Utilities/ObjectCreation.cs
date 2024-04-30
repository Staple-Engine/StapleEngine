using System;

namespace Staple.Internal;

public static class ObjectCreation
{
    public static T CreateObject<T>(Type type)
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
}
