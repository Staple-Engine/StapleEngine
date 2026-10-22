#if DEBUG
using System.Diagnostics;
#endif

namespace Staple.Internal;

public static class DebugUtils
{
    /// <summary>
    /// Ensures the debugger breaks here, if enabled. Useful for debugging complex issues where exceptions might not be easy to capture to debug.
    /// </summary>
    [Conditional("DEBUG")]
    public static void EnsureDebuggerBreaks()
    {
#if DEBUG
        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }
#endif
    }
}
