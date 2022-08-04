using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    public enum ShaderType
    {
        VertexFragment,
        Compute
    }

    [MessagePackObject]
    public class ShaderMetadata
    {
        [Key(0)]
        public ShaderType type = ShaderType.VertexFragment;
    }
}
