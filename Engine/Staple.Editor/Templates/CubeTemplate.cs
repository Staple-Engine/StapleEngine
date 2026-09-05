namespace Staple.Editor.Templates;

public class CubeTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Cube";

    public Entity Create()
    {
        return Entity.CreatePrimitive(Name, EntityPrimitiveType.Cube);
    }
}
