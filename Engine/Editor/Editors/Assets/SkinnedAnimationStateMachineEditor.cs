using System.Linq;
using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(SkinnedAnimationStateMachine))]
internal class SkinnedAnimationStateMachineEditor : StapleAssetEditor
{
    public override bool RenderField(FieldInfo field)
    {
        if(target is not SkinnedAnimationStateMachine stateMachine)
        {
            return base.RenderField(field);
        }

        switch(field.Name)
        {
            case nameof(SkinnedAnimationStateMachine.states):

                if(stateMachine.mesh == null ||
                    stateMachine.mesh.meshAsset == null ||
                    stateMachine.mesh.meshAsset.animations.Count == 0)
                {
                    return true;
                }

                var allAnimations = stateMachine.mesh.meshAsset.animations.Select(x => x.Key).ToList();

                EditorGUI.Label(field.Name.ExpandCamelCaseName());

                EditorGUI.TreeNode("Parameters", false, () =>
                {
                    EditorGUI.SameLine();

                    if (EditorGUI.Button("+"))
                    {
                        stateMachine.parameters.Add(new());
                    }

                    EditorGUI.Group(() =>
                    {
                        for (var i = 0; i < stateMachine.parameters.Count; i++)
                        {
                            var parameter = stateMachine.parameters[i];

                            parameter.name = EditorGUI.TextField("Name", parameter.name);

                            EditorGUI.SameLine();

                            if (EditorGUI.Button("-"))
                            {
                                stateMachine.parameters.RemoveAt(i);

                                break;
                            }

                            parameter.parameterType = EditorGUI.EnumDropdown("Type", parameter.parameterType);
                        }
                    });
                });

                EditorGUI.TreeNode("States", false, () =>
                {
                    EditorGUI.SameLine();

                    if (EditorGUI.Button($"+##{stateMachine.GetHashCode()}"))
                    {
                        stateMachine.states.Add(new());
                    }

                    var allAvailableParameters = stateMachine.parameters
                        .Select(x => x.name)
                        .Where(x => x != null)
                        .ToList();

                    EditorGUI.Group(() =>
                    {
                        for (var i = 0; i < stateMachine.states.Count; i++)
                        {
                            var state = stateMachine.states[i];

                            var allAvailableStates = stateMachine.states
                                .Select(x => x.name)
                                .Where(x => x != null && x != state.name)
                                .ToList();

                            state.name = EditorGUI.TextField("Name", state.name);

                            EditorGUI.SameLine();

                            if (EditorGUI.Button($"-"))
                            {
                                stateMachine.states.RemoveAt(i);

                                break;
                            }

                            var currentAnimationIndex = allAnimations.IndexOf(state.animation);

                            var newAnimationIndex = EditorGUI.Dropdown("Animation", allAnimations.ToArray(), currentAnimationIndex);

                            if (newAnimationIndex != currentAnimationIndex && newAnimationIndex >= 0)
                            {
                                state.animation = allAnimations[newAnimationIndex];
                            }

                            state.repeat = EditorGUI.Toggle("Repeat", state.repeat);

                            EditorGUI.TreeNode("Connections", false, () =>
                            {
                                EditorGUI.SameLine();

                                if (EditorGUI.Button("+"))
                                {
                                    state.connections.Add(new());
                                }

                                EditorGUI.Group(() =>
                                {
                                    for (var j = 0; j < state.connections.Count; j++)
                                    {
                                        var connection = state.connections[j];

                                        var currentStateIndex = allAvailableStates.IndexOf(connection.name);

                                        var newStateIndex = EditorGUI.Dropdown("Transition to", allAvailableStates.ToArray(), currentStateIndex);

                                        if (newStateIndex != currentStateIndex && newStateIndex >= 0 && newStateIndex < allAvailableStates.Count)
                                        {
                                            connection.name = allAvailableStates[newStateIndex];
                                        }

                                        connection.any = EditorGUI.Toggle("Trigger on any", connection.any);

                                        connection.onFinish = EditorGUI.Toggle("Trigger on finish", connection.onFinish);

                                        EditorGUI.TreeNode("Conditions", false, () =>
                                        {
                                            EditorGUI.SameLine();

                                            if (EditorGUI.Button("+"))
                                            {
                                                connection.parameters.Add(new());
                                            }

                                            for (var k = 0; k < connection.parameters.Count; k++)
                                            {
                                                var parameter = connection.parameters[k];

                                                var currentNameIndex = allAvailableParameters.IndexOf(parameter.name);

                                                var newNameIndex = EditorGUI.Dropdown("Name", allAvailableParameters.ToArray(), currentNameIndex);

                                                if (newNameIndex != currentNameIndex && newNameIndex >= 0 && newNameIndex < allAvailableParameters.Count)
                                                {
                                                    parameter.name = allAvailableParameters[newNameIndex];
                                                }

                                                parameter.condition = EditorGUI.EnumDropdown("Condition", parameter.condition);

                                                var existingParameter = stateMachine.parameters.FirstOrDefault(x => x.name == parameter.name);

                                                if (existingParameter != null)
                                                {
                                                    switch (existingParameter.parameterType)
                                                    {
                                                        case SkinnedAnimationStateMachine.AnimationParameterType.Bool:

                                                            parameter.boolValue = EditorGUI.Toggle("Value", parameter.boolValue);

                                                            break;

                                                        case SkinnedAnimationStateMachine.AnimationParameterType.Float:

                                                            parameter.floatValue = EditorGUI.FloatField("Value", parameter.floatValue);

                                                            break;

                                                        case SkinnedAnimationStateMachine.AnimationParameterType.Int:

                                                            parameter.intValue = EditorGUI.IntField("Value", parameter.intValue);

                                                            break;
                                                    }
                                                }
                                            }
                                        });
                                    }
                                });
                            });
                        }
                    });
                });

                break;
        }

        return base.RenderField(field);
    }
}
