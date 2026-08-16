namespace Staple.Editor;

[CustomEditor(typeof(SkinnedMeshInstance))]
internal class SkinnedMeshInstanceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (target is not SkinnedMeshInstance instance)
        {
            return;
        }

        if(instance.blendShapeNames != null)
        {
            EditorGUI.HeaderLabel("Blend Shapes");

            EditorGUI.Table($"{GetType().FullName}.BlendShapes", instance.blendShapeNames.Length, 2, false,
                null, null,
                (row, column) =>
                {
                    if (column == 0)
                    {
                        EditorGUI.Label(instance.blendShapeNames[row] ?? $"Blendshape {row + 1}");

                        return;
                    }

                    var previousValue = instance.blendShapeWeights[row];

                    instance.blendShapeWeights[row] = EditorGUI.FloatSlider("", $"{GetType().FullName}.BlendShapes{row}", previousValue, -1, 1);

                    EditorGUI.SameLine();

                    if (instance.blendShapeWeights[row] != previousValue)
                    {
                        instance.SetBlendShapeWeight(instance.blendShapeNames[row], instance.blendShapeWeights[row]);
                    }
                },
                null);
        }
    }
}
