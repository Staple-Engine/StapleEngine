using System;

namespace Baker
{
    enum ShaderParameterType
    {
        Color,
        vec4,
        Texture,
    }

    [Serializable]
    internal class ShaderParameter
    {
        public string name;
        public ShaderParameterSemantic semantic;
        public string type;
        public string attribute;
        public string defaultValue;
    }
}
