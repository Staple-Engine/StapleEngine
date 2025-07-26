using System.Collections.Generic;

namespace Staple.UI;

public sealed class UILayout
{
    internal UIManager owner;

    internal readonly Dictionary<string, UIPanel> elements = [];

    public readonly string name;

    public UIPanel FindPanel(string ID) => elements.TryGetValue(ID, out var panel) ? panel : null;
}
