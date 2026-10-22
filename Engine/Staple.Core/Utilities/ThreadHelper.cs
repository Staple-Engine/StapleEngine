using Staple.Internal;
using Staple.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Staple;

/// <summary>
/// Thread helper for running actions in the main thread
/// </summary>
public static class ThreadHelper
{
    private readonly struct DelayedAction(float delay, Action action)
    {
        public readonly TimeTracker time = new();
        public readonly float delay = delay;
        public readonly Action action = action;

        public bool CanTrigger() => time.ElapsedTimeNoReset >= delay;
    }

    private const int MaxActionsPerFrame = 50;

    private static Thread mainThread;
    private static readonly ConcurrentQueue<DelayedAction> pendingDelayedActions = [];
    private static readonly ConcurrentQueue<Action> pendingActions = [];

    public static bool IsMainThread => mainThread == Thread.CurrentThread;

    /// <summary>
    /// Attempts to initialize the threading system with the current thread as the main thread
    /// </summary>
    internal static void Initialize()
    {
        mainThread ??= Thread.CurrentThread;
    }

    /// <summary>
    /// Ensures we're currently in the main thread, otherwise throws an <see cref="InvalidOperationException"/>
    /// </summary>
    /// <param name="caller">The caller name. Autofilled.</param>
    internal static void EnsureMainThread([CallerMemberName] string caller = "")
    {
        if(Thread.CurrentThread == mainThread)
        {
            return;
        }

        DebugUtils.EnsureDebuggerBreaks();

        throw new InvalidOperationException($"{caller} must be called from the main thread! Try using ThreadHelper.Dispatch if you're in another thread.");
    }

    /// <summary>
    /// Performs a thread action safely
    /// </summary>
    /// <param name="action">The action to perform</param>
    internal static void PerformAction(Action action, TaskCompletionSource<bool> source)
    {
        try
        {
            action?.Invoke();

            source?.SetResult(true);
        }
        catch(Exception e)
        {
            source.SetException(e);

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

        for(var i = 0; i < MaxActionsPerFrame; i++)
        {
            if(pendingActions.TryDequeue(out var action))
            {
                PerformAction(action, null);
            }
            
            if(pendingDelayedActions.TryDequeue(out var delayedAction))
            {
                if (delayedAction.CanTrigger())
                {
                    PerformAction(delayedAction.action, null);
                }
                else
                {
                    pendingDelayedActions.Enqueue(delayedAction);
                }
            }
        }
    }

    /// <summary>
    /// Dispatches an action to run on the main thread
    /// </summary>
    /// <param name="action">The action to run</param>
    public static void Dispatch(Action action)
    {
        pendingActions.Enqueue(action);
    }

    /// <summary>
    /// Dispatches an action to run on the main thread and returns a task to await for
    /// </summary>
    /// <param name="action">The action to run</param>
    public static Task DispatchAsync(Action action)
    {
        var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        pendingActions.Enqueue(() =>
        {
            PerformAction(action, source);
        });

        return source.Task;
    }

    /// <summary>
    /// Dispatches an action to run on the main thread with a delay
    /// </summary>
    /// <param name="delay">The delay in seconds</param>
    /// <param name="action">The action to run</param>
    public static void DispatchDelayed(float delay, Action action)
    {
        pendingDelayedActions.Enqueue(new(delay, action));
    }

    /// <summary>
    /// Dispatches an action to run on the main thread with a delay and returns a task to await for
    /// </summary>
    /// <param name="delay">The delay in seconds</param>
    /// <param name="action">The action to run</param>
    public static Task DispatchDelayedAsync(float delay, Action action)
    {
        var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        pendingDelayedActions.Enqueue(new(delay, () =>
        {
            PerformAction(action, source);
        }));

        return source.Task;
    }
}
