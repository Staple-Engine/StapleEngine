using System;

namespace Staple.UI;

public interface IUIDraggable
{
    Action<UIPanel> OnDragBegin { get; set; }

    Action<UIPanel> OnDragging { get; set; }

    Action<UIPanel> OnDragEnd { get; set; }

    void DrawDraggable(Vector2Int mousePosition);
}
