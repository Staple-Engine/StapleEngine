
using System.Runtime.InteropServices;

namespace Staple.Internal;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
internal struct StapleShaderUniform
{
    public byte binding;
    public int position;
    public int size;
}
