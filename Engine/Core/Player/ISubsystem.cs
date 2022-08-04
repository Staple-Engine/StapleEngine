using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public interface ISubsystem
    {
        void Startup();

        void Shutdown();

        void Update();
    }
}
