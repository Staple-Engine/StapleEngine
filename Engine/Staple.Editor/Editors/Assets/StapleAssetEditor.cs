namespace Staple.Editor;

[CustomEditor(typeof(IStapleAsset))]
public class StapleAssetEditor : AssetEditor
{
    public override void ApplyChanges()
    {
        StapleEditor.SaveAsset(path, (IStapleAsset)target);
    }

    public void ShowInspectorGUI()
    {
        base.OnInspectorGUI();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ShowAssetUI(null);
    }
}
