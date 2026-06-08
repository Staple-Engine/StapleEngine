using System;
using System.Collections.Generic;

namespace Staple.Utilities;

/// <summary>
/// Gathers callbacks to run later. Used for threading situations when you are in the middle of a mutation event.
/// </summary>
public class CallbackGatherer
{
    private List<Action> pendingCallbacks = new();

    private object lockObject = new();

    /// <summary>
    /// Adds a callback to the callbacks list
    /// </summary>
    /// <param name="callback">The callback to run</param>
    public void AddCallback(Action callback)
    {
        lock(lockObject)
        {
            pendingCallbacks.Add(callback);
        }
    }

    /// <summary>
    /// Performns all callbacks
    /// </summary>
    public void PerformAll()
    {
        lock(lockObject)
        {
            while (pendingCallbacks.Count > 0)
            {
                var callbacks = pendingCallbacks.ToArray();

                pendingCallbacks.Clear();

                foreach(var callback in callbacks)
                {
                    try
                    {
                        callback?.Invoke();
                    }
                    catch(Exception e)
                    {
                        Log.Error($"[{GetType().Name}] Failed to run callbacks: {e}");
                    }
                }
            }
        }
    }
}