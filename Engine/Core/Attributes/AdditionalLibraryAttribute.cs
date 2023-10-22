using System;
using System.Collections.Generic;

namespace Staple
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AdditionalLibraryAttribute : Attribute
    {
        public AppPlatform platform;
        public string path;

        public AdditionalLibraryAttribute(AppPlatform platform, string path)
        {
            this.platform = platform;
            this.path = path;
        }
    }
}
