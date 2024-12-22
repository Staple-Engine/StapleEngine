using Staple.Internal;
using System;

namespace Baker;

[Serializable]
internal class ShaderParameter
{
    public string name;
    public ShaderParameterSemantic semantic;
    public ShaderUniformType type;
    public string attribute;
    public string defaultValue;
}
