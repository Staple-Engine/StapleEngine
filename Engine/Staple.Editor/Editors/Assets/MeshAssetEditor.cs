using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;

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

                boneCount = meshAsset.BoneCount;
                meshCount = meshAsset.meshes.Count;
                hasExcessiveBones = false;
                triangleCount = 0;

                foreach (var mesh in meshAsset.meshes)
                {
                    triangleCount += mesh.indices.Length;
                }

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

        EditorGUI.Button("Recreate All Materials", "MeshAssetRecreateMaterials", () =>
        {
            try
            {
                var files = Directory.GetFiles(Path.GetDirectoryName(path), $"*.{AssetSerialization.MaterialExtension}*");

                foreach (var file in files)
                {
                    File.Delete(file);
                }

                var meshFiles = new List<string>();
                var cacheFiles = new List<string>();

                foreach (var extension in AssetSerialization.MeshExtensions)
                {
                    meshFiles.AddRange(Directory.GetFiles(Path.GetDirectoryName(path), $"*.{extension}"));
                    cacheFiles.AddRange(Directory.GetFiles(Path.GetDirectoryName(cachePath), $"*.{extension}"));
                }

                foreach (var file in meshFiles)
                {
                    ThumbnailCache.ClearSingle(file);
                }

                foreach (var file in cacheFiles)
                {
                    File.Delete(file);
                }
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

        EditorGUI.Button("Apply settings to All in Folder", "MeshAssetApplyToAll", () =>
        {
            try
            {
                var meshFiles = new List<string>();
                var cacheFiles = new List<string>();

                foreach (var extension in AssetSerialization.MeshExtensions)
                {
                    meshFiles.AddRange(Directory.GetFiles(Path.GetDirectoryName(path), $"*.{extension}.meta"));
                    cacheFiles.AddRange(Directory.GetFiles(Path.GetDirectoryName(cachePath), $"*.{extension}"));
                }

                var newMedata = JsonConvert.DeserializeObject<MeshAssetMetadata>(JsonConvert.SerializeObject(metadata, Formatting.Indented, Tooling.Utilities.JsonSettings));

                foreach (var file in meshFiles)
                {
                    try
                    {
                        var holder = JsonConvert.DeserializeObject<AssetHolder>(File.ReadAllText(file));

                        if(holder?.guid != null)
                        {
                            newMedata.guid = holder.guid;

                            var text = JsonConvert.SerializeObject(newMedata, Formatting.Indented, Tooling.Utilities.JsonSettings);

                            File.WriteAllText(file, text);
                        }
                    }
                    catch(Exception)
                    {
                    }

                    ThumbnailCache.ClearSingle(file);
                }

                foreach (var file in cacheFiles)
                {
                    File.Delete(file);
                }
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

            if(meshCount > 0)
            {
                EditorGUI.Label("Bounds:");
                EditorGUI.Label($"Center: {meshAsset.Bounds.center}");
                EditorGUI.Label($"Size: {meshAsset.Bounds.size}");

                EditorGUI.Label("Used Components:");

                var mesh = meshAsset.meshes[0];

                EditorGUI.Label("Position");

                if(mesh.normals.Length > 0)
                {
                    EditorGUI.Label("Normal");
                }

                if (mesh.tangents.Length > 0)
                {
                    EditorGUI.Label("Tangent");
                }

                if (mesh.bitangents.Length > 0)
                {
                    EditorGUI.Label("Bitangent");
                }

                if (mesh.colors.Length > 0)
                {
                    EditorGUI.Label("Color");
                }

                if (mesh.colors2.Length > 0)
                {
                    EditorGUI.Label("Color2");
                }

                if (mesh.colors3.Length > 0)
                {
                    EditorGUI.Label("Color3");
                }

                if (mesh.colors4.Length > 0)
                {
                    EditorGUI.Label("Color4");
                }

                if (mesh.UV1.Length > 0)
                {
                    EditorGUI.Label("UV1");
                }

                if (mesh.UV2.Length > 0)
                {
                    EditorGUI.Label("UV2");
                }

                if (mesh.UV3.Length > 0)
                {
                    EditorGUI.Label("UV3");
                }

                if (mesh.UV4.Length > 0)
                {
                    EditorGUI.Label("UV4");
                }

                if (mesh.UV5.Length > 0)
                {
                    EditorGUI.Label("UV5");
                }

                if (mesh.UV6.Length > 0)
                {
                    EditorGUI.Label("UV6");
                }

                if (mesh.UV7.Length > 0)
                {
                    EditorGUI.Label("UV7");
                }

                if (mesh.UV8.Length > 0)
                {
                    EditorGUI.Label("UV8");
                }
            }
        }
    }
}
