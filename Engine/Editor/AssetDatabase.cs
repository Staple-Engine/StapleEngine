using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Staple.Internal;

namespace Staple.Editor
{
    internal static class AssetDatabase
    {
        private static readonly Dictionary<string, AssetHolder> assetsByGuid = new();
        private static readonly Dictionary<string, AssetHolder> assetsByType = new();

        internal static string basePath;

        public static void Reload()
        {
            assetsByGuid.Clear();
            assetsByType.Clear();

            var files = Array.Empty<string>();

            try
            {
                files = Directory.GetFiles(basePath, "*.meta", SearchOption.AllDirectories)
                    .Where(x => x.Contains($"Cache{Path.DirectorySeparatorChar}Staging") == false)
                    .ToArray();
            }
            catch(Exception)
            {
            }

            try
            {
                files = files
                    .Concat(Directory.GetFiles(Path.Combine(StapleEditor.StapleBasePath, "Builtin Resources"), "*.meta", SearchOption.AllDirectories))
                    .ToArray();
            }
            catch(Exception)
            {
            }

            foreach(var file in files)
            {
                try
                {
                    var text = File.ReadAllText(file);

                    var holder = JsonConvert.DeserializeObject<AssetHolder>(text);

                    if (holder != null && (holder.guid?.Length ?? 0) > 0 && (holder.typeName?.Length ?? 0) > 0)
                    {
                        if(assetsByGuid.ContainsKey(holder.guid))
                        {
                            Log.Warning($"[AssetDatabase] Duplicate guid found for '{holder.guid}' at {file}, skipping...");

                            continue;
                        }

                        assetsByGuid.AddOrSetKey(holder.guid, holder);
                        assetsByType.AddOrSetKey(holder.typeName, holder);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"[AssetDatabase] Missing guid or type name for potential asset at {file}. Skipping... (Exception: {e})");

                    continue;
                }
            }

            Log.Info($"[AssetDatabase] Reloaded Asset Database with {assetsByGuid.Count} assets");
        }
    }
}
