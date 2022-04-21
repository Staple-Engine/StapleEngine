using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [Serializable]
    public class AppSettings
    {
        public bool runInBackground = false;
        public string appName;
    }
}
