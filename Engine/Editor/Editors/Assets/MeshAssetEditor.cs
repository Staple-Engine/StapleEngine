using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Editor;

[CustomEditor(typeof(MeshAssetMetadata))]
internal class MeshAssetEditor : AssetEditor
{
    private MeshAsset meshAsset;
    private int meshCount = 0;
    private int boneCount = 0;
    private int triangleCount = 0;
    private bool hasExcessiveBones = false;
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

            if(meshAsset != null)
            {
                var boneSet = new HashSet<string>();

                meshCount = meshAsset.meshes.Count;
                hasExcessiveBones = false;
                triangleCount = 0;

                foreach (var mesh in meshAsset.meshes)
                {
                    triangleCount += mesh.indices.Length;

                    foreach (var submesh in mesh.bones)
                    {
                        hasExcessiveBones |= submesh.Length > SkinnedMeshRenderSystem.MaxBones;

                        foreach (var bone in submesh)
                        {
                            boneSet.Add(bone.name);
                        }
                    }
                }

                boneCount = boneSet.Count;
                triangleCount /= 3;
            }
            else
            {
                boneCount = 0;
                meshCount = 0;
                triangleCount = 0;
                hasExcessiveBones = false;
            }
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
            EditorGUI.Label($"Stats:\n{meshCount} meshes\n{triangleCount} triangles\n{boneCount} bones\n{meshAsset.animations.Count} animations\n\n");

            if(hasExcessiveBones)
            {
                EditorGUI.Label("Warning: There are one or more meshes with excessive bone count. " +
                    "Please change import settings to reduce bones or split meshes.");
            }

            if (boneCount > 0 && path.Contains(".fbx"))
            {
                EditorGUI.Label("Warning: Skinned FBX models currently import incorrectly,\nplease convert to another format such as gltf/glb");
            }
        }
    }
}
