using Staple.Internal;
using System.Collections.Generic;
using System.Text.Json;

namespace Staple.UI;

public class UITextBox(UIManager manager, string ID) : UIPanel(manager, ID)
{
    protected Texture backgroundTexture;
    protected Rect textureRect;
    protected string text;
    protected int cursorPosition;
    protected int textOffset;
    protected int padding;
    protected string fontName;
    protected int fontSize;

    protected Vector2Int lastSize;

    public bool isPassword;

    public string Text
    {
        get => text;

        set
        {
            text = value;

            cursorPosition = textOffset = 0;
        }
    }

    public override void SetSkin(UISkin skin)
    {
        backgroundTexture = skin.GetTexture("TextBox", "BackgroundTexture");
        textureRect = skin.GetRect("TextBox", "TextureRect");
        padding = skin.GetInt("TextBox", "Padding");
        fontSize = Manager.DefaultFontSize;

        SelectBoxExtraSize = new(textureRect.left + textureRect.right, textureRect.top + textureRect.bottom);
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        {
            if (properties.TryGetValue(nameof(text), out var t) &&
                t is JsonElement p &&
                p.ValueKind == JsonValueKind.String)
            {
                text = p.GetString();
            }
        }

        {
            if (properties.TryGetValue(nameof(isPassword), out var t) &&
                t is JsonElement p &&
                (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False))
            {
                isPassword = p.GetBoolean();
            }
        }
    }

    protected override void PerformLayout()
    {
        if(Size.Y < fontSize + 10)
        {
            Size = new(Size.X, fontSize + 10);
        }
    }

    public override void Update(Vector2Int parentPosition)
    {
        PerformLayout();
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        var size = Size + new Vector2Int(padding * 2, 0);

        if(IsCulled(position))
        {
            return;
        }

        DrawSpriteSliced(position, size, backgroundTexture, textureRect, Color.White.WithAlpha(Alpha));

        var text = Text;

        if(isPassword)
        {
            text = new string('*', text?.Length ?? 0);
        }

        var textSize = TextRenderer.instance.MeasureTextSimple(text.Substring(textOffset), new TextParameters().FontSize(fontSize));

        var actualTextSize = textSize.Position + textSize.Size;

        var offset = new Vector2Int(-textSize.left + padding, (int)(Size.Y * 0.25f));

        var count = 0;

        for(var i = 0; i < text.Length - textOffset; i++, count++)
        {
            var newSize = TextRenderer.instance.MeasureTextSimple(text.Substring(textOffset, i + 1), new TextParameters().FontSize(fontSize));

            var actualSize = newSize.Position + newSize.Size;

            if(actualSize.X >= Size.X)
            {
                break;
            }
        }

        var textParameters = new TextParameters()
            .FontSize(fontSize)
            .TextColor(new Color(0, 0, 0, Alpha))
            .Position(position + offset);

        RenderText(text.Substring(textOffset, count), textParameters);

        if(Manager.FocusedElement == this)
        {
            //Draw cursor
            var x = 0;

            for(var i = 0; i < cursorPosition; i++)
            {
                var cursorSize = MeasureTextSimple(text.Substring(textOffset, i + 1), new TextParameters()
                    .FontSize(fontSize)).AbsoluteSize;

                if(cursorSize.X >= Size.X + offset.X)
                {
                    break;
                }

                x = cursorSize.X;
            }

            DrawSprite(position + new Vector2Int(x + padding, 0), new(1, fontSize), Material.WhiteTexture, Color.Black.WithAlpha(Alpha));
        }
    }
}
