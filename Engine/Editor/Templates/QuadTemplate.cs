﻿using Staple.Internal;

namespace Staple.Editor;

internal class QuadTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Quad";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(MeshRenderer));

        var renderer = entity.GetComponent<MeshRenderer>();

        renderer.mesh = Mesh.Quad;
        renderer.material = Resources.Load<Material>("Hidden/Materials/Standard.mat");

        return entity;
    }
}