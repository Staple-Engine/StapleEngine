using System;

namespace Staple
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DisallowMultipleComponentAttribute : Attribute
    {
    }
}