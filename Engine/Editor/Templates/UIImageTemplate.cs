using Staple.UI;

namespace Staple.Editor;

internal class UIImageTemplate : IEntityTemplate
{
    public string Name { get; set; } = "UI Image";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(UIImage));

        var image = entity.GetComponent<UIImage>();

        image.texture = Resources.Load<Texture>("Hidden/Textures/Sprites/DefaultSprite.png");
        image.size = new Vector2Int(50, 50);

        return entity;
    }
}
