using System;

namespace Staple.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class CustomEditorAttribute : Attribute
    {
        public Type target;
    }
}
