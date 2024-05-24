namespace Staple;

public interface IUIElement : IComponent
{
    void Render(Vector2Int position, ushort viewID);
}
