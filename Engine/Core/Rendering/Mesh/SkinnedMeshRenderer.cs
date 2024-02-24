using System.Collections.Generic;

namespace Staple;

public class SkinnedMeshRenderer : Renderable
{
    public Mesh mesh;
    public List<Material> materials = new();
}
