using Staple.Internal;
using System.Collections.Generic;
using System.Text.Json;

namespace Staple.UI;

public class UIText(UIManager manager, string ID) : UIPanel(manager, ID)
{
    public int fontSize;

    public TextParameters parameters = new();

    public UITextAlignment alignment = UITextAlignment.Left;

    private string text;

    public string Text
    {
        get => text;

        set => SetText(value);
    }

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

    protected override void OnConstructed()
    {
        base.OnConstructed();

        DefaultSize = new(200, 16);
    }

    public override void SetSkin(UISkin skin)
    {
        fontSize = Manager.DefaultFontSize;

        parameters.TextColor(Manager.DefaultFontColor)
            .SecondaryTextColor(Manager.DefaultSecondaryFontColor);
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        {
            if (properties.TryGetValue(nameof(TextParameters.fontSize), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.Number)
            {
                fontSize = p.GetNumberValue<int>();
            }
        }

        {
            if (properties.TryGetValue(nameof(TextParameters.textColor), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.String)
            {
                parameters.textColor = new Color(p.GetString());
            }
        }

        {
            if (properties.TryGetValue(nameof(TextParameters.secondaryTextColor), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.String)
            {
                parameters.secondaryTextColor = new Color(p.GetString());
            }
        }

        {
            if (properties.TryGetValue(nameof(TextParameters.borderColor), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.String)
            {
                parameters.borderColor = new Color(p.GetString());
            }
        }

        {
            if (properties.TryGetValue(nameof(TextParameters.borderSize), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.Number)
            {
                parameters.borderSize = p.GetNumberValue<int>();
            }
        }

        {
            if (properties.TryGetValue(nameof(TextParameters.font), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.String)
            {
                var guid = AssetDatabase.GetAssetGuid(p.GetString());

                guid ??= p.GetString();

                parameters.font = guid;
            }
        }

        {
            if (properties.TryGetValue(nameof(TextParameters.rotation), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.Number)
            {
                parameters.rotation = p.GetNumberValue<float>();
            }
        }

        {
            if (properties.TryGetValue(nameof(text), out var t) &&
                t is JsonElement str &&
                str.ValueKind == JsonValueKind.String)
            {
                Text = str.GetString();
            }
        }
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
                RenderText(Strings[i], parameters
                    .FontSize(fontSize)
                    .Position(position + new Vector2Int(
                        (Size.X - MeasureTextSimple(Strings[i], new TextParameters().FontSize(fontSize)).AbsoluteSize.X) / 2, textYOffset)));
            }
            else if(alignment.HasFlag(UITextAlignment.Right))
            {
                RenderText(Strings[i], parameters
                    .FontSize(fontSize)
                    .Position(position + new Vector2Int(
                        Size.X - MeasureTextSimple(Strings[i], new TextParameters().FontSize(fontSize)).AbsoluteSize.X, textYOffset)));
            }
            else
            {
                RenderText(Strings[i], parameters
                    .FontSize(fontSize)
                    .Position(position + new Vector2Int(0, textYOffset)));
            }
        }
    }

    public void SetText(string text, bool autoExpandHeight = false)
    {
        this.text = text;

        Strings = FitTextOnRect(text, new TextParameters()
            .FontSize(fontSize), autoExpandHeight ? new(Size.X, 999999) : Size);

        if(autoExpandHeight)
        {
            Size = new(Size.X,
                Size.Y > Strings.Length * (fontSize + 4) ? Size.Y : Strings.Length * (fontSize + 4));
        }
    }
}
