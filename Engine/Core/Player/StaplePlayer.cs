using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public static class StaplePlayer
    {
        public static void Run(string[] args)
        {
            new AppPlayer(new AppSettings()
            {
                appName = "Test",
                runInBackground = false,
            }, args).Run();
        }
    }
}
