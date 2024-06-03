using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(FontMetadata))]
internal class FontAssetEditor : Editor
{
    private int characterCount = 0;
    private bool isValid = false;
    private bool needsUpdate = true;

    private string[] textureMaxSizes = Array.Empty<string>();

    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var metadata = target as FontMetadata;

        switch (name)
        {
            case nameof(FontMetadata.typeName):
            case nameof(FontMetadata.guid):

                return true;

            case nameof(FontMetadata.textureSize):

                {
                    var current = (int)getter();

                    var index = Array.IndexOf(TextureMetadata.TextureMaxSizes, current);

                    if (textureMaxSizes.Length == 0)
                    {
                        textureMaxSizes = TextureMetadata.TextureMaxSizes.Select(x => x.ToString()).ToArray();
                    }

                    var newIndex = EditorGUI.Dropdown(name.ExpandCamelCaseName(), textureMaxSizes, index);

                    if (index != newIndex)
                    {
                        setter(TextureMetadata.TextureMaxSizes[newIndex]);
                    }

                    return true;
                }
        }

        return false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var metadata = (FontMetadata)target;
        var originalMetadata = (FontMetadata)original;

        if(EditorGUI.Changed)
        {
            needsUpdate = true;
        }

        if(needsUpdate)
        {
            needsUpdate = false;

            characterCount = 0;

            foreach(var value in Enum.GetValues<FontCharacterSet>())
            {
                if(metadata.includedCharacterSets.HasFlag(value) && TextFont.characterRanges.TryGetValue(value, out var range))
                {
                    characterCount += (range.Item2 - range.Item1) + 1;
                }
            }

            isValid = FontAsset.IsValid(cachePath, metadata);
        }

        EditorGUI.Label($"Character Count: {characterCount}");

        if(isValid == false)
        {
            EditorGUI.Label("Warning: Texture size is not large enough!");
        }

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

                EditorUtils.RefreshAssets(false, () =>
                {
                    needsUpdate = true;
                });
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
