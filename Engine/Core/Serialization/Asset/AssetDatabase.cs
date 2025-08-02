using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Staple.Internal;

namespace Staple;

/// <summary>
/// Asset Database containing data on each asset in a project
/// </summary>
public static class AssetDatabase
{
    public class AssetInfo
    {
        public string guid;
        public string name;
        public string typeName;
        public string path;

        public override string ToString()
        {
            return $"{guid} {name} {typeName}";
        }
    }

    internal static readonly Dictionary<string, List<AssetInfo>> assets = [];
    internal static readonly Dictionary<string, string> assetGuids = [];
    internal static readonly Dictionary<string, string[]> assetsByType = [];

    internal static List<string> assetDirectories = [];

    /// <summary>
    /// Callback to resolve asset paths, if needed.
    /// </summary>
    public static Func<string, string> assetPathResolver;

    /// <summary>
    /// Reloads the asset database. This will scan all files in resource paks and any additional directories in `assetDirectories`.
    /// </summary>
    public static void Reload()
    {
        assets.Clear();
        assetGuids.Clear();
        assetsByType.Clear();

        var typeList = new Dictionary<string, List<string>>();

        void AddAsset(AssetInfo asset)
        {
            if(assets.TryGetValue(asset.guid, out var container) == false)
            {
                container = [];

                assets.Add(asset.guid, container);
            }

            container.Add(asset);

            assetGuids.AddOrSetKey(asset.path, asset.guid);

            if(typeList.TryGetValue(asset.typeName, out var types) == false)
            {
                types = [];

                typeList.Add(asset.typeName, types);
            }

            types.Add(asset.guid);
        }

        foreach(var pair in Mesh.defaultMeshes)
        {
            var asset = new AssetInfo()
            {
                guid = pair.Key,
                path = pair.Key,
                name = pair.Key.Replace("Internal/", ""),
                typeName = typeof(Mesh).FullName,
            };

            AddAsset(asset);
        }

        var files = new Dictionary<string, string[]>();

        foreach(var path in assetDirectories)
        {
            try
            {
                files.Add(path, Directory.GetFiles(path, "*.meta", SearchOption.AllDirectories)
                    .Where(x => x.Contains($"Cache{Path.DirectorySeparatorChar}Staging") == false)
                    .ToArray());
            }
            catch (Exception)
            {
            }
        }

        foreach (var pak in ResourceManager.instance.resourcePaks)
        {
            foreach (var file in pak.Value.Files)
            {
                var asset = new AssetInfo()
                {
                    guid = file.guid,
                    path = file.path,
                    name = Path.GetFileNameWithoutExtension(file.path.Replace(".meta", "")),
                    typeName = file.typeName,
                };

                AddAsset(asset);
            }
        }

        foreach (var pair in files)
        {
            foreach(var file in pair.Value)
            {
                try
                {
                    var text = File.ReadAllText(file);

                    var holder = JsonSerializer.Deserialize(text, AssetHolderSerializationContext.Default.AssetHolder);

                    if (holder != null && (holder.guid?.Length ?? 0) > 0 && (holder.typeName?.Length ?? 0) > 0)
                    {
                        if (assets.ContainsKey(holder.guid))
                        {
                            Log.Warning($"[AssetDatabase] Duplicate guid found for '{holder.guid}' at {file}, skipping...");

                            continue;
                        }

                        var prefix = file.Replace('\\', '/').Contains("/Cache/Packages/") ? "" : $"{Path.GetFileName(pair.Key)}/";

                        var asset = new AssetInfo()
                        {
                            guid = holder.guid,
                            name = Path.GetFileNameWithoutExtension(file.Replace(".meta", "")),
                            path = file.Replace($"{pair.Key}{Path.DirectorySeparatorChar}", prefix).Replace("\\", "/").Replace(".meta", ""),
                            typeName = holder.typeName,
                        };

                        AddAsset(asset);

                        if(holder.typeName == typeof(Shader).FullName)
                        {
                            var shaderPath = ResourceManager.ShaderPrefix + asset.path;

                            if(asset.path.StartsWith("Assets/"))
                            {
                                shaderPath = string.Concat("Assets/", ResourceManager.ShaderPrefix, asset.path.AsSpan("Assets/".Length));
                            }

                            AddAsset(new()
                            {
                                guid = holder.guid,
                                name = asset.name,
                                path = shaderPath,
                                typeName = holder.typeName,
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[AssetDatabase] Missing guid or type name for potential asset at {file}. Skipping... (Exception: {e})");

                    continue;
                }
            }
        }

        foreach(var pair in typeList)
        {
            assetsByType.Add(pair.Key, pair.Value.ToArray());
        }

        Log.Info($"[AssetDatabase] Reloaded Asset Database with {assets.Count} assets");
    }

    /// <summary>
    /// Gets the asset entry for a specific asset.
    /// </summary>
    /// <param name="guid">The asset guid</param>
    /// <returns>The entry or null</returns>
    internal static AssetInfo GetAssetEntry(string guid) => assets.TryGetValue(guid, out var a) &&
        (a?.Count ?? 0) > 0 ? a[0] : null;

    /// <summary>
    /// Gets the path to an asset by guid
    /// </summary>
    /// <param name="guid">The asset guid</param>
    /// <returns>The path or null</returns>
    public static string GetAssetPath(string guid)
    {
        if(assets.TryGetValue(guid, out var container) == false ||
            (container?.Count ?? 0) == 0)
        {
            return null;
        }

        var path = container[0].path;

        return assetPathResolver?.Invoke(path) ?? path;
    }

    /// <summary>
    /// Gets the path to an asset by guid and filtering for a specific prefix. This is important for shaders.
    /// </summary>
    /// <param name="guid">The asset guid</param>
    /// <param name="prefix">The prefix to search for</param>
    /// <returns>The path or null</returns>
    public static string GetAssetPath(string guid, string prefix)
    {
        var assetsPrefix = "Assets/" + prefix;

        if(assets.TryGetValue(guid, out var container) == false)
        {
            return null;
        }

        string path = null;

        for(var i = 0; i < container.Count; i++)
        {
            var asset = container[i];

            if (asset.path.StartsWith(prefix) || asset.path.StartsWith(assetsPrefix))
            {
                path = asset.path;

                break;
            }
        }

        if (path == null)
        {
            return null;
        }

        return assetPathResolver?.Invoke(path) ?? path;
    }

    /// <summary>
    /// Gets the name for an asset for a guid
    /// </summary>
    /// <param name="guid">The asset guid</param>
    /// <returns>The name or null</returns>
    public static string GetAssetName(string guid)
    {
        if(guid == null)
        {
            return null;
        }

        if(guid.Contains(':'))
        {
            var parts = guid.Split(':');

            if(parts.Length == 2)
            {
                guid = parts[0];
            }
        }

        return GetAssetEntry(guid)?.name;
    }

    /// <summary>
    /// Gets an asset guid for a path
    /// </summary>
    /// <param name="path">The path for the asset</param>
    /// <returns>The guid or null</returns>
    public static string GetAssetGuid(string path) => assetGuids.TryGetValue(path, out var guid) ? guid : null;

    /// <summary>
    /// Gets an asset guid for a path with a prefix
    /// </summary>
    /// <param name="path">The path for the asset</param>
    /// <param name="prefix">The prefix to search for</param>
    /// <returns>The guid or null</returns>
    public static string GetAssetGuid(string path, string prefix)
    {
        var t = prefix + path;

        return GetAssetGuid(t);
    }

    /// <summary>
    /// Gets the type name of an asset
    /// </summary>
    /// <param name="guid">The asset guid</param>
    /// <returns>The asset type, or null</returns>
    public static string GetAssetType(string guid) => assets.TryGetValue(guid, out var container) &&
        (container?.Count ?? 0) > 0 ? container[0].typeName : null;

    /// <summary>
    /// Attempts to find an asset of a specific type name
    /// </summary>
    /// <param name="typeName">The full name of the type the asset should have</param>
    /// <returns>A list of asset guids</returns>
    public static string[] FindAssetsByType(string typeName) => assetsByType.TryGetValue(typeName, out var c) ? c : [];

    /// <summary>
    /// Attempts to resolve the full path of an asset by guid
    /// </summary>
    /// <param name="guid">The asset guid</param>
    /// <returns>The path, or null</returns>
    public static string ResolveAssetFullPath(string guid, string prefix = "")
    {
        var path = GetAssetPath(guid, prefix);

        if(path == null)
        {
            return null;
        }

        foreach(var directory in assetDirectories)
        {
            var target = Path.Combine(directory, path);

            if (File.Exists(target) ||
                Directory.Exists(target))
            {
                return target;
            }
        }

        return null;
    }
}
