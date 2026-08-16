using System;
using System.Collections.Generic;

namespace Staple.Editor;

[CustomEditor(typeof(SkinnedMeshRenderer))]
internal class SkinnedMeshRendererEditor : Editor
{
    public override bool DrawProperty(Type type, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        if(target is not SkinnedMeshRenderer renderer)
        {
            return false;
        }

        if(name == nameof(SkinnedMeshRenderer.blendShapeWeights))
        {
            if(getter() is List<float> blendShapeWeights && renderer.mesh?.MeshAssetMesh?.blendShape is MeshAsset.BlendShape blendShape)
            {
                EditorGUI.HeaderLabel("Blend Shapes");

                EditorGUI.Table($"{GetType().FullName}.{name}", blendShapeWeights.Count, 2, false,
                    null, null,
                    (row, column) =>
                    {
                        ref var channel = ref blendShape.channels[row];

                        if (column == 0)
                        {
                            EditorGUI.Label(channel.name ?? $"Blendshape {row + 1}");

                            return;
                        }

                        var newValue = blendShapeWeights[row];

                        blendShapeWeights[row] = newValue = EditorGUI.FloatSlider("",
                            $"{GetType().FullName}.{name}{row}", newValue, -1, 1);

                        EditorGUI.SameLine();

                        var originalValue = blendShape.channels[row].weight;

                        if (newValue != originalValue)
                        {
                            renderer.instance?.Content?.SetBlendShapeWeight(blendShape.channels[row].name, newValue);

                            EditorGUI.Button("R", $"{GetType().FullName}.{name}{row}.Revert",
                                () =>
                                {
                                    blendShapeWeights[row] = originalValue;

                                    renderer.instance?.Content?.SetBlendShapeWeight(blendShape.channels[row].name, originalValue);
                                });
                        }
                        else
                        {
                            EditorGUI.ButtonDisabled("R", $"{GetType().FullName}.{name}{row}.Revert", null);
                        }
                    },
                    null);
            }

            return true;
        }

        return false;
    }

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
