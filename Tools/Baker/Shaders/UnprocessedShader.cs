using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Baker;

[Serializable]
internal class UnprocessedShader
{
    public ShaderType type;
    public BlendMode sourceBlend;
    public BlendMode destinationBlend;
    public List<ShaderParameter> parameters = [];
    public List<ShaderInstancingParameter> instancingParameters = [];
    public ShaderPiece vertex;
    public ShaderPiece fragment;
    public ShaderPiece compute;
    public List<string> variants = [];
}
