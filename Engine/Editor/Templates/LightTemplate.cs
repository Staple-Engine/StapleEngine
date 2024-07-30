namespace Staple.Editor;

internal class LightTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Light";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(Light));

        return entity;
    }
}
