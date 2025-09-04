using System.Collections.Generic;

namespace Staple.UI;

public class UIScrollableFrame(UIManager manager, string ID) : UIPanel(manager, ID)
{
    public const int ScrollbarDraggableSize = 15;

    protected UIScrollBar verticalScrollbar;
    protected UIScrollBar horizontalScrollbar;

    public bool VerticalScrollbarVisible => verticalScrollbar?.Visible ?? false;
    public bool HorizontalScrollbarVisible => horizontalScrollbar?.Visible ?? false;

    public override void SetSkin(UISkin skin)
    {
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
    }

    protected void MakeScrolls()
    {
        verticalScrollbar = Manager.CreateElement<UIScrollBar>($"{ID}.VerticalScroll");
        horizontalScrollbar = Manager.CreateElement<UIScrollBar>($"{ID}.HorizontalScroll");

        verticalScrollbar.vertical = true;
    }

    public override void Update(Vector2Int parentPosition)
    {
        if(verticalScrollbar == null || horizontalScrollbar == null)
        {
            MakeScrolls();
        }

        if ((verticalScrollbar.parent == null || horizontalScrollbar.parent == null) &&
            Manager.GetElement(ID) != null)
        {
            verticalScrollbar.parent = this;
            horizontalScrollbar.parent = this;
        }

        var position = parentPosition + Position;

        var childrenSize = ChildrenSize;

        if(childrenSize.Y > Size.Y)
        {
            verticalScrollbar.Size = new Vector2Int(ScrollbarDraggableSize, Size.Y) - verticalScrollbar.SelectBoxExtraSize;
            verticalScrollbar.Position = new Vector2Int(Size.X - verticalScrollbar.Size.X, 0) +
                new Vector2Int(-verticalScrollbar.SelectBoxExtraSize.X / 2, verticalScrollbar.SelectBoxExtraSize.Y / 2);
            Translation = new(Translation.X,
                Math.RoundToInt(verticalScrollbar.Value / (float)verticalScrollbar.maxValue * (childrenSize.Y - Size.Y)));
        }

        verticalScrollbar.Visible = ChildrenSize.Y > Size.Y;

        if(childrenSize.X > Size.X)
        {
            horizontalScrollbar.Size = new Vector2Int(Size.X, ScrollbarDraggableSize) - horizontalScrollbar.SelectBoxExtraSize;

            horizontalScrollbar.Position = new Vector2Int(0, Size.Y - horizontalScrollbar.Size.Y) +
                new Vector2Int(horizontalScrollbar.SelectBoxExtraSize.X / 2, -horizontalScrollbar.SelectBoxExtraSize.Y / 2);

            Translation = new(Math.RoundToInt(horizontalScrollbar.Value / (float)horizontalScrollbar.maxValue) * (childrenSize.X - Size.X),
                Translation.Y);
        }

        horizontalScrollbar.Visible = childrenSize.X > Size.X;

        if(verticalScrollbar.Visible && horizontalScrollbar.Visible)
        {
            verticalScrollbar.Size -= new Vector2Int(0, horizontalScrollbar.SelectBoxExtraSize.Y + horizontalScrollbar.Size.Y);

            horizontalScrollbar.Size -= new Vector2Int(verticalScrollbar.SelectBoxExtraSize.X + verticalScrollbar.Size.X, 0);
        }

        if (verticalScrollbar.Visible && Manager.MouseOverElement == this)
        {
            var step = Input.MouseDelta.X switch
            {
                float f when f < 0 => 1,
                float f when f > 0 => -1,
                _ => 0,
            };

            var maxSteps = (verticalScrollbar.maxValue - verticalScrollbar.minValue) / verticalScrollbar.valueStep;
            var stepPower = (int)(maxSteps * 0.05f);

            if (step < 0)
            {
                if (verticalScrollbar.currentStep > stepPower)
                {
                    verticalScrollbar.currentStep -= stepPower;
                }
                else if (verticalScrollbar.currentStep > 0)
                {
                    verticalScrollbar.currentStep = 0;
                }
            }
            else if (step > 0)
            {
                if (verticalScrollbar.currentStep + stepPower < maxSteps)
                {
                    verticalScrollbar.currentStep += stepPower;
                }
                else if (verticalScrollbar.currentStep < maxSteps)
                {
                    verticalScrollbar.currentStep = maxSteps;
                }
            }
        }

        foreach(var child in Children)
        {
            child.Update(position);
        }
    }

    public override void Draw(Vector2Int parentPosition)
    {
        if(verticalScrollbar == null || horizontalScrollbar == null)
        {
            MakeScrolls();
        }

        var position = parentPosition + Position;

        if(IsCulled(position))
        {
            return;
        }

        //TODO: Background and scissoring

        foreach(var child in Children)
        {
            if(child == verticalScrollbar || child == horizontalScrollbar)
            {
                continue;
            }

            child.Draw(position - Translation);
        }

        verticalScrollbar.Draw(position);
        horizontalScrollbar.Draw(position);
    }
}
