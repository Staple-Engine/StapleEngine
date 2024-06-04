using Staple.UI;

namespace Staple.Editor;

internal class UIContainerTemplate : IEntityTemplate
{
    public string Name { get; set; } = "UI Container";

    public Entity Create()
    {
        var entity = Entity.Create(Name, typeof(Transform), typeof(UIContainer));

        return entity;
    }
}
