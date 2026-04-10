using System;

namespace Staple.UI;

public interface IUIDroppable
{
    Action<UIPanel> OnDrop { get; set; }
}
