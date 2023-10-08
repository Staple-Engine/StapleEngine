using System;

namespace Staple
{
    /// <summary>
    /// Allows hiding values in the editor inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HideInInspectorAttribute : Attribute
    {
    }
}
