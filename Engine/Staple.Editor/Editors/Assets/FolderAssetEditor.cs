using Staple.Internal;

namespace Staple.Editor;

[CustomEditor(typeof(FolderAsset))]
internal class FolderAssetEditor : AssetEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ShowAssetUI(null);
    }
}
