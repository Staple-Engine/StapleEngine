namespace Staple.Editor.Templates;

public class SphereTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Sphere";

    public Entity Create()
    {
        var entity = Entity.CreatePrimitive(EntityPrimitiveType.Sphere);

        entity.Name = Name;

        return entity;
    }
}
