using System;
using System.Collections.Generic;

namespace Staple.UI;

public class UIMessageBox(UIManager manager, string ID) : UIPanel(manager, ID)
{
    private readonly string title;
    private readonly string message;
    private readonly string buttonTitle;
    private readonly string secondaryButtonTitle;

    private UIWindow window;
    private UIText text;

    public Action<UIMessageBox, int> OnButtonpressed;

    protected override void OnConstructed()
    {
        BlockingInput = true;
    }

    public override void SetSkin(UISkin skin)
    {
    }

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
    }

    protected override void PerformLayout()
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
