using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    internal class ResourceLocator
    {
        public string basePath;

        public static ResourceLocator instance = new ResourceLocator();

        public byte[] LoadFile(string path)
        {
            if(basePath == null)
            {
                return null;
            }

            try
            {
                return File.ReadAllBytes(Path.Combine(basePath, path));
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
