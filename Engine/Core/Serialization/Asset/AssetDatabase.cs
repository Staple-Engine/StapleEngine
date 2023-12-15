using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Staple.Internal;

namespace Staple
{
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

        internal static readonly List<AssetInfo> assets = new();

        internal static List<string> assetDirectories = new();

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
                    };

                    var extension = Path.GetExtension(file.path);

                    //TODO: Do this better
                    switch (extension)
                    {
                        case ".stsh":

                            asset.typeName = typeof(Shader).FullName;

                            break;

                        case ".mat":

                            asset.typeName = typeof(Material).FullName;

                            break;

                        case ".stsc":

                            asset.typeName = typeof(Scene).FullName;

                            break;

                        default:

                            var shortExtension = extension.Substring(1);

                            if(AssetSerialization.TextureExtensions.Contains(shortExtension))
                            {
                                asset.typeName = typeof(Texture).FullName;
                            }
                            else if(AssetSerialization.AudioExtensions.Contains(shortExtension))
                            {
                                asset.typeName = typeof(AudioClip).FullName;
                            }
                            else if(AssetSerialization.MeshExtensions.Contains(shortExtension))
                            {
                                asset.typeName = typeof(Mesh).FullName;
                            }

                            break;
                    }

                    assets.Add(asset);
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
                            if (assets.Any(x => x.guid == holder.guid))
                            {
                                Log.Warning($"[AssetDatabase] Duplicate guid found for '{holder.guid}' at {file}, skipping...");

                                continue;
                            }

                            assets.Add(new AssetInfo()
                            {
                                guid = holder.guid,
                                name = Path.GetFileNameWithoutExtension(file.Replace(".meta", "")),
                                path = file.Replace($"{pair.Key}{Path.DirectorySeparatorChar}", "").Replace("\\", "/").Replace(".meta", ""),
                                typeName = holder.typeName,
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"[AssetDatabase] Missing guid or type name for potential asset at {file}. Skipping... (Exception: {e})");

                        continue;
                    }
                }
            }

            Log.Info($"[AssetDatabase] Reloaded Asset Database with {assets.Count} assets");
        }

        /// <summary>
        /// Gets the path to an asset by guid
        /// </summary>
        /// <param name="guid">The asset guid</param>
        /// <returns>The path or null</returns>
        public static string GetAssetPath(string guid)
        {
            var path = assets.FirstOrDefault(x => x.guid == guid)?.path;

            if(path == null)
            {
                return null;
            }

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
            var path = assets.FirstOrDefault(x => x.guid == guid && x.path.StartsWith(prefix))?.path;

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
            return assets.FirstOrDefault(x => x.guid == guid)?.name;
        }

        /// <summary>
        /// Gets an asset guid for a path
        /// </summary>
        /// <param name="path">The path for the asset</param>
        /// <returns>The guid or null</returns>
        public static string GetAssetGuid(string path)
        {
            return assets.FirstOrDefault(x => x.path == path)?.guid;
        }

        /// <summary>
        /// Gets an asset guid for a path with a prefix
        /// </summary>
        /// <param name="path">The path for the asset</param>
        /// <param name="prefix">The prefix to search for</param>
        /// <returns>The guid or null</returns>
        public static string GetAssetGuid(string path, string prefix)
        {
            var t = prefix + path;

            return assets.FirstOrDefault(x => x.path == t)?.guid;
        }
    }
}
