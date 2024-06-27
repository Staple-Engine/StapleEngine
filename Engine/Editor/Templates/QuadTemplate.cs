namespace Staple.Editor;

internal class QuadTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Quad";

    public Entity Create()
    {
        var entity = Entity.CreatePrimitive(EntityPrimitiveType.Quad);

        entity.Name = Name;

        return entity;
    }
}
