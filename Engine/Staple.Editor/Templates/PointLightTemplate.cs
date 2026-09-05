namespace Staple.Editor.Templates;

public class PointLightTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Point Light";

    public Entity Create()
    {
        var entity = Entity.Create(Name, out Transform _, out Light light);

        light.type = LightType.Point;

        return entity;
    }
}
