namespace Staple.Editor;

[CustomEditor(typeof(TextAsset))]
internal class TextAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var asset = (TextAsset)target;

        if(string.IsNullOrEmpty(asset.text))
        {
            EditorGUI.Label($"Binary File ({EditorUtils.ByteSizeString(asset.bytes?.Length ?? 0)})");
        }
        else
        {
            EditorGUI.Disabled(true, () =>
            {
                EditorGUI.TextFieldMultiline("Text", "TextAssetEditor.Text", asset.text,
                    new(EditorGUI.RemainingHorizontalSpace(), EditorGUI.RemainingVerticalSpace()));
            });
        }
    }
}
