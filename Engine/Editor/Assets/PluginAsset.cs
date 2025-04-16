using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Staple.Editor;

/// <summary>
/// Configuration for how to handle a plugin
/// </summary>
[Serializable]
public class PluginAsset
{
    [HideInInspector]
    public string guid;

    public bool autoReferenced = true;

    public bool anyPlatform = true;
    public List<AppPlatform> platforms = [];

    [HideInInspector]
    public string typeName = typeof(PluginAsset).FullName;
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
