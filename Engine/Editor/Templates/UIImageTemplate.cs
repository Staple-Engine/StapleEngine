using Staple.UI;

namespace Staple.Editor;

internal class UIImageTemplate : IEntityTemplate
{
    public string Name { get; set; } = "UI Image";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(UIImage));

        var image = entity.GetComponent<UIImage>();

        var texture = Resources.Load<Texture>("Hidden/Textures/Sprites/DefaultSprite.png");

        image.sprite = texture != null && texture.Sprites.Length > 0 ? texture.Sprites[0] : null;
        image.size = new Vector2Int(50, 50);

        return entity;
    }
}
