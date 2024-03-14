using ImGuiNET;
using System.Linq;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(AnimationStateMachine))]
internal class AnimationStateMachineEditor : StapleAssetEditor
{
    public override bool RenderField(FieldInfo field)
    {
        if(target is not AnimationStateMachine stateMachine)
        {
            return base.RenderField(field);
        }

        switch(field.Name)
        {
            case nameof(AnimationStateMachine.states):

                if(stateMachine.mesh == null ||
                    stateMachine.mesh.meshAsset == null ||
                    stateMachine.mesh.meshAsset.animations.Count == 0)
                {
                    return true;
                }

                var allAnimations = stateMachine.mesh.meshAsset.animations.Select(x => x.Key).ToList();

                EditorGUI.Label(field.Name.ExpandCamelCaseName());

                EditorGUI.SameLine();

                if(EditorGUI.Button("+"))
                {
                    stateMachine.states.Add(new());
                }

                ImGui.BeginGroup();

                for(var i = 0; i < stateMachine.states.Count; i++)
                {
                    var state = stateMachine.states[i];

                    state.name = EditorGUI.TextField("Name", state.name);

                    EditorGUI.SameLine();

                    if (EditorGUI.Button("-"))
                    {
                        stateMachine.states.RemoveAt(i);

                        break;
                    }

                    var currentAnimationIndex = allAnimations.IndexOf(state.animation);

                    var newAnimationIndex = EditorGUI.Dropdown("Animation", allAnimations.ToArray(), currentAnimationIndex);

                    if(newAnimationIndex != currentAnimationIndex && newAnimationIndex >= 0)
                    {
                        state.animation = allAnimations[newAnimationIndex];
                    }

                    var allAvailableStates = stateMachine.states
                        .Select(x => x.name)
                        .Where(x => x != null && x != state.name)
                        .ToList();

                    var currentStateIndex = allAvailableStates.IndexOf(state.next);

                    var newStateIndex = EditorGUI.Dropdown("Transition to", allAvailableStates.ToArray(), currentStateIndex);

                    if(newStateIndex != currentStateIndex && newStateIndex >= 0 && newStateIndex < allAvailableStates.Count)
                    {
                        state.next = allAvailableStates[newStateIndex];
                    }

                    state.repeat = EditorGUI.Toggle("Repeat", state.repeat);

                    state.any = EditorGUI.Toggle("Trigger on any", state.any);
                }

                ImGui.EndGroup();

                break;
        }

        return base.RenderField(field);
    }
}
