using Staple.Internal;
using System;
using System.Linq;

namespace Staple.Editor;

[CustomEditor(typeof(FontMetadata))]
internal class FontAssetEditor : AssetEditor
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
            case nameof(FontMetadata.textureSize):

                {
                    var current = (int)getter();

                    var index = Array.IndexOf(TextureMetadata.TextureMaxSizes, current);

                    if (textureMaxSizes.Length == 0)
                    {
                        textureMaxSizes = TextureMetadata.TextureMaxSizes.Select(x => x.ToString()).ToArray();
                    }

                    var newIndex = EditorGUI.Dropdown(name.ExpandCamelCaseName(), "FontAssetTextureSize", textureMaxSizes, index);

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

        ShowAssetUI(() => needsUpdate = true);
    }
}
