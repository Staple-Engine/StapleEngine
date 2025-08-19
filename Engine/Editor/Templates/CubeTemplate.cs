namespace Staple.Editor.Templates;

public class CubeTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Cube";

    public Entity Create()
    {
        var entity = Entity.CreatePrimitive(EntityPrimitiveType.Cube);

        entity.Name = Name;

        return entity;
    }
}
