using System;

namespace Staple.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class CustomEditorAttribute : Attribute
    {
        public Type target;

        public CustomEditorAttribute(Type target)
        {
            this.target = target;
        }
    }
}
