using System;

namespace Staple.Editor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItemAttribute : Attribute
    {
        public string path;

        public MenuItemAttribute(string path)
        {
            this.path = path;
        }
    }
}
