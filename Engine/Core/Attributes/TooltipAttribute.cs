using System;

namespace Staple;

public class TooltipAttribute : Attribute
{
    public string caption;

    public TooltipAttribute(string caption)
    {
        this.caption = caption;
    }
}
