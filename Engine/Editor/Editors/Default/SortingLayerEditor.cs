namespace Staple.Editor
{
    [CustomEditor(target = typeof(SortingLayerAttribute))]
    internal class SortingLayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var current = (int)(uint)target;

            var result = EditorGUI.Dropbox("Sorting Layer", LayerMask.AllSortingLayers.ToArray(), current);

            if(result != current)
            {
                target = (uint)result;
            }
        }
    }
}
