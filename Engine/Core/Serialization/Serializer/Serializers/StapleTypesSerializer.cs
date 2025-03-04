using System;
using System.Numerics;
using System.Reflection;
using System.Text.Json;

namespace Staple.Internal;

internal class StapleTypesSerializer : IStapleTypeSerializer
{
    public bool HandlesType(Type type)
    {
        return type switch
        {
            Type t when t == typeof(Color) || t == typeof(Color32) => true,
            Type t when t == typeof(LayerMask) => true,
            Type t when t == typeof(Rect) || t == typeof(RectFloat) => true,
            Type t when t == typeof(Vector2Int) || t == typeof(Vector3Int) || t == typeof(Vector4Int) => true,
            _ => false,
        };
    }

    public object DeserializeField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo)
    {
    }

    public object DeserializeJsonField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo, JsonElement element)
    {
    }

    public object SerializeField(object instance, Type type, FieldInfo field, Type fieldType)
    {
    }

    public object SerializeJsonField(object instance, Type type, FieldInfo field, Type fieldType)
    {
        switch(type)
        {
            case Type t when t == typeof(Color):

                {
                    if (instance is Color c)
                    {
                        return $"#{c.HexValue}";
                    }
                }

                break;

            case Type t when t == typeof(Color32):

                {
                    if (instance is Color32 c)
                    {
                        return $"#{c.HexValue}";
                    }
                }

                break;

            case Type t when t == typeof(LayerMask):

                {
                    if(instance is LayerMask mask)
                    {
                        return mask.value;
                    }
                }

                break;

            case Type t when t == typeof(Rect):

                {
                    if(instance is Rect rect)
                    {
                        return new Vector4(rect.left, rect.top, rect.right, rect.bottom);
                    }
                }

                break;

            case Type t when t == typeof(RectFloat):

                {
                    if (instance is RectFloat rect)
                    {
                        return new Vector4(rect.left, rect.top, rect.right, rect.bottom);
                    }
                }

                break;
        }

        return null;
    }
}
