using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.IO;

namespace Staple.Editor;

partial class StapleEditor
{
    public void CreatePrefabFromDragged()
    {
        var entity = draggedEntity;

        draggedEntity = default;

        if (entity.IsValid)
        {
            var prefab = SceneSerialization.SerializeIntoPrefab(entity);

            if (prefab != null)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(prefab, Formatting.Indented, new JsonSerializerSettings()
                    {
                        Converters =
                        {
                            new StringEnumConverter(),
                        }
                    });

                    var currentPath = projectBrowser.currentContentNode?.path ??
                        Path.Combine(projectBrowser.basePath, "Assets");

                    var fileName = GetProperFileName(currentPath, entity.Name, Path.Combine(currentPath, $"{entity.Name}.stpr"), "stpr");

                    File.WriteAllText(fileName, json);

                    json = JsonConvert.SerializeObject(new AssetHolder()
                    {
                        guid = prefab.guid,
                        typeName = typeof(Prefab).FullName,
                    },
                    Formatting.Indented, new JsonSerializerSettings()
                    {
                        Converters =
                        {
                            new StringEnumConverter(),
                        }
                    });

                    File.WriteAllText($"{fileName}.meta", json);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
