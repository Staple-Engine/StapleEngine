using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Staple.ProjectManagement;

/// <summary>
/// Allows creating an assembly definition asset
/// </summary>
[Serializable]
public class AssemblyDefinition : IGuidAsset
{
    private readonly GuidHasher hasher = new();

    [HideInInspector]
    public string guid;

    public bool anyPlatform = true;
    public List<AppPlatform> platforms = [];
    public List<AppPlatform> excludedPlatforms = [];
    public bool allowUnsafeCode;
    public bool autoReferenced = true;
    public List<string> referencedAssemblies = [];
    public bool overrideReferences = false;
    public List<string> referencedPlugins = [];

    public GuidHasher Guid => hasher;

    public static object Create(string guid)
    {
        var path = AssetDatabase.GetAssetPath(guid);

        if (path == null)
        {
            return null;
        }

        var text = File.ReadAllText(StorageUtils.GetRootPath(ProjectManager.Instance.basePath, path));

        if(text == null)
        {
            return null;
        }

        try
        {
            var outValue = JsonSerializer.Deserialize(text, AssemblyDefinitionMetadataSerializationContext.Default.AssemblyDefinition);

            if(outValue != null)
            {
                outValue.guid = guid;
                outValue.Guid.Guid = guid;
            }

            return outValue;
        }
        catch(Exception e)
        {
            Log.Error($"[AssemblyDefinition] Failed to deserialize: {e}");
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
[JsonSerializable(typeof(AssemblyDefinition))]
internal partial class AssemblyDefinitionMetadataSerializationContext : JsonSerializerContext
{
}
