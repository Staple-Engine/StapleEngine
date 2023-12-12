using System;
using System.Threading;

namespace Staple.Internal
{
    public static class GuidGenerator
    {
        public static Guid Generate()
        {
            Thread.Sleep(25);

            return Guid.NewGuid();
        }
    }
}
