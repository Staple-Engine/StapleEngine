using System;
using System.Linq;

namespace Staple.Editor;

[CustomEditor(typeof(SkinnedAnimationStateMachine))]
internal class SkinnedAnimationStateMachineEditor : StapleAssetEditor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.Button("Open Editor", "SkinnedAnimationStateMachineEditor.Open", () =>
        {
            var window = EditorWindow.GetWindow<SkinnedAnimationStateMachineWindow>();

            window.owner = this;
        });
    }
}
