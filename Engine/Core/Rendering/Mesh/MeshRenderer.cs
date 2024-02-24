using System.Collections.Generic;

namespace Staple;

public class MeshRenderer : Renderable
{
    public Mesh mesh;
    public List<Material> materials = new();
}
