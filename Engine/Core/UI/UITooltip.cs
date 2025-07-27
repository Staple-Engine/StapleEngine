using System.Collections.Generic;

namespace Staple.UI;

public class UITooltip(UIManager manager, string ID) : UIPanel(manager, ID)
{
    internal UIPanel source;

    public string overrideText;

    public int fontSize = 16;

    public override void SetSkin(UISkin skin)
    {
        fontSize = Manager.DefaultFontSize;
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
    }

    protected override void PerformLayout()
    {
        var text = source?.Tooltip ?? overrideText ?? "";

        var actualFontSize = MeasureTextSimple(text, new TextParameters().FontSize(fontSize)).AbsoluteSize;

        Size = new(actualFontSize.X + 10, actualFontSize.Y + 10);
    }

    public override void Update(Vector2Int parentPosition)
    {
        PerformLayout();
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var element = Manager.MouseOverElement;

        if((source == null || element != source) && string.IsNullOrEmpty(overrideText))
        {
            return;
        }

        if(string.IsNullOrEmpty(overrideText) == false && source == null && element != null)
        {
            return;
        }

        var text = source?.Tooltip ?? overrideText ?? "";

        var actualFontSize = MeasureTextSimple(text, new TextParameters()
            .FontSize(fontSize)).AbsoluteSize;

        var position = parentPosition + Position;

        position.Y -= Size.Y;

        if(position.X > Manager.CanvasSize.X / 2)
        {
            position.X -= Size.X;
        }

        if(IsCulled(position))
        {
            return;
        }

        DrawSprite(position, Size, Material.WhiteTexture, new Color(0.98f, 0.96f, 0.815f, 1));

        RenderText(text, new TextParameters()
            .FontSize(fontSize)
            .TextColor(Color.Black)
            .Position(position + (Size - actualFontSize) / 2));
    }
}
