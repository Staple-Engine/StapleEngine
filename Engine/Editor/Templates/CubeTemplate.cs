using Staple.Internal;

namespace Staple.Editor;

internal class CubeTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Cube";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(MeshRenderer));

        var renderer = entity.GetComponent<MeshRenderer>();

        renderer.mesh = Mesh.Cube;
        renderer.materials = new([ Resources.Load<Material>("Hidden/Materials/Standard.mat") ]);

        return entity;
    }
}
