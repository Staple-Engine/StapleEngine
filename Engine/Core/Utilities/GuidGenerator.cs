using System;
using System.Threading;

namespace Staple.Internal;

/// <summary>
/// Generates GUIDs safely. Uses a delay to ensure they're unique.
/// </summary>
public static class GuidGenerator
{
    /// <summary>
    /// Generates a new GUID. Do notice that it'll cause a delay of 25ms each time.
    /// </summary>
    /// <returns>The new Guid</returns>
    public static Guid Generate()
    {
        Thread.Sleep(25);

        return Guid.NewGuid();
    }
}
