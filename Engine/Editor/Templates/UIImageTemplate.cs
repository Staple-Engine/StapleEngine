using Staple.UI;

namespace Staple.Editor;

internal class UIImageTemplate : IEntityTemplate
{
    public string Name { get; set; } = "UI Image";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(UIImage));

        return entity;
    }
}
