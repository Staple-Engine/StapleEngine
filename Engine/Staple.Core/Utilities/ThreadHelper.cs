using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple;

/// <summary>
/// Thread helper for running actions in the main thread
/// </summary>
public static class ThreadHelper
{
    private static Thread mainThread;
    private static readonly List<Action> pendingActions = [];
    private static readonly Lock threadLock = new();

    public static bool IsMainThread => mainThread == Thread.CurrentThread;

    /// <summary>
    /// Attempts to initialize the threading system with the current thread as the main thread
    /// </summary>
    internal static void Initialize()
    {
        mainThread ??= Thread.CurrentThread;
    }

    /// <summary>
    /// Performs a thread action safely
    /// </summary>
    /// <param name="action">The action to perform</param>
    internal static void PerformAction(Action action)
    {
        try
        {
            action?.Invoke();
        }
        catch(Exception e)
        {
            Log.Debug($"[Threading] Failed to perform an action: {e}");
        }
    }

    /// <summary>
    /// Handles all pending actions in the main thread
    /// </summary>
    internal static void Update()
    {
        if(!IsMainThread)
        {
            return;
        }

        for(; ; )
        {
            Action current = null;

            lock (threadLock)
            {
                current = pendingActions.Count > 0 ? pendingActions[0] : null;

                if (current != null)
                {
                    pendingActions.RemoveAt(0);
                }
            }

            if(current == null)
            {
                return;
            }

            PerformAction(current);
        }
    }

    /// <summary>
    /// Dispatches an action to run on the main thread
    /// </summary>
    /// <param name="action">The action to run</param>
    public static void Dispatch(Action action)
    {
        lock(threadLock)
        {
            pendingActions.Add(action);
        }
    }
}
