namespace Staple.Editor.Templates;

public class EntityTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Entity";

    public Entity Create()
    {
        return Entity.Create(typeof(Transform));
    }
}
