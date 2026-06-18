namespace Staple.Internal;

public class ShaderUniformInfo
{
    internal ShaderUniform uniform;
    internal int count = 1;
    internal bool isAlias = false;
    internal StringID handle;

    public override string ToString()
    {
        return uniform.name;
    }
}
