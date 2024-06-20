using System;

namespace Staple
{
    /// <summary>
    /// Describes an otherwise non-serializable field as serializable. Usually used for private fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeFieldAttribute : Attribute
    {
    }
}