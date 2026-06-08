namespace Staple.Editor.Templates;

public class QuadTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Quad";

    public Entity Create()
    {
        var entity = Entity.CreatePrimitive(EntityPrimitiveType.Quad);

        entity.Name = Name;

        return entity;
    }
}
