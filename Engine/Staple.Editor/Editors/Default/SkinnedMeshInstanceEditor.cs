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
                    ref var name = ref instance.blendShapeNames[row];

                    if (column == 0)
                    {
                        EditorGUI.Label(name ?? $"Blendshape {row + 1}");

                        return;
                    }

                    ref var weight = ref instance.blendShapeWeights[row];

                    var previousValue = weight;

                    weight = EditorGUI.FloatField("", $"{GetType().FullName}.BlendShapes{row}", previousValue);

                    if (weight != previousValue)
                    {
                        instance.SetBlendShapeWeight(name, weight);
                    }
                },
                null);
        }
    }
}
