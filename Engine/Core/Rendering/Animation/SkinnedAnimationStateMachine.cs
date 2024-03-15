using System;
using System.Collections.Generic;

namespace Staple;

public class SkinnedAnimationStateMachine : IStapleAsset, IGuidAsset
{
    public enum AnimationCondition
    {
        Equal,
        NotEqual,
        BiggerEqual,
        Bigger,
        Less,
        LessEqual,
    }

    public enum AnimationParameterType
    {
        Bool,
        Int,
        Float,
    }

    [Serializable]
    public class AnimationParameter
    {
        public string name;
        public AnimationParameterType parameterType;
    }

    [Serializable]
    public class AnimationConditionParameter
    {
        public string name;
        public AnimationCondition condition;
        public bool boolValue;
        public int intValue;
        public float floatValue;
    }

    [Serializable]
    public class AnimationStateConnection
    {
        public string name;
        public bool any;
        public bool onFinish;
        public List<AnimationConditionParameter> parameters = new();
    }

    [Serializable]
    public class AnimationState
    {
        public string name;
        public string animation;
        public bool repeat;

        public List<AnimationStateConnection> connections = new();
    }

    public Mesh mesh;

    public List<AnimationState> states = new();

    public List<AnimationParameter> parameters = new();

    public string Guid { get; set; }

    public static object Create(string guid)
    {
        return Resources.Load<SkinnedAnimationStateMachine>(guid);
    }
}
