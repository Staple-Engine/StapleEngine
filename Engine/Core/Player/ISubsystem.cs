using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public enum SubsystemType
    {
        Render,
        FixedUpdate,
        Update
    }

    public interface ISubsystem
    {
        SubsystemType type { get; }

        void Startup();

        void Shutdown();

        void Update();
    }
}
