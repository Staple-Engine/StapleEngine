using System.Collections.Generic;
using System.Linq;

namespace Staple;

/// <summary>
/// State machine controller for skinned mesh animators
/// </summary>
public sealed class SkinnedAnimationController
{
    /// <summary>
    /// Animation parameter
    /// </summary>
    internal class Parameter
    {
        /// <summary>
        /// The parameter type
        /// </summary>
        public SkinnedAnimationStateMachine.AnimationParameterType type;

        /// <summary>
        /// The bool value if the type is bool
        /// </summary>
        public bool boolValue;

        /// <summary>
        /// The int value if the type is int
        /// </summary>
        public int intValue;

        /// <summary>
        /// The float value if the type is float
        /// </summary>
        public float floatValue;
    }

    /// <summary>
    /// The animator we're using
    /// </summary>
    internal SkinnedMeshAnimator animator;

    /// <summary>
    /// The state machine we're using
    /// </summary>
    internal SkinnedAnimationStateMachine stateMachine;

    /// <summary>
    /// The current state in the state machine
    /// </summary>
    internal SkinnedAnimationStateMachine.AnimationState currentState;

    /// <summary>
    /// List of all parameters
    /// </summary>
    private readonly Dictionary<string, Parameter> parameters = [];

    public SkinnedAnimationController(SkinnedMeshAnimator animator)
    {
        this.animator = animator;

        stateMachine = animator?.stateMachine;

        if(stateMachine == null)
        {
            return;
        }

        var startState = stateMachine.states.FirstOrDefault()?.name;

        if(startState != null)
        {
            SetState(startState);
        }

        foreach (var parameter in stateMachine.parameters)
        {
            if((parameter.name?.Length ?? 0) == 0)
            {
                continue;
            }

            parameters.AddOrSetKey(parameter.name, new()
            {
                type = parameter.parameterType,
            });
        }
    }

    /// <summary>
    /// Sets a bool value for a parameter
    /// </summary>
    /// <param name="name">The parameter name</param>
    /// <param name="value">The new value</param>
    public void SetBoolParameter(string name, bool value)
    {
        if(parameters.TryGetValue(name, out var parameter) &&
            parameter.type == SkinnedAnimationStateMachine.AnimationParameterType.Bool)
        {
            parameter.boolValue = value;
        }

        CheckConditions();
    }

    /// <summary>
    /// Sets an int value for a parameter
    /// </summary>
    /// <param name="name">The parameter name</param>
    /// <param name="value">The new value</param>
    public void SetIntParameter(string name, int value)
    {
        if (parameters.TryGetValue(name, out var parameter) &&
            parameter.type == SkinnedAnimationStateMachine.AnimationParameterType.Int)
        {
            parameter.intValue = value;
        }

        CheckConditions();
    }

    /// <summary>
    /// Sets a float value for a parameter
    /// </summary>
    /// <param name="name">The parameter name</param>
    /// <param name="value">The new value</param>
    public void SetFloatParameter(string name, float value)
    {
        if (parameters.TryGetValue(name, out var parameter) &&
            parameter.type == SkinnedAnimationStateMachine.AnimationParameterType.Float)
        {
            parameter.floatValue = value;
        }

        CheckConditions();
    }

    /// <summary>
    /// Checks if the animation finished
    /// </summary>
    /// <returns>Whether the animation finished</returns>
    private bool AnimationFinished()
    {
        if(currentState == null ||
            animator.animation != currentState.animation ||
            animator.evaluator == null)
        {
            return false;
        }

        return animator.evaluator.FinishedPlaying;
    }

    /// <summary>
    /// Checks whether a parameter passes its condition
    /// </summary>
    /// <param name="parameter">The parameter to check</param>
    /// <returns>Whether it passes</returns>
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

    /// <summary>
    /// Checks the current conditions to trigger a state change
    /// </summary>
    private void CheckConditions()
    {
        if(currentState == null || currentState.connections.Count == 0)
        {
            return;
        }

        foreach(var connection in currentState.connections)
        {
            var shouldTrigger = false;

            if(connection.onFinish)
            {
                shouldTrigger = AnimationFinished();
            }
            else if (connection.parameters.Count > 0)
            {
                if (connection.any)
                {
                    foreach (var parameter in connection.parameters)
                    {
                        if (CheckParameter(parameter))
                        {
                            shouldTrigger = true;

                            break;
                        }
                    }
                }
                else
                {
                    shouldTrigger = true;

                    foreach (var parameter in connection.parameters)
                    {
                        if (CheckParameter(parameter) == false)
                        {
                            shouldTrigger = false;

                            break;
                        }
                    }
                }
            }

            if (shouldTrigger)
            {
                SetState(connection.name);

                break;
            }
        }
    }

    /// <summary>
    /// Sets a new state
    /// </summary>
    /// <param name="name">The state name</param>
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

        animator.SetAnimation(state.animation, state.repeat);
    }
}
