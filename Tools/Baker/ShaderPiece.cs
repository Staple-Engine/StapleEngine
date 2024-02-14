using System;
using System.Collections.Generic;

namespace Baker;

[Serializable]
internal class ShaderPiece
{
    public List<string> inputs = new List<string>();
    public List<string> outputs = new List<string>();
    public List<string> code = new List<string>();
}
