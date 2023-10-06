using System;

namespace Staple.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomEditorAttribute : Attribute
    {
        public Type target;

        public CustomEditorAttribute(Type target)
        {
            this.target = target;
        }
    }
}
