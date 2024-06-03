using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(MeshAssetMetadata))]
internal class MeshAssetEditor : Editor
{
    private MeshAsset meshAsset;
    private bool needsLoad = true;

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

        var hasChanges = metadata != originalMetadata;

        if (hasChanges)
        {
            EditorGUI.Button("Apply", () =>
            {
                try
                {
                    var text = JsonConvert.SerializeObject(metadata, Formatting.Indented, new JsonSerializerSettings()
                    {
                        Converters =
                        {
                            new StringEnumConverter(),
                        }
                    });

                    File.WriteAllText(path, text);
                }
                catch (Exception)
                {
                }

                var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(original, field.GetValue(metadata));
                }

                EditorUtils.RefreshAssets(false, () =>
                {
                    meshAsset = null;
                    needsLoad = true;
                });
            });

            EditorGUI.SameLine();

            EditorGUI.Button("Revert", () =>
            {
                var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(metadata, field.GetValue(original));
                }
            });
        }
        else
        {
            EditorGUI.ButtonDisabled("Apply", null);

            EditorGUI.SameLine();

            EditorGUI.ButtonDisabled("Revert", null);
        }

        EditorGUI.SameLine();

        EditorGUI.Button("Recreate Materials", () =>
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
            var hasExcessiveBones = meshAsset.meshes.Any(x => x.bones.Any(x => x.Count > 128));

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
