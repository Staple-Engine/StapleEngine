using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Editor
{
    internal static class AssetDatabase
    {
        [Serializable]
        private class GuidHolder
        {
            public string guid;
        }

        private static Dictionary<string, string> assets = new();
        internal static string basePath;

        public static void Reload()
        {
            assets.Clear();

            var files = new string[0];

            try
            {
                files = Directory.GetFiles(basePath, "*.meta", SearchOption.AllDirectories);
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
                string guid = null;

                if(file.Contains(".stsh"))
                {
                    try
                    {
                        guid = File.ReadAllText(file);
                    }
                    catch(Exception)
                    {
                        continue;
                    }
                }
                else
                {
                    try
                    {
                        var text = File.ReadAllText(file);

                        var holder = JsonConvert.DeserializeObject<GuidHolder>(text);

                        if(holder != null)
                        {
                            guid = holder.guid;
                        }
                    }
                    catch(Exception)
                    {
                        continue;
                    }
                }

                if((guid?.Length ?? 0) > 0)
                {
                    if(assets.ContainsKey(guid))
                    {
                        Log.Error($"[AssetDatabase] Asset {file} has duplicate guid. Skipping...");

                        continue;
                    }

                    assets.Add(guid, file);
                }
            }

            Log.Info($"[AssetDatabase] Reloaded Asset Database with {assets.Count} assets");
        }
    }
}
