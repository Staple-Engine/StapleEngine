using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Staple.UI;

[Serializable]
public class UISkin
{
    public Dictionary<string, Dictionary<string, object>> data = [];

    public bool TryGetProperty(string className, string key, out object value)
    {
        if(data.TryGetValue(className, out var pairs) == false)
        {
            value = null;

            return false;
        }

        return pairs.TryGetValue(key, out value);
    }

    public Texture GetTexture(string className, string key)
    {
        if(TryGetProperty(className, key, out var p) &&
            p is JsonElement t && t.ValueKind == JsonValueKind.String)
        {
            return ResourceManager.instance.LoadTexture(t.GetString());
        }

        return null;
    }

    public Vector2Int GetVector2Int(string className, string key)
    {
        if (TryGetProperty(className, key, out var p) &&
            p is JsonElement v && v.ValueKind == JsonValueKind.Object &&
            v.GetProperty("x") is JsonElement x && x.ValueKind == JsonValueKind.Number &&
            v.GetProperty("y") is JsonElement y && y.ValueKind == JsonValueKind.Number)
        {
            return new Vector2Int(x.GetNumberValue<int>(), y.GetNumberValue<int>());
        }

        return Vector2Int.Zero;
    }

    public Rect GetRect(string className, string key)
    {
        if (TryGetProperty(className, key, out var p) &&
            p is JsonElement r && r.ValueKind == JsonValueKind.Object &&
            r.GetProperty("left") is JsonElement left && left.ValueKind == JsonValueKind.Number &&
            r.GetProperty("right") is JsonElement right && right.ValueKind == JsonValueKind.Number &&
            r.GetProperty("top") is JsonElement top && top.ValueKind == JsonValueKind.Number &&
            r.GetProperty("bottom") is JsonElement bottom && bottom.ValueKind == JsonValueKind.Number)
        {
            return new Rect(left.GetNumberValue<int>(), right.GetNumberValue<int>(), top.GetNumberValue<int>(), bottom.GetNumberValue<int>());
        }

        return default;
    }

    public Color GetColor(string className, string key)
    {
        if(TryGetProperty(className, key, out var p) &&
            p is JsonElement c && c.ValueKind == JsonValueKind.String)
        {
            return new Color(c.GetString());
        }

        return Color.White;
    }

    public int GetInt(string className, string key)
    {
        if (TryGetProperty(className, key, out var p) &&
            p is JsonElement i && i.ValueKind == JsonValueKind.Number)
        {
            return i.GetNumberValue<int>();
        }

        return 0;
    }

    public string GetString(string className, string key)
    {
        if (TryGetProperty(className, key, out var p) &&
            p is JsonElement str && str.ValueKind == JsonValueKind.String)
        {
            return str.GetString();
        }

        return null;
    }
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, object>>))]
[JsonSerializable(typeof(UISkin))]
internal partial class UISkinSerializationContext : JsonSerializerContext
{
}
