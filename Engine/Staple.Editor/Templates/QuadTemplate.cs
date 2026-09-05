namespace Staple.Editor.Templates;

public class QuadTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Quad";

    public Entity Create()
    {
        return Entity.CreatePrimitive(Name, EntityPrimitiveType.Quad);
    }
}
