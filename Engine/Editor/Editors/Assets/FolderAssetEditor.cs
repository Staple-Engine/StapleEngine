using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.IO;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(FolderAsset))]
internal class FolderAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var asset = (FolderAsset)target;
        var originalAsset = (FolderAsset)original;

        var hasChanges = asset != originalAsset;

        if (hasChanges)
        {
            EditorGUI.Button("Apply", "FolderApply", () =>
            {
                try
                {
                    var text = JsonConvert.SerializeObject(asset, Formatting.Indented, new JsonSerializerSettings()
                    {
                        Converters =
                        {
                            new StringEnumConverter(),
                        }
                    });

                    File.WriteAllText(path, text);
                }
                catch (Exception)
                {
                }

                var fields = asset.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(originalAsset, field.GetValue(asset));
                }
            });

            EditorGUI.SameLine();

            EditorGUI.Button("Revert", "FolderRevert", () =>
            {
                var fields = asset.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(asset, field.GetValue(originalAsset));
                }
            });
        }
        else
        {
            EditorGUI.ButtonDisabled("Apply", "FolderApply", null);

            EditorGUI.SameLine();

            EditorGUI.ButtonDisabled("Revert", "FolderRevert", null);
        }
    }
}
