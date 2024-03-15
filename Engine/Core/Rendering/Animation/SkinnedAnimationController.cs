using System.Collections.Generic;
using System.Linq;

namespace Staple;

public class SkinnedAnimationController
{
    internal class Parameter
    {
        public SkinnedAnimationStateMachine.AnimationParameterType type;
        public bool boolValue;
        public int intValue;
        public float floatValue;
    }

    internal SkinnedMeshAnimator animator;
    internal SkinnedAnimationStateMachine stateMachine;
    internal SkinnedAnimationStateMachine.AnimationState currentState;

    private Dictionary<string, Parameter> parameters = new();

    public SkinnedAnimationController(SkinnedMeshAnimator animator, SkinnedAnimationStateMachine stateMachine)
    {
        this.animator = animator;
        this.stateMachine = stateMachine;

        var startState = this.stateMachine.states.FirstOrDefault()?.name;

        if(startState != null)
        {
            SetState(startState);
        }

        foreach (var parameter in stateMachine.parameters)
        {
            if(parameter.name == null || parameter.name.Length == 0)
            {
                continue;
            }

            parameters.AddOrSetKey(parameter.name, new()
            {
                type = parameter.parameterType,
            });
        }
    }

    public void SetBoolParameter(string name, bool value)
    {
        if(parameters.TryGetValue(name, out var parameter) &&
            parameter.type == SkinnedAnimationStateMachine.AnimationParameterType.Bool)
        {
            parameter.boolValue = value;
        }

        CheckConditions();
    }

    public void SetIntParameter(string name, int value)
    {
        if (parameters.TryGetValue(name, out var parameter) &&
            parameter.type == SkinnedAnimationStateMachine.AnimationParameterType.Int)
        {
            parameter.intValue = value;
        }

        CheckConditions();
    }

    public void SetFloatParameter(string name, float value)
    {
        if (parameters.TryGetValue(name, out var parameter) &&
            parameter.type == SkinnedAnimationStateMachine.AnimationParameterType.Float)
        {
            parameter.floatValue = value;
        }

        CheckConditions();
    }

    private bool CheckParameter(SkinnedAnimationStateMachine.AnimationConditionParameter parameter)
    {
        if (parameter.name == null || parameter.name.Length == 0)
        {
            return false;
        }

        if (parameters.TryGetValue(parameter.name, out var localParameter))
        {
            var valid = false;

            switch (parameter.condition)
            {
                case SkinnedAnimationStateMachine.AnimationCondition.Equal:

                    switch (localParameter.type)
                    {
                        case SkinnedAnimationStateMachine.AnimationParameterType.Bool:

                            valid = localParameter.boolValue == parameter.boolValue;

                            break;

                        case SkinnedAnimationStateMachine.AnimationParameterType.Float:

                            valid = localParameter.floatValue == parameter.floatValue;

                            break;

                        case SkinnedAnimationStateMachine.AnimationParameterType.Int:

                            valid = localParameter.intValue == parameter.intValue;

                            break;
                    }

                    break;

                case SkinnedAnimationStateMachine.AnimationCondition.NotEqual:

                    switch (localParameter.type)
                    {
                        case SkinnedAnimationStateMachine.AnimationParameterType.Bool:

                            valid = localParameter.boolValue != parameter.boolValue;

                            break;

                        case SkinnedAnimationStateMachine.AnimationParameterType.Float:

                            valid = localParameter.floatValue != parameter.floatValue;

                            break;

                        case SkinnedAnimationStateMachine.AnimationParameterType.Int:

                            valid = localParameter.intValue != parameter.intValue;

                            break;
                    }

                    break;

                case SkinnedAnimationStateMachine.AnimationCondition.Bigger:

                    switch (localParameter.type)
                    {
                        case SkinnedAnimationStateMachine.AnimationParameterType.Float:

                            valid = localParameter.floatValue > parameter.floatValue;

                            break;

                        case SkinnedAnimationStateMachine.AnimationParameterType.Int:

                            valid = localParameter.intValue > parameter.intValue;

                            break;
                    }

                    break;

                case SkinnedAnimationStateMachine.AnimationCondition.BiggerEqual:

                    switch (localParameter.type)
                    {
                        case SkinnedAnimationStateMachine.AnimationParameterType.Float:

                            valid = localParameter.floatValue >= parameter.floatValue;

                            break;

                        case SkinnedAnimationStateMachine.AnimationParameterType.Int:

                            valid = localParameter.intValue >= parameter.intValue;

                            break;
                    }

                    break;

                case SkinnedAnimationStateMachine.AnimationCondition.Less:

                    switch (localParameter.type)
                    {
                        case SkinnedAnimationStateMachine.AnimationParameterType.Float:

                            valid = localParameter.floatValue < parameter.floatValue;

                            break;

                        case SkinnedAnimationStateMachine.AnimationParameterType.Int:

                            valid = localParameter.intValue < parameter.intValue;

                            break;
                    }

                    break;

                case SkinnedAnimationStateMachine.AnimationCondition.LessEqual:

                    switch (localParameter.type)
                    {
                        case SkinnedAnimationStateMachine.AnimationParameterType.Float:

                            valid = localParameter.floatValue <= parameter.floatValue;

                            break;

                        case SkinnedAnimationStateMachine.AnimationParameterType.Int:

                            valid = localParameter.intValue <= parameter.intValue;

                            break;
                    }

                    break;
            }

            return valid;
        }

        return false;
    }

    private void CheckConditions()
    {
        if(currentState == null || currentState.connections.Count == 0)
        {
            return;
        }

        foreach(var connection in currentState.connections)
        {
            var shouldTrigger = false;

            if(connection.any)
            {
                foreach(var parameter in connection.parameters)
                {
                    if(CheckParameter(parameter))
                    {
                        shouldTrigger = true;

                        break;
                    }
                }
            }
            else
            {
                shouldTrigger = true;

                foreach(var parameter in connection.parameters)
                {
                    if(CheckParameter(parameter) == false)
                    {
                        shouldTrigger = false;

                        break;
                    }
                }
            }

            if(shouldTrigger)
            {
                SetState(connection.name);

                break;
            }
        }
    }

    private void SetState(string name)
    {
        if (name == null)
        {
            return;
        }

        var state = animator.stateMachine.states.FirstOrDefault(x => x.name == name);

        if (state == null)
        {
            return;
        }

        currentState = state;

        animator.repeat = state.repeat;
        animator.animation = state.animation;
        animator.playTime = 0;
        animator.evaluator = null;
    }
}
