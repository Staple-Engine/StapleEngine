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
            renderer.mesh.meshAssetIndex >= renderer.mesh.meshAsset.meshes.Count)
        {
            return;
        }

        var mesh = renderer.mesh.meshAsset.meshes[renderer.mesh.meshAssetIndex];

        var boneCount = 0;

        foreach(var b in mesh.bones)
        {
            boneCount += b.Count;
        }

        EditorGUI.Label($"{mesh.name}\n{boneCount} bones");
    }
}
