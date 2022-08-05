using MessagePack;

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
