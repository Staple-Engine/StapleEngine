namespace Staple.Editor;

[CustomEditor(typeof(TextAsset))]
internal class TextAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var asset = (TextAsset)target;

        if(string.IsNullOrEmpty(asset.Text))
        {
            EditorGUI.Label($"Binary File ({EditorUtils.ByteSizeString(asset.Bytes?.Length ?? 0)})");
        }
        else
        {
            EditorGUI.Disabled(true, () =>
            {
                EditorGUI.TextFieldMultiline("Text", "TextAssetEditor.Text", asset.Text,
                    new(EditorGUI.RemainingHorizontalSpace(), EditorGUI.RemainingVerticalSpace()));
            });
        }
    }
}
