using System;

namespace Staple
{
    /// <summary>
    /// Marks a component as abstract, therefore not usable by itself
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class AbstractComponentAttribute : Attribute
    {
    }
}
