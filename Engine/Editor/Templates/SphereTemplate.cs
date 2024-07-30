namespace Staple.Editor;

internal class SphereTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Sphere";

    public Entity Create()
    {
        var entity = Entity.CreatePrimitive(EntityPrimitiveType.Sphere);

        entity.Name = Name;

        return entity;
    }
}
