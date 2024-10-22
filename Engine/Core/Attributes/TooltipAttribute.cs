using System;

namespace Staple;

[AttributeUsage(AttributeTargets.Field)]
public class TooltipAttribute(string caption) : Attribute
{
    public string caption = caption;
}
