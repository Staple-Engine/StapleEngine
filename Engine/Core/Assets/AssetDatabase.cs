using MessagePack;
using Staple.Internal;
using Staple.Jobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Staple;

/// <summary>
/// Asset Database containing data on each asset in a project
/// </summary>
public static class AssetDatabase
{
    internal static SerializableAssetDatabase database = new();

    /// <summary>
    /// Asset Path to Guids
    /// </summary>
    internal static readonly Dictionary<string, string> assetGuids = [];

    /// <summary>
    /// Asset Type to Asset Array
    /// </summary>
    internal static readonly Dictionary<string, List<string>> assetsByType = [];

    internal static readonly Lock threadLock = new();

    internal static List<string> assetDirectories = [];

    internal static IAssetDatabaseObserver databaseObserver;

    internal static bool debug = false;

    /// <summary>
    /// Callback to resolve asset paths, if needed.
    /// </summary>
    public static Func<string, string> assetPathResolver;

    /// <summary>
    /// Reloads the asset database. This will scan all files in resource paks and any additional directories in `assetDirectories`.
    /// </summary>
    /// <param name="path">Path to the asset database file</param>
    /// <param name="onFinish">Called when the reload is finished</param>
    public static void Reload(string path, Action onFinish)
    {
        database.assets.Clear();

        assetGuids.Clear();
        assetsByType.Clear();

        if(path != null)
        {
            try
            {
                if (File.Exists(path))
                {
                    var data = File.ReadAllBytes(path);

                    using var stream = new MemoryStream(data);

                    var header = MessagePackSerializer.Deserialize<SerializableAssetDatabaseHeader>(stream);

                    if (header == null ||
                        header.header.SequenceEqual(SerializableAssetDatabaseHeader.ValidHeader) == false ||
                        header.version != SerializableAssetDatabaseHeader.ValidVersion)
                    {
                        throw new Exception("Invalid header");
                    }

                    var database = MessagePackSerializer.Deserialize<SerializableAssetDatabase>(stream);

                    if (database?.assets == null)
                    {
                        throw new Exception("Invalid data");
                    }

                    var pathsToGuids = new Dictionary<string, List<string>>();
                    var guidsToRemove = new List<string>();

                    foreach (var pair in database.assets)
                    {
                        if (pair.Value == null ||
                            pair.Value.Any(x => x == null))
                        {
                            throw new Exception("Invalid data");
                        }

                        if(Platform.IsEditor)
                        {
                            for (var i = pair.Value.Count - 1; i >= 0; i--)
                            {
                                var found = false;

                                var item = pair.Value[i];

                                foreach(var resourcePair in ResourceManager.instance.resourcePaks)
                                {
                                    foreach(var file in resourcePair.Value.Files)
                                    {
                                        if(file.path == item.path)
                                        {
                                            found = true;

                                            break;
                                        }
                                    }

                                    if(found)
                                    {
                                        break;
                                    }
                                }

                                if(found == false)
                                {
                                    foreach (var directory in assetDirectories)
                                    {
                                        try
                                        {
                                            //Remove the topmost directory name due to the way we resolve local paths
                                            var p = Path.Combine(Path.GetDirectoryName(directory), item.path);

                                            if (File.Exists(p) ||
                                                Directory.Exists(p))
                                            {
                                                if(pathsToGuids.TryGetValue(item.path, out var guids) == false)
                                                {
                                                    guids = [];

                                                    pathsToGuids.Add(item.path, guids);
                                                }

                                                guids.Add(item.guid);

                                                found = true;

                                                break;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                        }

                                        if(found)
                                        {
                                            break;
                                        }
                                    }
                                }

                                if (found == false)
                                {
                                    if(debug)
                                    {
                                        Log.Debug($"[AssetDatabase] Removing {item.path}: File not found");
                                    }

                                    pair.Value.RemoveAt(i);
                                }
                            }
                        }

                        if (pair.Value.Count == 0)
                        {
                            guidsToRemove.Add(pair.Key);
                        }

                        foreach (var item in pair.Value)
                        {
                            assetGuids.AddOrSetKey(item.path, item.guid);

                            if (assetsByType.TryGetValue(item.typeName, out var list) == false)
                            {
                                list = [];

                                assetsByType.Add(item.typeName, list);
                            }

                            list.Add(item.guid);
                        }
                    }

                    foreach(var pair in pathsToGuids)
                    {
                        if(pair.Value.Count > 1)
                        {
                            foreach(var guid in pair.Value)
                            {
                                database.assets.Remove(guid);
                            }
                        }
                    }

                    foreach(var guid in guidsToRemove)
                    {
                        database.assets.Remove(guid);
                    }

                    AssetDatabase.database = database;
                }
            }
            catch (Exception e)
            {
                database = new();

                assetGuids.Clear();
                assetsByType.Clear();

                Log.Error($"[AssetDatabase] Failed to load cached asset database:\n{e}\nRebuilding...");
            }
        }

        void AddAsset(SerializableAssetDatabaseAssetInfo asset)
        {
            if(database.assets.TryGetValue(asset.guid, out var container) == false)
            {
                container = [];

                database.assets.Add(asset.guid, container);
            }

            foreach(var item in container)
            {
                if(item.guid == asset.guid &&
                    item.typeName == asset.typeName &&
                    item.name == asset.name &&
                    item.path == asset.path)
                {
                    return;
                }
            }

            container.Add(asset);

            assetGuids.AddOrSetKey(asset.path, asset.guid);

            if(assetsByType.TryGetValue(asset.typeName, out var list) == false)
            {
                list = [];

                assetsByType.Add(asset.typeName, list);
            }

            list.Add(asset.guid);
        }

        foreach(var pair in Mesh.defaultMeshes)
        {
            var asset = new SerializableAssetDatabaseAssetInfo()
            {
                guid = pair.Key,
                path = pair.Key,
                name = pair.Key.Replace("Internal/", ""),
                typeName = typeof(Mesh).FullName,
                lastModified = DateTime.UtcNow.Ticks,
            };

            AddAsset(asset);
        }

        foreach (var pak in ResourceManager.instance.resourcePaks)
        {
            foreach (var file in pak.Value.Files)
            {
                var asset = new SerializableAssetDatabaseAssetInfo()
                {
                    guid = file.guid,
                    path = file.path,
                    name = Path.GetFileNameWithoutExtension(file.path.Replace(".meta", "")),
                    typeName = file.typeName,
                    lastModified = DateTime.UtcNow.Ticks,
                };

                AddAsset(asset);
            }
        }

        var files = new Dictionary<string, string[]>();

        if(Platform.IsEditor)
        {
            foreach (var directory in assetDirectories)
            {
                try
                {
                    files.Add(directory, Directory.GetFiles(directory, "*.meta", SearchOption.AllDirectories)
                        .Where(x => x.Contains($"Cache{Path.DirectorySeparatorChar}Staging") == false)
                        .ToArray());
                }
                catch (Exception)
                {
                }
            }
        }

        var fileCount = files.Select(x => x.Value.Length).Sum();

        var counter = fileCount;

        void Counter(string message)
        {
            lock (threadLock)
            {
                counter--;

                if (counter <= 0)
                {
                    if(Platform.IsEditor)
                    {
                        try
                        {
                            using var stream = File.OpenWrite(path);
                            using var writer = new BinaryWriter(stream);

                            var header = new SerializableAssetDatabaseHeader();

                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(database));

                            writer.Write(encoded.ToArray());
                        }
                        catch (Exception)
                        {
                        }
                    }

                    Log.Info($"[AssetDatabase] Reloaded Asset Database with {database.assets.Count} assets");

                    onFinish?.Invoke();
                }
                else
                {
                    databaseObserver?.AssetDatabaseSetProgress((fileCount - counter) / (float)fileCount, message);
                }
            }
        }

        foreach (var pair in files)
        {
            var key = pair.Key;

            foreach (var file in pair.Value)
            {
                var f = file;

                JobScheduler.Schedule(new ActionJob(() =>
                {
                    try
                    {
                        var prefix = f.Replace('\\', '/').Contains("/Cache/Packages/") ? "" : $"{Path.GetFileName(key)}/";
                        var localPath = f.Replace($"{key}{Path.DirectorySeparatorChar}", prefix).Replace("\\", "/").Replace(".meta", "");

                        var lastModifiedTicks = (long)0;

                        lock (threadLock)
                        {
                            if(assetGuids.TryGetValue(localPath, out var guid) &&
                                database.assets.TryGetValue(guid, out var content))
                            {
                                foreach(var item in content)
                                {
                                    if(item.path == localPath)
                                    {
                                        lastModifiedTicks = item.lastModified;

                                        break;
                                    }
                                }
                            }
                        }

                        var currentModifiedTicks = long.MaxValue;

                        try
                        {
                            var lastModifiedMeta = File.GetLastWriteTimeUtc(f);

                            if(lastModifiedMeta != null)
                            {
                                currentModifiedTicks = lastModifiedMeta.Ticks;
                            }
                        }
                        catch(Exception)
                        {
                        }

                        if(currentModifiedTicks <= lastModifiedTicks)
                        {
                            Counter(Path.GetFileNameWithoutExtension(f));

                            return;
                        }

                        var text = File.ReadAllText(f);

                        var holder = JsonSerializer.Deserialize(text, AssetHolderSerializationContext.Default.AssetHolder);

                        if (holder != null && (holder.guid?.Length ?? 0) > 0 && (holder.typeName?.Length ?? 0) > 0)
                        {
                            var asset = new SerializableAssetDatabaseAssetInfo()
                            {
                                guid = holder.guid,
                                name = Path.GetFileNameWithoutExtension(f.Replace(".meta", "")),
                                path = localPath,
                                typeName = holder.typeName,
                                lastModified = currentModifiedTicks,
                            };

                            lock (threadLock)
                            {
                                AddAsset(asset);
                            }
                        }

                        Counter(Path.GetFileNameWithoutExtension(f));
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"[AssetDatabase] Missing guid or type name for potential asset at {f}. Skipping... (Exception: {e})");

                        Counter(Path.GetFileNameWithoutExtension(f));
                    }
                }));
            }
        }

        if(files.Count == 0)
        {
            Counter("Finished");
        }
    }

    /// <summary>
    /// Gets the asset entry for a specific asset.
    /// </summary>
    /// <param name="guid">The asset guid</param>
    /// <returns>The entry or null</returns>
    internal static SerializableAssetDatabaseAssetInfo GetAssetEntry(string guid) => database.assets.TryGetValue(guid, out var a) &&
        (a?.Count ?? 0) > 0 ? a[0] : null;

    /// <summary>
    /// Gets the path to an asset by guid
    /// </summary>
    /// <param name="guid">The asset guid</param>
    /// <returns>The path or null</returns>
    public static string GetAssetPath(string guid)
    {
        if(database.assets.TryGetValue(guid, out var container) == false ||
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

        if(database.assets.TryGetValue(guid, out var container) == false)
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
    /// Gets the type name of an asset
    /// </summary>
    /// <param name="guid">The asset guid</param>
    /// <returns>The asset type, or null</returns>
    public static string GetAssetType(string guid) => database.assets.TryGetValue(guid, out var container) &&
        (container?.Count ?? 0) > 0 ? container[0].typeName : null;

    /// <summary>
    /// Attempts to find an asset of a specific type name
    /// </summary>
    /// <param name="typeName">The full name of the type the asset should have</param>
    /// <returns>A list of asset guids</returns>
    public static string[] FindAssetsByType(string typeName) => assetsByType.TryGetValue(typeName, out var c) ? c.ToArray() : [];

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
