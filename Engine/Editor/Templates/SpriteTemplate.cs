namespace Staple.Editor;

internal class SpriteTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Sprite";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(SpriteRenderer));

        var renderer = entity.GetComponent<SpriteRenderer>();

        renderer.texture = Resources.Load<Texture>("Hidden/Textures/Sprites/DefaultSprite.png");
        renderer.material = Resources.Load<Material>("Hidden/Materials/Sprite.mat");

        return entity;
    }
}
