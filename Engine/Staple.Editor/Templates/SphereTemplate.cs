namespace Staple.Editor.Templates;

public class SphereTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Sphere";

    public Entity Create()
    {
        return Entity.CreatePrimitive(Name, EntityPrimitiveType.Sphere);
    }
}
