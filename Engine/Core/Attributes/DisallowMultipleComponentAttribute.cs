using System;

namespace Staple
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class DisallowMultipleComponentAttribute : Attribute
    {
    }
}