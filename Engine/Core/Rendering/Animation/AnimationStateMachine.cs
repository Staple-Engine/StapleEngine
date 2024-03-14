using System;
using System.Collections.Generic;

namespace Staple;

public class AnimationStateMachine : IStapleAsset, IGuidAsset
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
    public class AnimationConditionParameter
    {
        public string name;
        public AnimationCondition condition;
        public AnimationParameterType parameterType;
        public bool boolValue;
        public int intValue;
        public float floatValue;
    }

    [Serializable]
    public class AnimationState
    {
        public string name;
        public string next;
        public string animation;
        public bool repeat;
        public bool any;
        public List<AnimationConditionParameter> parameters = new();
    }

    public Mesh mesh;

    public List<AnimationState> states = new();

    public string Guid { get; set; }

    public static object Create(string guid)
    {
        return Resources.Load<AnimationStateMachine>(guid);
    }
}
