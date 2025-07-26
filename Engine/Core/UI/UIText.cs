namespace Staple.UI;

public class UIText(UIManager manager) : UIPanel(manager)
{
    public int fontSize;

    public TextParameters parameters = new();

    public UITextAlignment alignment = UITextAlignment.Left;

    public string Text { get; private set; } = "";

    public string[] Strings { get; private set; } = [];

    public Vector2Int TextSize
    {
        get
        {
            var outValue = Vector2Int.Zero;

            for (var i = 0; i < Strings.Length; i++)
            {
                var textSize = MeasureTextSimple(Strings[i], new TextParameters().FontSize(fontSize)).AbsoluteSize;

                outValue.Y += textSize.Y;

                if(outValue.X < textSize.X)
                {
                    outValue.X = textSize.X;
                }
            }

            return outValue;
        }
    }

    public override void SetSkin(UISkin skin)
    {
        fontSize = Manager.DefaultFontSize;

        parameters.TextColor(Manager.DefaultFontColor)
            .SecondaryTextColor(Manager.DefaultSecondaryFontColor);
    }

    public override void Update(Vector2Int parentPosition)
    {
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        var yOffset = 0;

        if(alignment.HasFlag(UITextAlignment.VCenter))
        {
            yOffset = (Size.Y - (Strings.Length * (fontSize + 4))) / 2;
        }

        for (int i = 0, textYOffset = yOffset; i < Strings.Length; i++, textYOffset += fontSize + 4)
        {
            if(alignment.HasFlag(UITextAlignment.Center))
            {
                RenderText(Strings[i], new TextParameters()
                    .FontSize(fontSize)
                    .Position(position + new Vector2Int(
                        (Size.X - MeasureTextSimple(Strings[i], new TextParameters().FontSize(fontSize)).AbsoluteSize.X) / 2, textYOffset)));
            }
            else if(alignment.HasFlag(UITextAlignment.Right))
            {
                RenderText(Strings[i], new TextParameters()
                    .FontSize(fontSize)
                    .Position(position + new Vector2Int(
                        Size.X - MeasureTextSimple(Strings[i], new TextParameters().FontSize(fontSize)).AbsoluteSize.X, textYOffset)));
            }
            else
            {
                RenderText(Strings[i], new TextParameters()
                    .FontSize(fontSize)
                    .Position(position + new Vector2Int(0, textYOffset)));
            }
        }
    }

    public void SetText(string text, bool autoExpandHeight = false)
    {
        Text = text;
        Strings = FitTextOnRect(text, new TextParameters()
            .FontSize(fontSize), autoExpandHeight ? new(Size.X, 999999) : Size);

        if(autoExpandHeight)
        {
            Size = new(Size.X,
                Size.Y > Strings.Length * (fontSize + 4) ? Size.Y : Strings.Length * (fontSize + 4));
        }
    }
}
