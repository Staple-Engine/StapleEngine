namespace Staple.Editor;

internal class PointLightTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Point Light";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(Light));

        var light = entity.GetComponent<Light>();

        light.type = LightType.Point;

        return entity;
    }
}
