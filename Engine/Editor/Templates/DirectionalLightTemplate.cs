﻿namespace Staple.Editor.Templates;

public class DirectionalLightTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Directional Light";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(Light));

        return entity;
    }
}
