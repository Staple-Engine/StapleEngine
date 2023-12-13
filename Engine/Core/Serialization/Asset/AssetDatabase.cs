using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Staple.Internal;

namespace Staple
{
    public static class AssetDatabase
    {
        public class AssetInfo
        {
            public string guid;
            public string name;
            public string typeName;
            public string path;
        }

        internal static readonly List<AssetInfo> assets = new();

        internal static List<string> assetPaths = new();

        public static void Reload()
        {
            assets.Clear();

            var files = Array.Empty<string>();

            foreach(var path in assetPaths)
            {
                try
                {
                    files = Directory.GetFiles(path, "*.meta", SearchOption.AllDirectories)
                        .Where(x => x.Contains($"Cache{Path.DirectorySeparatorChar}Staging") == false)
                        .ToArray();
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

                    //TODO: Do this better
                    switch (Path.GetExtension(file.path))
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

                        case ".wav":
                        case ".mp3":
                        case ".ogg":

                            asset.typeName = typeof(AudioClip).FullName;

                            break;

                        case ".png": //TODO

                            asset.typeName = typeof(Texture).FullName;

                            break;
                    }

                    assets.Add(asset);
                }
            }

            foreach (var file in files)
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
                            path = file.Replace(".meta", ""),
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

            Log.Info($"[AssetDatabase] Reloaded Asset Database with {assets.Count} assets");
        }

        public static string GetAssetPath(string guid)
        {
            return assets.FirstOrDefault(x => x.guid == guid)?.path;
        }

        public static string GetAssetPath(string guid, string prefix)
        {
            return assets.FirstOrDefault(x => x.guid == guid && x.path.StartsWith(prefix))?.path;
        }

        public static string GetAssetName(string guid)
        {
            return assets.FirstOrDefault(x => x.guid == guid)?.name;
        }

        public static string GetAssetGuid(string path)
        {
            return assets.FirstOrDefault(x => x.path == path)?.guid;
        }
    }
}
