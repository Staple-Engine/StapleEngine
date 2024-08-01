using Staple.Internal;
using System;
using System.IO;
using System.Linq;

namespace Staple.Editor;

[CustomEditor(typeof(MeshAssetMetadata))]
internal class MeshAssetEditor : AssetEditor
{
    private MeshAsset meshAsset;
    private bool needsLoad = true;

    public override bool DrawProperty(Type type, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var t = target as MeshAssetMetadata;

        if(name == nameof(MeshAssetMetadata.useSmoothNormals) && t.regenerateNormals == false)
        {
            return true;
        }

        return false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var metadata = (MeshAssetMetadata)target;
        var originalMetadata = (MeshAssetMetadata)original;

        if(needsLoad)
        {
            needsLoad = false;

            meshAsset = ResourceManager.instance.LoadMeshAsset(metadata.guid, true);
        }

        ShowAssetUI(() =>
        {
            meshAsset = null;
            needsLoad = true;
        });

        EditorGUI.SameLine();

        EditorGUI.Button("Recreate Materials", "MeshAssetRecreateMaterials", () =>
        {
            try
            {
                var files = Directory.GetFiles(Path.GetDirectoryName(path), "*.mat*");

                foreach (var file in files)
                {
                    File.Delete(file);
                }

                File.Delete(cachePath);
            }
            catch (Exception)
            {
            }

            EditorUtils.RefreshAssets(false, () =>
            {
                meshAsset = null;
                needsLoad = true;
            });
        });

        if (meshAsset != null)
        {
            var hasExcessiveBones = meshAsset.meshes.Any(x => x.bones.Any(x => x.Count > SkinnedMeshRenderSystem.MaxBones));

            EditorGUI.Label($"{meshAsset.meshes.Count} meshes.");

            if(hasExcessiveBones)
            {
                EditorGUI.Label("There are one or more meshes with excessive bone count. " +
                    "Please change import settings to reduce bones or split meshes.");
            }

            if (meshAsset.meshes.Any(x => x.bones.Count > 0) && path.Contains(".fbx"))
            {
                EditorGUI.Label("Skinned FBX models currently import incorrectly,\nplease convert to another format such as gltf/glb");
            }
        }
    }
}
