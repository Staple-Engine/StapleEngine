namespace Staple.Editor;

[CustomEditor(typeof(VisualShaderAsset))]
internal class VisualShaderAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.Button("Open Editor", "VisualShaderAssetEditor.Open", () =>
        {
            EditorWindow.GetWindow<VisualShaderAssetWindow>().owner = this;
        });
    }
}
