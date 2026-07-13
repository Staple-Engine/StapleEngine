namespace Staple.Editor;

[CustomEditor(typeof(SkinnedMeshRenderer))]
internal class SkinnedMeshRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(target is not SkinnedMeshRenderer renderer ||
            renderer.mesh == null ||
            renderer.mesh.meshAsset == null ||
            renderer.mesh.meshAssetIndex < 0 ||
            renderer.mesh.meshAssetIndex >= renderer.mesh.meshAsset.Meshes.Length)
        {
            return;
        }

        var mesh = renderer.mesh.meshAsset.Meshes[renderer.mesh.meshAssetIndex];

        EditorGUI.Label($"{mesh.name}\n{mesh.bones.Length} bones");
    }
}
