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

    public object SerializeField(object instance, Type type, FieldInfo field, Type fieldType)
    {
        switch (type)
        {
            case Type t when t == typeof(Color):

                {
                    if (instance is Color c)
                    {
                        return c.UIntValue;
                    }
                }

                break;

            case Type t when t == typeof(Color32):

                {
                    if (instance is Color32 c)
                    {
                        return c.UIntValue;
                    }
                }

                break;

            case Type t when t == typeof(LayerMask):

                {
                    if (instance is LayerMask mask)
                    {
                        return mask.value;
                    }
                }

                break;

            case Type t when t == typeof(Rect):

                {
                    if (instance is Rect rect)
                    {
                        return new Vector4Holder(new Vector4(rect.left, rect.top, rect.right, rect.bottom));
                    }
                }

                break;

            case Type t when t == typeof(RectFloat):

                {
                    if (instance is RectFloat rect)
                    {
                        return new Vector4Holder(new Vector4(rect.left, rect.top, rect.right, rect.bottom));
                    }
                }

                break;

            case Type t when t == typeof(Vector2Int):

                {
                    if (instance is Vector2Int v)
                    {
                        return new Vector2Holder(v);
                    }
                }

                break;

            case Type t when t == typeof(Vector3Int):

                {
                    if (instance is Vector3Int v)
                    {
                        return new Vector3Holder(v);
                    }
                }

                break;

            case Type t when t == typeof(Vector4Int):

                {
                    if (instance is Vector4Int v)
                    {
                        return new Vector4Holder(v);
                    }
                }

                break;
        }

        return null;
    }

    public object DeserializeField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo)
    {
        switch (type)
        {
            case Type t when t == typeof(Color):

                {
                    if (fieldInfo.value is uint u)
                    {
                        return (Color)new Color32(u);
                    }
                }

                break;

            case Type t when t == typeof(Color32):

                {
                    if (fieldInfo.value is uint u)
                    {
                        return new Color32(u);
                    }
                }

                break;

            case Type t when t == typeof(LayerMask):

                {
                    if (fieldInfo.value is uint u)
                    {
                        return new LayerMask(u);
                    }
                }

                break;

            case Type t when t == typeof(Rect):

                {
                    if (fieldInfo.value is Vector4Holder h)
                    {
                        return new Rect((int)h.x, (int)h.z, (int)h.y, (int)h.w);
                    }
                }

                break;

            case Type t when t == typeof(RectFloat):

                {
                    if (fieldInfo.value is Vector4Holder h)
                    {
                        return new RectFloat(h.x, h.z, h.y, h.w);
                    }
                }

                break;

            case Type t when t == typeof(Vector2Int):

                {
                    if (fieldInfo.value is Vector2Holder h)
                    {
                        return new Vector2Int((int)h.x, (int)h.y);
                    }
                }

                break;

            case Type t when t == typeof(Vector3Int):

                {
                    if (fieldInfo.value is Vector3Holder h)
                    {
                        return new Vector3Int((int)h.x, (int)h.y, (int)h.z);
                    }
                }

                break;

            case Type t when t == typeof(Vector4Int):

                {
                    if (fieldInfo.value is Vector4Holder h)
                    {
                        return new Vector4Int((int)h.x, (int)h.y, (int)h.z, (int)h.w);
                    }
                }

                break;
        }

        return null;
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
                    if (instance is Rect rect)
                    {
                        return new Vector4Holder(new Vector4(rect.left, rect.top, rect.right, rect.bottom));
                    }
                }

                break;

            case Type t when t == typeof(RectFloat):

                {
                    if (instance is RectFloat rect)
                    {
                        return new Vector4Holder(new Vector4(rect.left, rect.top, rect.right, rect.bottom));
                    }
                }

                break;

            case Type t when t == typeof(Vector2Int):

                {
                    if (instance is Vector2Int v)
                    {
                        return new Vector2Holder(v);
                    }
                }

                break;

            case Type t when t == typeof(Vector3Int):

                {
                    if (instance is Vector3Int v)
                    {
                        return new Vector3Holder(v);
                    }
                }

                break;

            case Type t when t == typeof(Vector4Int):

                {
                    if (instance is Vector4Int v)
                    {
                        return new Vector4Holder(v);
                    }
                }

                break;
        }

        return null;
    }

    public object DeserializeJsonField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo, JsonElement element)
    {
        switch (type)
        {
            case Type t when t == typeof(Color) && element.ValueKind == JsonValueKind.String:

                return new Color(element.GetString());

            case Type t when t == typeof(Color32) && element.ValueKind == JsonValueKind.String:

                return new Color32(element.GetString());

            case Type t when t == typeof(LayerMask) && element.ValueKind == JsonValueKind.Number:

                return new LayerMask(element.GetUInt32());

            case Type t when t == typeof(Rect) && element.ValueKind == JsonValueKind.Object:

                {
                    if (element.TryGetProperty(nameof(Vector4Holder.x), out var xProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.y), out var yProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.z), out var zProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.w), out var wProp) &&
                        xProp.ValueKind == JsonValueKind.Number &&
                        yProp.ValueKind == JsonValueKind.Number &&
                        zProp.ValueKind == JsonValueKind.Number &&
                        wProp.ValueKind == JsonValueKind.Number)
                    {
                        return new Rect(xProp.GetInt32(), zProp.GetInt32(), yProp.GetInt32(), wProp.GetInt32());
                    }
                }

                break;

            case Type t when t == typeof(RectFloat) && element.ValueKind == JsonValueKind.Object:

                {
                    if (element.TryGetProperty(nameof(Vector4Holder.x), out var xProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.y), out var yProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.z), out var zProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.w), out var wProp) &&
                        xProp.ValueKind == JsonValueKind.Number &&
                        yProp.ValueKind == JsonValueKind.Number &&
                        zProp.ValueKind == JsonValueKind.Number &&
                        wProp.ValueKind == JsonValueKind.Number)
                    {
                        return new RectFloat(xProp.GetSingle(), zProp.GetSingle(), yProp.GetSingle(), wProp.GetSingle());
                    }
                }

                break;

            case Type t when t == typeof(Vector2Int) && element.ValueKind == JsonValueKind.Object:

                {
                    if (element.TryGetProperty(nameof(Vector2Holder.x), out var xProp) &&
                        element.TryGetProperty(nameof(Vector2Holder.y), out var yProp) &&
                        xProp.ValueKind == JsonValueKind.Number &&
                        yProp.ValueKind == JsonValueKind.Number)
                    {
                        return new Vector2Int(xProp.GetInt32(), yProp.GetInt32());
                    }
                }

                break;

            case Type t when t == typeof(Vector3Int) && element.ValueKind == JsonValueKind.Object:

                {
                    if (element.TryGetProperty(nameof(Vector3Holder.x), out var xProp) &&
                        element.TryGetProperty(nameof(Vector3Holder.y), out var yProp) &&
                        element.TryGetProperty(nameof(Vector3Holder.z), out var zProp) &&
                        xProp.ValueKind == JsonValueKind.Number &&
                        yProp.ValueKind == JsonValueKind.Number &&
                        zProp.ValueKind == JsonValueKind.Number)
                    {
                        return new Vector3Int(xProp.GetInt32(), yProp.GetInt32(), zProp.GetInt32());
                    }
                }

                break;

            case Type t when t == typeof(Vector4Int) && element.ValueKind == JsonValueKind.Object:

                {
                    if (element.TryGetProperty(nameof(Vector4Holder.x), out var xProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.y), out var yProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.z), out var zProp) &&
                        element.TryGetProperty(nameof(Vector4Holder.w), out var wProp) &&
                        xProp.ValueKind == JsonValueKind.Number &&
                        yProp.ValueKind == JsonValueKind.Number &&
                        zProp.ValueKind == JsonValueKind.Number &&
                        wProp.ValueKind == JsonValueKind.Number)
                    {
                        return new Vector4Int(xProp.GetInt32(), yProp.GetInt32(), zProp.GetInt32(), wProp.GetInt32());
                    }
                }

                break;
        }

        return null;
    }
}
