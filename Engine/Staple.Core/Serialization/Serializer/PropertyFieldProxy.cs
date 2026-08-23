using System;
using System.Reflection;

namespace Staple.Internal;

/// <summary>
/// Proxy for managing a field or proxy, used mainly for simplifying serialization
/// </summary>
public class PropertyFieldProxy
{
    private readonly FieldInfo field;

    private readonly PropertyInfo property;

    public readonly Type FieldType;

    public readonly bool IsValid;

    public PropertyFieldProxy(Type type, string name, BindingFlags flags)
    {
        try
        {
            field = type.GetField(name, flags);

            if(field == null)
            {
                property = type.GetProperty(name, flags);

                if(property != null &&
                    (!property.CanRead ||
                    !property.CanWrite))
                {
                    property = null;
                }
            }
        }
        catch(Exception)
        {
            return;
        }

        if(field == null && property == null)
        {
            return;
        }

        if (field != null)
        {
            FieldType = field.FieldType;
        }
        else if(property != null)
        {
            FieldType = property.PropertyType;
        }

        IsValid = true;
    }

    public bool HasAttribute(Type t)
    {
        if(field != null)
        {
            return field.GetCustomAttribute(t) != null;
        }

        if(property != null)
        {
            return property.GetCustomAttribute(t) != null;
        }

        return false;
    }

    public bool HasAttribute<T>()
    {
        return HasAttribute(typeof(T));
    }

    public object GetValue(object instance)
    {
        try
        {
            if (field != null)
            {
                return field.GetValue(instance);
            }
            else if(property != null)
            {
                return property.GetValue(instance);
            }
        }
        catch (Exception)
        {
        }

        return null;
    }

    public void SetValue(object instance, object value)
    {
        try
        {
            if (field != null)
            {
                field.SetValue(instance, value);
            }
            else if (property != null)
            {
                property.SetValue(instance, value);
            }
        }
        catch (Exception)
        {
        }
    }
}
