using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;

namespace Staple.Editor;

[CustomEditor(typeof(FontMetadata))]
internal class FontAssetEditor : Editor
{
    private string[] textureMaxSizes = Array.Empty<string>();

    public override bool RenderField(FieldInfo field)
    {
        var metadata = target as FontMetadata;

        switch (field.Name)
        {
            case nameof(FontMetadata.typeName):
            case nameof(FontMetadata.guid):

                return true;

            case nameof(FontMetadata.textureSize):

                {
                    var current = (int)field.GetValue(target);

                    var index = Array.IndexOf(TextureMetadata.TextureMaxSizes, current);

                    if (textureMaxSizes.Length == 0)
                    {
                        textureMaxSizes = TextureMetadata.TextureMaxSizes.Select(x => x.ToString()).ToArray();
                    }

                    var newIndex = EditorGUI.Dropdown(field.Name.ExpandCamelCaseName(), textureMaxSizes, index);

                    if (index != newIndex)
                    {
                        field.SetValue(target, TextureMetadata.TextureMaxSizes[newIndex]);
                    }

                    return true;
                }
        }

        return base.RenderField(field);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var metadata = (FontMetadata)target;
        var originalMetadata = (FontMetadata)original;

        var hasChanges = metadata != originalMetadata;

        if (hasChanges)
        {
            EditorGUI.Button("Apply", () =>
            {
                try
                {
                    var text = JsonConvert.SerializeObject(metadata, Formatting.Indented);

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
            });

            EditorGUI.SameLine();

            EditorGUI.Button("Revert", () =>
            {
                var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(metadata, field.GetValue(original));
                }
            });
        }
        else
        {
            EditorGUI.ButtonDisabled("Apply", null);

            EditorGUI.SameLine();

            EditorGUI.ButtonDisabled("Revert", null);
        }
    }
}
