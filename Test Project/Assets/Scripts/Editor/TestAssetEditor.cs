using Staple.Editor;

namespace TestGame
{
    [CustomEditor(typeof(TestAsset))]
    public class TestAssetEditor : StapleAssetEditor
    {
        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            EditorGUI.Label("Custom Editor from Game!");
        }
    }
}