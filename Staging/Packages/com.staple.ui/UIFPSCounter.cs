using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Staple.UI;

public class UIFPSCounter(UIManager manager, string ID) : UIText(manager, ID)
{
    public string format = "FPS: {0}";

    public override void ApplyLayoutProperties(Dictionary<string, object> properties)
    {
        base.ApplyLayoutProperties(properties);

        if (properties.TryGetValue("format", out var f) && f is JsonElement e && e.ValueKind == JsonValueKind.String)
        {
            format = e.GetString();
        }
    }

    public override void Update(Vector2Int parentPosition)
    {
        base.Update(parentPosition);

        try
        {
            Text = string.Format(format, Time.FPS);
        }
        catch(Exception)
        {
            Text = Time.FPS.ToString();
        }
    }
}
