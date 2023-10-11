using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple
{
    public static class Threading
    {
        private static Thread mainThread;
        private static readonly List<Action> pendingActions = new();
        private static readonly object threadLock = new();

        public static bool IsMainThread => mainThread == Thread.CurrentThread;

        internal static void Initialize()
        {
            mainThread ??= Thread.CurrentThread;
        }

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

        internal static void Update()
        {
            if(IsMainThread == false)
            {
                return;
            }

            lock(threadLock)
            {
                foreach(var action in pendingActions)
                {
                    PerformAction(action);
                }

                pendingActions.Clear();
            }
        }

        public static void Dispatch(Action action)
        {
            if(IsMainThread)
            {
                PerformAction(action);
            }
            else
            {
                lock(threadLock)
                {
                    pendingActions.Add(action);
                }
            }
        }
    }
}
