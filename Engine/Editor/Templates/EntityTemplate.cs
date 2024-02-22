namespace Staple.Editor;

internal class EntityTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Entity";

    public Entity Create()
    {
        return Entity.Create();
    }
}
