using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Staple.UI;

public class UICheckBox(UIManager manager, string ID) : UIPanel(manager, ID)
{
    private Texture checkTexture;
    private Texture uncheckTexture;
    private Vector2Int labelOffset;
    private bool checkedValue;
    private int fontSize;
    private TextParameters parameters = new();

    public bool Checked
    {
        get
        {
            return checkedValue;
        }

        set
        {
            var previous = checkedValue;

            checkedValue = value;

            if(checkedValue)
            {

            }
            else
            {
            }

            if(previous != value)
            {
                OnValueChanged?.Invoke(this, value);
            }
        }
    }

    public string caption;

    public Action<UICheckBox, bool> OnValueChanged;

    public override void SetSkin(UISkin skin)
    {
        checkTexture = skin.GetTexture("CheckBox", "CheckTexture");
        uncheckTexture = skin.GetTexture("CheckBox", "UnCheckTexture");
        labelOffset = skin.GetVector2Int("CheckBox", "LabelOffset");
        
        fontSize = Manager.DefaultFontSize;

        parameters.TextColor(Manager.DefaultFontColor)
            .SecondaryTextColor(Manager.DefaultSecondaryFontColor);
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        {
            if (properties.TryGetValue(nameof(caption), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.String)
            {
                caption = p.GetString();
            }
        }

        {
            if (properties.TryGetValue("checked", out var t) &&
                t is JsonElement p &&
                (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False))
            {
                checkedValue = p.GetBoolean();
            }
        }
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
    }

    protected override void PerformLayout()
    {
        Size = new Vector2Int((checkTexture != null ? checkTexture.Size.X : 0) +
            MeasureTextSimple(caption, new TextParameters().FontSize(fontSize)).AbsoluteSize.X + labelOffset.X,
            checkTexture != null ? checkTexture.Size.Y : 0);
    }

    public override void Update(Vector2Int parentPosition)
    {
        PerformLayout();
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        if (IsCulled(position))
        {
            return;
        }

        var t = Checked ? checkTexture : uncheckTexture;

        DrawSprite(position, t.Size, t, Color.White.WithAlpha(Alpha));

        var textColor = parameters.textColor;
        var secondaryTextColor = parameters.secondaryTextColor;

        RenderText(caption, parameters
            .TextColor(textColor.WithAlpha(Alpha))
            .SecondaryTextColor(secondaryTextColor.WithAlpha(Alpha))
            .FontSize(fontSize)
            .Position(position + new Vector2Int(t.Width, 0) + labelOffset));

        parameters.textColor = textColor;
        parameters.secondaryTextColor = secondaryTextColor;
    }
}
