﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Staple.Internal;

public static class StapleHooks
{
    private static readonly Dictionary<StapleHookEvent, ObservableBoxStrong> hooks = [];

    public static void RegisterHook(IStapleHook hook)
    {
        if(hook == null || hook.Name == null || hook.HookedEvents == null)
        {
            return;
        }

        foreach (var e in hook.HookedEvents)
        {
            if(hooks.TryGetValue(e, out var box) == false)
            {
                box = new();

                hooks.Add(e, box);
            }

            box.AddObserver(hook);
        }
    }

    public static void RemoveHook(IStapleHook hook)
    {
        if(hook == null)
        {
            return;
        }

        hook.OnEvent(StapleHookEvent.Cleanup, null);

        foreach(var e in hook.HookedEvents)
        {
            if(hooks.TryGetValue(e, out var box) == false)
            {
                continue;
            }

            box.RemoveObserver(hook);
        }
    }

    internal static void ExecuteHooks(StapleHookEvent e, object args)
    {
        if(hooks.TryGetValue(e, out var box) == false)
        {
            return;
        }

        box.Emit((observer) =>
        {
            var hook = (IStapleHook)observer;

            try
            {
                hook.OnEvent(e, args);
            }
            catch (Exception ex)
            {
                Log.Error($"[{e}] Executing hook '{hook.Name}' failed and the hook is being removed: {ex}");

                box.RemoveObserver(observer);
            }
        });
    }

    internal static void RemoveAll(Assembly assembly)
    {
        foreach(var pair in hooks)
        {
            pair.Value.RemoveAll(assembly);
        }
    }
}