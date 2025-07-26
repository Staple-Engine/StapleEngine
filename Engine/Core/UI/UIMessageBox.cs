using System;

namespace Staple.UI;

public class UIMessageBox : UIPanel
{
    private readonly string title;
    private readonly string message;
    private readonly string buttonTitle;
    private readonly string secondaryButtonTitle;

    private UIWindow window;
    private UIText text;

    public Action<UIMessageBox, int> OnButtonpressed;

    public UIMessageBox(UIManager manager, string title, string message, string buttonTitle, string secondaryButtonTitle) : base(manager)
    {
        this.title = title;
        this.message = message;
        this.buttonTitle = buttonTitle;
        this.secondaryButtonTitle = secondaryButtonTitle;

        BlockingInput = true;
    }

    public override void SetSkin(UISkin skin)
    {
    }

    public override void PerformLayout()
    {
        base.PerformLayout();
    }

    public override void Update(Vector2Int parentPosition)
    {
    }

    public override void Draw(Vector2Int parentPosition)
    {
    }
}
