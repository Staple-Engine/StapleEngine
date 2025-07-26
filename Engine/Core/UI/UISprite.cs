namespace Staple.UI;

public class UISprite(UIManager manager) : UIPanel(manager)
{
    public Sprite sprite;

    public override void SetSkin(UISkin skin)
    {
    }

    public override void PerformLayout()
    {
    }

    public override void Update(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        foreach (var child in Children)
        {
            child.Update(position);
        }
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = ParentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        DrawSprite(position, sprite.Rect.Size, sprite.texture, sprite.Rect, Color.White);

        foreach(var child in Children)
        {
            child.Draw(position);
        }
    }
}
