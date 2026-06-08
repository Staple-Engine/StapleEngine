using Staple.UI;

namespace Staple.Editor.Templates;

public class UICanvasTemplate : IEntityTemplate
{
    public string Name { get; set; } = "UI Canvas";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(UICanvas));

        return entity;
    }
}
