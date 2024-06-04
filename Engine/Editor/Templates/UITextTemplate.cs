using Staple.UI;

namespace Staple.Editor;

internal class UITextTemplate : IEntityTemplate
{
    public string Name { get; set; } = "UI Text";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(UIText));

        return entity;
    }
}
