namespace Staple.Editor.Templates;

public class SpriteTemplate : IEntityTemplate
{
    public string Name { get; set; } = "Sprite";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(SpriteRenderer));

        var renderer = entity.GetComponent<SpriteRenderer>();

        var texture = Resources.Load<Texture>("Hidden/Textures/Sprites/DefaultSprite.png");

        renderer.sprite = texture != null && texture.Sprites.Length > 0 ? texture.Sprites[0] : null;

        renderer.material = SpriteUtils.DefaultMaterial.Value;

        return entity;
    }
}
