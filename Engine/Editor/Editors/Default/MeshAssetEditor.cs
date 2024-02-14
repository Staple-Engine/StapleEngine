using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.IO;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(MeshAssetMetadata))]
internal class MeshAssetEditor : Editor
{
    public override bool RenderField(FieldInfo field)
    {
        if(field.Name == nameof(MeshAssetMetadata.typeName) ||
            field.Name == nameof(MeshAssetMetadata.guid))
        {
            return true;
        }

        return base.RenderField(field);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var metadata = (MeshAssetMetadata)target;
        var originalMetadata = (MeshAssetMetadata)original;

        var hasChanges = metadata != originalMetadata;

        if (hasChanges)
        {
            if (EditorGUI.Button("Apply"))
            {
                try
                {
                    var text = JsonConvert.SerializeObject(metadata, Formatting.Indented, new JsonSerializerSettings()
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

                var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(original, field.GetValue(metadata));
                }

                EditorUtils.RefreshAssets(false, null);
            }

            EditorGUI.SameLine();

            if (EditorGUI.Button("Revert"))
            {
                var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(metadata, field.GetValue(original));
                }
            }
        }
        else
        {
            EditorGUI.ButtonDisabled("Apply");

            EditorGUI.SameLine();

            EditorGUI.ButtonDisabled("Revert");
        }
    }
}
