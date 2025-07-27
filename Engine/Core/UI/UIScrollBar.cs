using System;

namespace Staple.UI;

public class UIScrollBar(UIManager manager, bool vertical) : UIPanel(manager)
{
    private Rect backgroundTextureRect;
    private Rect handleTextureRect;
    private int padding;
    private int minSize;
    private Texture backgroundTexture;
    private Texture handleTexture;
    private readonly bool vertical = vertical;

    public int minValue;
    public int maxValue = 100;
    public int currentStep;
    public int valueStep = 1;

    public int Value
    {
        get
        {
            if(currentStep > maxValue / valueStep)
            {
                currentStep = maxValue / valueStep;
            }

            return minValue + currentStep * valueStep;
        }
    }

    public Action<UIPanel> OnChange;

    public override void SetSkin(UISkin skin)
    {
        backgroundTextureRect = skin.GetRect("Scrollbar", "BackgroundTextureRect");
        handleTextureRect = skin.GetRect("Scrollbar", "HandleTextureRect");
        padding = skin.GetInt("Scrollbar", "Padding");
        minSize = skin.GetInt("Scrollbar", "MinSize");
        backgroundTexture = skin.GetTexture("Scrollbar", "BackgroundTexture");
        handleTexture = skin.GetTexture("Scrollbar", "HandleTexture");
    }

    protected override void PerformLayout()
    {
    }

    public override void Update(Vector2Int parentPosition)
    {
        if(Manager.FocusedElement != this || Input.GetMouseButton(MouseButton.Left) == false)
        {
            return;
        }

        var position = parentPosition + Position;

        var steps = (maxValue - minValue) / valueStep;

        for (var i = 0; i <= steps; i++)
        {
            var stepOffset = (vertical ? (Size.Y + SelectBoxExtraSize.Y) / steps * i :
                (Size.X + SelectBoxExtraSize.X) / steps * i);

            var min = position + (vertical ? new Vector2Int(padding, stepOffset + padding - SelectBoxExtraSize.Y / 2) :
                new Vector2Int(stepOffset + padding - SelectBoxExtraSize.X / 2, padding)) - SelectBoxExtraSize / 2;

            var max = position + (vertical ? new Vector2Int(Size.X - padding * 2 + backgroundTextureRect.left + backgroundTextureRect.right,
                stepOffset + padding - SelectBoxExtraSize.Y / 2 + minSize) :
                new Vector2Int(stepOffset + padding - SelectBoxExtraSize.X / 2 + minSize,
                Size.Y - padding * 2 + backgroundTextureRect.top + backgroundTextureRect.bottom)) + SelectBoxExtraSize / 2;

            var aabb = AABB.CreateFromMinMax(new(min, 0), new(max, 0));

            if(aabb.Contains(new(Input.PointerPosition, 0)))
            {
                var needsChange = currentStep != i;

                currentStep = i;

                if(needsChange)
                {
                    OnChange?.Invoke(this);
                }

                return;
            }
        }
    }

    public override void Draw(Vector2Int parentPosition)
    {
        var position = parentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        var steps = (maxValue - minValue) / valueStep;

        var stepOffset = (vertical ? (Size.Y + SelectBoxExtraSize.Y) / steps * currentStep :
            (Size.X + SelectBoxExtraSize.X) / steps * currentStep);

        DrawSpriteSliced(position, Size, backgroundTexture, backgroundTextureRect, Color.White);

        var handlePosition = position + (vertical ? new Vector2Int(padding, stepOffset + padding) - backgroundTextureRect.Position :
            new Vector2Int(stepOffset + padding, padding) - backgroundTextureRect.Position);

        var handleSize = vertical ? new Vector2Int(Size.X - padding * 2 + backgroundTextureRect.left + backgroundTextureRect.right, minSize) :
            new Vector2Int(minSize, Size.Y - padding * 2 + backgroundTextureRect.top + backgroundTextureRect.bottom);

        DrawSpriteSliced(handlePosition, handleSize, handleTexture, handleTextureRect, Color.White);
    }
}
