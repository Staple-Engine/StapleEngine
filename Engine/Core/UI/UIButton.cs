namespace Staple.UI;

public class UIButton(UIManager manager) : UIPanel(manager)
{
    protected Texture normalTexture;
    protected Texture focusedTexture;

    protected Rect textureRect;
    protected Vector2Int labelOffset;
    protected Color fontColor = Color.White;

    public string caption;

    public int fontSize = 16;

    public override void SetSkin(UISkin skin)
    {
        normalTexture = skin.GetTexture("Button", "BackgroundTexture");
        focusedTexture = skin.GetTexture("Button", "FocusedTexture");
        textureRect = skin.GetRect("Button", "TextureRect");
        labelOffset = skin.GetVector2Int("Button", "LabelOffset");
        fontColor = skin.GetColor("Button", "FontColor");
        fontSize = skin.GetInt("Button", "FontSize");
    }

    protected override void PerformLayout()
    {
        var size = MeasureTextSimple(caption, new TextParameters().FontSize(fontSize));

        var actualSize = size.Position + size.Size;

        var s = Size;

        if(actualSize.X > s.X)
        {
            s.X = actualSize.X + 10;
        }

        if(actualSize.Y > s.Y)
        {
            s.Y = actualSize.Y + 10;
        }

        Size = s;
    }

    public override void Update(Vector2Int parentPosition)
    {
        PerformLayout();
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        var caption = this.caption ?? "";

        var size = MeasureTextSimple(caption, new TextParameters().FontSize(fontSize)).AbsoluteSize;

        DrawSpriteSliced(position, Size, clickPressed ? focusedTexture : normalTexture, textureRect, Color.White);

        var offset = new Vector2Int((Size.X - size.X) / 2 + labelOffset.X, labelOffset.Y);

        var textParameters = new TextParameters()
            .TextColor(fontColor)
            .FontSize(fontSize)
            .Position(position + offset);

        RenderText(caption, textParameters);
    }
}
