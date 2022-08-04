using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public interface IEntitySystem
    {
        Type[] targetComponents { get; }

        void Process(Entity entity);
    }
}
