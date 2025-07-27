using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staple.UI;

[Serializable]
public class UILayout
{
    [Serializable]
    public class UIPanelData
    {
        public string control;
        public string x;
        public string y;
        public string wide;
        public string tall;
        public bool visible = true;

        public Dictionary<string, object> properties = [];

        public Dictionary<string, UIPanelData> children = [];
    }

    public Dictionary<string, Dictionary<string, UIPanelData>> data = [];
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, UILayout.UIPanelData>))]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, UILayout.UIPanelData>>))]
[JsonSerializable(typeof(UILayout.UIPanelData))]
[JsonSerializable(typeof(UILayout))]
internal partial class UILayoutSerializationContext : JsonSerializerContext
{
}
