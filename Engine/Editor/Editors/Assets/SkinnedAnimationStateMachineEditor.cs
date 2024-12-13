using System;
using System.Linq;

namespace Staple.Editor;

[CustomEditor(typeof(SkinnedAnimationStateMachine))]
internal class SkinnedAnimationStateMachineEditor : StapleAssetEditor
{
    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        if (target is not SkinnedAnimationStateMachine stateMachine)
        {
            return false;
        }

        switch(name)
        {
            case nameof(SkinnedAnimationStateMachine.parameters):

                if (stateMachine.mesh == null ||
                    stateMachine.mesh.meshAsset == null ||
                    stateMachine.mesh.meshAsset.animations.Count == 0)
                {
                    return true;
                }

                EditorGUI.TreeNode("Parameters", "SkinnedAnimationParameters", false, () =>
                {
                    EditorGUI.SameLine();

                    EditorGUI.Button("+", "SkinnedAnimationParametersAdd", () =>
                    {
                        stateMachine.parameters.Add(new());
                    });

                    EditorGUI.Group(() =>
                    {
                        for (var i = 0; i < stateMachine.parameters.Count; i++)
                        {
                            var parameter = stateMachine.parameters[i];

                            parameter.name = EditorGUI.TextField("Name", $"SkinnedAnimationParameters{i}Name", parameter.name);

                            EditorGUI.SameLine();

                            EditorGUI.Button("-", $"SkinnedAnimationParameters{i}Remove", () =>
                            {
                                stateMachine.parameters.RemoveAt(i);
                            });

                            parameter.parameterType = EditorGUI.EnumDropdown("Type", $"SkinnedAnimationParameters{i}Type", parameter.parameterType);
                        }
                    });
                }, null);

                return true;

            case nameof(SkinnedAnimationStateMachine.states):

                if(stateMachine.mesh == null ||
                    stateMachine.mesh.meshAsset == null ||
                    stateMachine.mesh.meshAsset.animations.Count == 0)
                {
                    return true;
                }

                var allAnimations = stateMachine.mesh.meshAsset.animations.Select(x => x.Key).ToList();

                EditorGUI.TreeNode("States", $"SkinnedAnimationStates", false, () =>
                {
                    EditorGUI.SameLine();

                    EditorGUI.Button("+", $"SkinnedAnimationStatesAdd", () =>
                    {
                        stateMachine.states.Add(new());
                    });

                    var allAvailableParameters = stateMachine.parameters
                        .Select(x => x.name)
                        .Where(x => x != null)
                        .ToList();

                    EditorGUI.Group(() =>
                    {
                        var skip = false;

                        for (var i = 0; i < stateMachine.states.Count; i++)
                        {
                            var state = stateMachine.states[i];

                            var allAvailableStates = stateMachine.states
                                .Select(x => x.name)
                                .Where(x => x != null && x != state.name)
                                .ToList();

                            state.name = EditorGUI.TextField("Name", $"SkinnedAnimationStates{i}Name", state.name);

                            EditorGUI.SameLine();

                            EditorGUI.Button("-", $"SkinnedAnimationStates{i}Remove", () =>
                            {
                                stateMachine.states.RemoveAt(i);

                                skip = true;
                            });

                            if(skip)
                            {
                                break;
                            }

                            var currentAnimationIndex = allAnimations.IndexOf(state.animation);

                            var newAnimationIndex = EditorGUI.Dropdown("Animation", $"SkinnedAnimationStates{i}Animation",
                                allAnimations.ToArray(), currentAnimationIndex);

                            if (newAnimationIndex != currentAnimationIndex && newAnimationIndex >= 0)
                            {
                                state.animation = allAnimations[newAnimationIndex];
                            }

                            state.repeat = EditorGUI.Toggle("Repeat", $"SkinnedAnimationStates{i}Repeat", state.repeat);

                            EditorGUI.TreeNode("Connections", $"SkinnedAnimationStates{i}Connections", false, () =>
                            {
                                EditorGUI.SameLine();

                                EditorGUI.Button("+", $"SkinnedAnimationStates{i}ConnectionsAdd", () =>
                                {
                                    state.connections.Add(new());
                                });

                                EditorGUI.Group(() =>
                                {
                                    for (var j = 0; j < state.connections.Count; j++)
                                    {
                                        var connection = state.connections[j];

                                        var currentStateIndex = allAvailableStates.IndexOf(connection.name);

                                        var newStateIndex = EditorGUI.Dropdown("Transition to", $"SkinnedAnimationStates{i}Transition{j}",
                                            allAvailableStates.ToArray(), currentStateIndex);

                                        if (newStateIndex != currentStateIndex && newStateIndex >= 0 && newStateIndex < allAvailableStates.Count)
                                        {
                                            connection.name = allAvailableStates[newStateIndex];
                                        }

                                        EditorGUI.SameLine();

                                        EditorGUI.Button("-", $"SkinnedAnimationStates{i}Transition{j}Remove", () =>
                                        {
                                            state.connections.RemoveAt(j);

                                            skip = true;
                                        });

                                        if (skip)
                                        {
                                            break;
                                        }

                                        connection.any = EditorGUI.Toggle("Trigger on any", $"SkinnedAnimationStates{i}Any{j}", connection.any);

                                        connection.onFinish = EditorGUI.Toggle("Trigger on finish", $"SkinnedAnimationStates{i}Trigger{j}", connection.onFinish);

                                        EditorGUI.TreeNode("Conditions", $"SkinnedAnimationStates{i}Conditions{j}", false, () =>
                                        {
                                            EditorGUI.SameLine();

                                            EditorGUI.Button("+", $"SkinnedAnimationStates{i}Conditions{j}Add", () =>
                                            {
                                                connection.parameters.Add(new());
                                            });

                                            for (var k = 0; k < connection.parameters.Count; k++)
                                            {
                                                var parameter = connection.parameters[k];

                                                var currentNameIndex = allAvailableParameters.IndexOf(parameter.name);

                                                var newNameIndex = EditorGUI.Dropdown("Name", $"SkinnedAnimationStates{i}Conditions{j}Name{k}",
                                                    allAvailableParameters.ToArray(), currentNameIndex);

                                                if (newNameIndex != currentNameIndex && newNameIndex >= 0 && newNameIndex < allAvailableParameters.Count)
                                                {
                                                    parameter.name = allAvailableParameters[newNameIndex];
                                                }

                                                EditorGUI.SameLine();

                                                EditorGUI.Button("-", $"SkinnedAnimationStates{i}Conditions{j}Remove{k}", () =>
                                                {
                                                    connection.parameters.RemoveAt(k);

                                                    skip = true;
                                                });

                                                if (skip)
                                                {
                                                    break;
                                                }

                                                parameter.condition = EditorGUI.EnumDropdown("Condition", $"SkinnedAnimationStates{i}Conditions{j}Condition{k}",
                                                    parameter.condition);

                                                var existingParameter = stateMachine.parameters.FirstOrDefault(x => x.name == parameter.name);

                                                if (existingParameter != null)
                                                {
                                                    switch (existingParameter.parameterType)
                                                    {
                                                        case SkinnedAnimationStateMachine.AnimationParameterType.Bool:

                                                            parameter.boolValue = EditorGUI.Toggle("Value", $"SkinnedAnimationStates{i}Conditions{j}Value{k}",
                                                                parameter.boolValue);

                                                            break;

                                                        case SkinnedAnimationStateMachine.AnimationParameterType.Float:

                                                            parameter.floatValue = EditorGUI.FloatField("Value", $"SkinnedAnimationStates{i}Conditions{j}Value{k}",
                                                                parameter.floatValue);

                                                            break;

                                                        case SkinnedAnimationStateMachine.AnimationParameterType.Int:

                                                            parameter.intValue = EditorGUI.IntField("Value", $"SkinnedAnimationStates{i}Conditions{j}Value{k}",
                                                                parameter.intValue);

                                                            break;
                                                    }
                                                }
                                            }
                                        }, null);
                                    }
                                });
                            }, null);
                        }
                    });
                }, null);

                return true;
        }

        return false;
    }
}
