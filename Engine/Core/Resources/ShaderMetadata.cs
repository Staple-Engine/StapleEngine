using MessagePack;
using System.Collections.Generic;

namespace Staple.Internal
{
    public enum ShaderType
    {
        VertexFragment,
        Compute
    }

    public enum ShaderUniformType
    {
        Vector4,
        Color,
        Texture,
        Matrix3x3,
        Matrix4x4
    }

    [MessagePackObject]
    public class ShaderUniform
    {
        [Key(0)]
        public string name;

        [Key(1)]
        public ShaderUniformType type;
    }

    [MessagePackObject]
    public class ShaderMetadata
    {
        [Key(0)]
        public ShaderType type = ShaderType.VertexFragment;

        [Key(1)]
        public List<ShaderUniform> uniforms = new List<ShaderUniform>();
    }
}
