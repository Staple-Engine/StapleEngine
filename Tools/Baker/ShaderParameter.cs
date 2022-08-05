using System;

namespace Baker
{
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
