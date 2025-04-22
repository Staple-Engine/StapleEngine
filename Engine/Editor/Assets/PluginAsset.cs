using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Staple.Editor;

/// <summary>
/// Configuration for how to handle a plugin
/// </summary>
[Serializable]
public class PluginAsset : IGuidAsset
{
    private readonly GuidHasher hasher = new();

    [HideInInspector]
    public string guid;

    public bool autoReferenced = false;

    public bool anyPlatform = true;
    public List<AppPlatform> platforms = [];

    [HideInInspector]
    public string typeName = typeof(PluginAsset).FullName;

    public static bool IsAssembly(string path)
    {
        try
        {
            AssemblyName.GetAssemblyName(path);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public GuidHasher Guid => hasher;

    public static object Create(string guid)
    {
        var path = AssetDatabase.GetAssetPath(guid);

        if(path == null)
        {
            return null;
        }

        var text = File.ReadAllText(Path.Combine(StapleEditor.instance.BasePath, path));

        if (text == null)
        {
            return null;
        }

        try
        {
            var outValue = JsonSerializer.Deserialize(text, PluginAssetMetadataSerializationContext.Default.PluginAsset);

            if (outValue != null)
            {
                outValue.guid = guid;
            }

            return outValue;
        }
        catch (Exception e)
        {
            Log.Error($"[PluginAsset] Failed to deserialize: {e}");
        }

        return null;
    }
}

[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(AppPlatform))]
[JsonSerializable(typeof(List<AppPlatform>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(PluginAsset))]
internal partial class PluginAssetMetadataSerializationContext : JsonSerializerContext
{
}
