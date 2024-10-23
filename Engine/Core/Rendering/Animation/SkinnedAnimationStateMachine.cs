using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Skinned animation state machine
/// </summary>
public sealed class SkinnedAnimationStateMachine : IStapleAsset, IGuidAsset
{
    /// <summary>
    /// Condition for an animation parameter to transition to another state
    /// </summary>
    public enum AnimationCondition
    {
        Equal,
        NotEqual,
        BiggerEqual,
        Bigger,
        Less,
        LessEqual,
    }

    /// <summary>
    /// Type of parameter to validate when verifying a condition to transition to another state
    /// </summary>
    public enum AnimationParameterType
    {
        Bool,
        Int,
        Float,
    }

    /// <summary>
    /// Animation parameter.
    /// </summary>
    [Serializable]
    public class AnimationParameter
    {
        /// <summary>
        /// Parameter name
        /// </summary>
        public string name;

        /// <summary>
        /// Parameter type
        /// </summary>
        public AnimationParameterType parameterType;
    }

    /// <summary>
    /// Animation condition parameter.
    /// 
    /// Contains info to validate a condition to transition into another state.
    /// </summary>
    [Serializable]
    public class AnimationConditionParameter
    {
        /// <summary>
        /// Parameter name
        /// </summary>
        public string name;

        /// <summary>
        /// Condition to trigger
        /// </summary>
        public AnimationCondition condition;

        /// <summary>
        /// Bool value (if parameter is bool)
        /// </summary>
        public bool boolValue;

        /// <summary>
        /// Int value (if parameter is int)
        /// </summary>
        public int intValue;

        /// <summary>
        /// Float value (if parameter is float)
        /// </summary>
        public float floatValue;
    }

    /// <summary>
    /// Animation state connection.
    /// Contains info to be able to transition to another state.
    /// </summary>
    [Serializable]
    public class AnimationStateConnection
    {
        /// <summary>
        /// Target state name
        /// </summary>
        public string name;

        /// <summary>
        /// Whether it triggers from any parameter passing validation
        /// </summary>
        public bool any;

        /// <summary>
        /// Whether it triggers when the animation finishes
        /// </summary>
        public bool onFinish;

        /// <summary>
        /// List of parameters
        /// </summary>
        public List<AnimationConditionParameter> parameters = [];
    }

    /// <summary>
    /// Animation state
    /// </summary>
    [Serializable]
    public class AnimationState
    {
        /// <summary>
        /// State name
        /// </summary>
        public string name;

        /// <summary>
        /// Animation name
        /// </summary>
        public string animation;

        /// <summary>
        /// Whether the animation loops
        /// </summary>
        public bool repeat;

        /// <summary>
        /// Connections to other states
        /// </summary>
        public List<AnimationStateConnection> connections = [];
    }

    /// <summary>
    /// The mesh associated with this state machine
    /// </summary>
    public Mesh mesh;

    /// <summary>
    /// List of states
    /// </summary>
    public List<AnimationState> states = [];

    /// <summary>
    /// List of parameters
    /// </summary>
    public List<AnimationParameter> parameters = [];

    public string Guid { get; set; }

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadAsset<SkinnedAnimationStateMachine>(guid);
    }
}
