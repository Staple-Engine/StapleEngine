using System;
using System.Collections.Generic;
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
            Type t when t == typeof(IGuidAsset) || t.GetInterface(typeof(IGuidAsset).FullName) != null => true,
            Type t when t == typeof(Entity) => true,
            Type t when t == typeof(IComponent) || t.GetInterface(typeof(IComponent).FullName) != null => true,
            Type t when t == typeof(Sprite) => true,
            _ => false,
        };
    }

    public object SerializeField(object instance, Type type, FieldInfo field, Type fieldType, StapleSerializationMode mode)
    {
        switch (fieldType)
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

            case Type t when t == typeof(IGuidAsset) ||
                t.GetInterface(typeof(IGuidAsset).FullName) != null:

                {
                    if(instance is IGuidAsset a)
                    {
                        //Normalize the asset path in case the guid is actually a path
                        return AssetSerialization.GetAssetPathFromCache(a.Guid.Guid);
                    }
                }

                break;

            case Type t when t == typeof(Entity):

                {
                    if(instance is Entity e)
                    {
                        return e.Identifier.ID;
                    }
                }

                break;

            case Type t when t == typeof(IComponent) ||
                t.GetInterface(typeof(IComponent).FullName) != null:

                {
                    if(instance is IComponent component &&
                        (World.Current?.TryGetComponentEntity(component, out var e) ?? false))
                    {
                        return $"{e.Identifier.ID}:{component.GetType().ToString()}";
                    }
                }

                break;

            case Type t when t == typeof(Sprite):

                {
                    if(instance is Sprite sprite)
                    {
                        return $"{sprite.texture?.Guid.Guid ?? ""}:{sprite.spriteIndex}";
                    }
                }

                break;
        }

        return null;
    }

    public object DeserializeField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo, StapleSerializationMode mode)
    {
        switch (fieldType)
        {
            case Type t when t == typeof(Color):

                {
                    if (fieldInfo.value is uint u)
                    {
                        return (Color)new Color32(u);
                    }
                    else if (fieldInfo.value is string s)
                    {
                        return new Color(s);
                    }
                }

                break;

            case Type t when t == typeof(Color32):

                {
                    if (fieldInfo.value is uint u)
                    {
                        return new Color32(u);
                    }
                    else if(fieldInfo.value is string s)
                    {
                        return new Color32(s);
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
                    if (fieldInfo.value is Dictionary<object, object> d &&
                        d.TryGetValue(nameof(Vector4Holder.x), out var xObject) &&
                        d.TryGetValue(nameof(Vector4Holder.y), out var yObject) &&
                        d.TryGetValue(nameof(Vector4Holder.z), out var zObject) &&
                        d.TryGetValue(nameof(Vector4Holder.w), out var wObject) &&
                        xObject is float x &&
                        yObject is float y &&
                        zObject is float z &&
                        wObject is float w)
                    {
                        return new Rect((int)x, (int)z, (int)y, (int)w);
                    }
                    else if (fieldInfo.value is Vector4Holder h)
                    {
                        return new Rect((int)h.x, (int)h.z, (int)h.y, (int)h.w);
                    }
                }

                break;

            case Type t when t == typeof(RectFloat):

                {
                    if (fieldInfo.value is Dictionary<object, object> d &&
                        d.TryGetValue(nameof(Vector4Holder.x), out var xObject) &&
                        d.TryGetValue(nameof(Vector4Holder.y), out var yObject) &&
                        d.TryGetValue(nameof(Vector4Holder.z), out var zObject) &&
                        d.TryGetValue(nameof(Vector4Holder.w), out var wObject) &&
                        xObject is float x &&
                        yObject is float y &&
                        zObject is float z &&
                        wObject is float w)
                    {
                        return new RectFloat(x, z, y, w);
                    }
                    else if (fieldInfo.value is Vector4Holder h)
                    {
                        return new RectFloat(h.x, h.z, h.y, h.w);
                    }
                }

                break;

            case Type t when t == typeof(Vector2Int):

                {
                    if (fieldInfo.value is Dictionary<object, object> d &&
                        d.TryGetValue(nameof(Vector2Holder.x), out var xObject) &&
                        d.TryGetValue(nameof(Vector2Holder.y), out var yObject) &&
                        xObject is float x &&
                        yObject is float y)
                    {
                        return new Vector2Int((int)x, (int)y);
                    }
                    else if (fieldInfo.value is Vector2Holder h)
                    {
                        return new Vector2Int((int)h.x, (int)h.y);
                    }
                }

                break;

            case Type t when t == typeof(Vector3Int):

                {
                    if (fieldInfo.value is Dictionary<object, object> d &&
                        d.TryGetValue(nameof(Vector3Holder.x), out var xObject) &&
                        d.TryGetValue(nameof(Vector3Holder.y), out var yObject) &&
                        d.TryGetValue(nameof(Vector3Holder.z), out var zObject) &&
                        xObject is float x &&
                        yObject is float y &&
                        zObject is float z)
                    {
                        return new Vector3Int((int)x, (int)y, (int)z);
                    }
                    else if (fieldInfo.value is Vector3Holder h)
                    {
                        return new Vector3Int((int)h.x, (int)h.y, (int)h.z);
                    }
                }

                break;

            case Type t when t == typeof(Vector4Int):

                {
                    if (fieldInfo.value is Dictionary<object, object> d &&
                        d.TryGetValue(nameof(Vector4Holder.x), out var xObject) &&
                        d.TryGetValue(nameof(Vector4Holder.y), out var yObject) &&
                        d.TryGetValue(nameof(Vector4Holder.z), out var zObject) &&
                        d.TryGetValue(nameof(Vector4Holder.w), out var wObject) &&
                        xObject is float x &&
                        yObject is float y &&
                        zObject is float z &&
                        wObject is float w)
                    {
                        return new Vector4Int((int)x, (int)y, (int)z, (int)w);
                    }
                    else if (fieldInfo.value is Vector4Holder h)
                    {
                        return new Vector4Int((int)h.x, (int)h.y, (int)h.z, (int)h.w);
                    }
                }

                break;

            case Type t when t == typeof(IGuidAsset) ||
                t.GetInterface(typeof(IGuidAsset).FullName) != null:

                {
                    if (fieldInfo.value is string s)
                    {
                        return AssetSerialization.GetGuidAsset(t, s);
                    }
                }

                break;

            case Type t when t == typeof(Entity):

                try
                {
                    var ID = Convert.ToInt32(fieldInfo.value);

                    return ID;
                }
                catch(Exception)
                {
                }

                break;

            case Type t when t == typeof(IComponent) ||
                t.GetInterface(typeof(IComponent).FullName) != null:

                {
                    if (fieldInfo.value is string s)
                    {
                        return s;
                    }
                }

                break;

            case Type t when t == typeof(Sprite):

                {
                    if(fieldInfo.value is string s)
                    {
                        var pieces = s.Split(':');

                        if (pieces.Length != 2 ||
                            int.TryParse(pieces[1], out var spriteIndex) == false ||
                            spriteIndex < 0)
                        {
                            return null;
                        }

                        var texture = ResourceManager.instance.LoadTexture(pieces[0]);

                        if (texture == null || spriteIndex >= texture.Sprites.Length)
                        {
                            return null;
                        }

                        return new Sprite()
                        {
                            texture = texture,
                            spriteIndex = spriteIndex,
                        };
                    }
                }

                break;
        }

        return null;
    }

    public object SerializeJsonField(object instance, Type type, FieldInfo field, Type fieldType, StapleSerializationMode mode)
    {
        switch (fieldType)
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

            case Type t when t == typeof(IGuidAsset) ||
                t.GetInterface(typeof(IGuidAsset).FullName) != null:

                {
                    if (instance is IGuidAsset a)
                    {
                        //Normalize the asset path in case the guid is actually a path
                        return AssetSerialization.GetAssetPathFromCache(a.Guid.Guid);
                    }
                }

                break;

            case Type t when t == typeof(Entity):

                {
                    if (instance is Entity e)
                    {
                        return e.Identifier.ID;
                    }
                }

                break;

            case Type t when t == typeof(IComponent) ||
                t.GetInterface(typeof(IComponent).FullName) != null:

                {
                    if (instance is IComponent component &&
                        (World.Current?.TryGetComponentEntity(component, out var e) ?? false))
                    {
                        return $"{e.Identifier.ID}:{t.ToString()}";
                    }
                }

                break;

            case Type t when t == typeof(Sprite):

                {
                    if (instance is Sprite sprite)
                    {
                        return $"{sprite.texture?.Guid.Guid ?? ""}:{sprite.spriteIndex}";
                    }
                }

                break;
        }

        return null;
    }

    public object DeserializeJsonField(Type type, FieldInfo field, Type fieldType, StapleSerializerField fieldInfo,
        JsonElement element, StapleSerializationMode mode)
    {
        switch (fieldType)
        {
            case Type t when t == typeof(Color) && element.ValueKind == JsonValueKind.String:

                return new Color(element.GetString());

            case Type t when t == typeof(Color32) && element.ValueKind == JsonValueKind.String:

                return new Color32(element.GetString());

            case Type t when t == typeof(LayerMask) && element.ValueKind == JsonValueKind.Number:

                return new LayerMask(element.GetNumberValue<uint>());

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
                        return new Rect(xProp.GetNumberValue<int>(),
                            zProp.GetNumberValue<int>(),
                            yProp.GetNumberValue<int>(),
                            wProp.GetNumberValue<int>());
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
                        return new RectFloat(xProp.GetNumberValue<float>(),
                            zProp.GetNumberValue<float>(),
                            yProp.GetNumberValue<float>(),
                            wProp.GetNumberValue<float>());
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
                        return new Vector2Int(xProp.GetNumberValue<int>(),
                            yProp.GetNumberValue<int>());
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
                        return new Vector3Int(xProp.GetNumberValue<int>(),
                            yProp.GetNumberValue<int>(),
                            zProp.GetNumberValue<int>());
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
                        return new Vector4Int(xProp.GetNumberValue<int>(),
                            yProp.GetNumberValue<int>(),
                            zProp.GetNumberValue<int>(),
                            wProp.GetNumberValue<int>());
                    }
                }

                break;

            case Type t when (t == typeof(IGuidAsset) ||
                t.GetInterface(typeof(IGuidAsset).FullName) != null) &&
                element.ValueKind == JsonValueKind.String:

                return AssetSerialization.GetGuidAsset(t, element.GetString());

            case Type t when t == typeof(Entity) &&
                element.ValueKind == JsonValueKind.Number:

                return element.GetNumberValue<int>();

            case Type t when (t == typeof(IComponent) ||
                t.GetInterface(typeof(IComponent).FullName) != null) &&
                element.ValueKind == JsonValueKind.String:

                return element.GetString();

            case Type t when t == typeof(Sprite) &&
                element.ValueKind == JsonValueKind.String:

                {
                    var pieces = element.GetString().Split(':');

                    if(pieces.Length != 2 ||
                        int.TryParse(pieces[1], out var spriteIndex) == false ||
                        spriteIndex < 0)
                    {
                        return null;
                    }

                    var texture = ResourceManager.instance.LoadTexture(pieces[0]);

                    if(texture == null || spriteIndex >= texture.Sprites.Length)
                    {
                        return null;
                    }

                    return new Sprite()
                    {
                        texture = texture,
                        spriteIndex = spriteIndex,
                    };
                }
        }

        return null;
    }
}
