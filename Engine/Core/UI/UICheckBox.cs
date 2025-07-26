using System;

namespace Staple.UI;

public class UICheckBox(UIManager manager) : UIPanel(manager)
{
    private Texture checkTexture;
    private Texture uncheckTexture;
    private int fontSize;
    private Color fontColor;
    private Vector2Int labelOffset;
    private bool checkedValue;

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
        fontSize = Manager.DefaultFontSize;
        fontColor = Manager.DefaultFontColor;
        labelOffset = skin.GetVector2Int("CheckBox", "LabelOffset");
    }

    public override void PerformLayout()
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

        DrawSprite(position, t.Size, t, Color.White);

        RenderText(caption, new TextParameters()
            .FontSize(fontSize)
            .TextColor(fontColor)
            .Position(position + new Vector2Int(t.Width, 0) + labelOffset));
    }
}
