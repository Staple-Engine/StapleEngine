using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Staple.Internal;

/// <summary>
/// Window for rendering
/// </summary>
internal class RenderWindow
{
    public const int ClearView = 0;

    //In case we have more than one window in the future
    internal static int windowReferences = 0;
    internal static int bgfxReferences = 0;

    internal int width = 0;
    internal int height = 0;
    internal bool hasFocus = true;
    internal IRenderWindow window;
    internal bgfx.RendererType rendererType;
    internal bgfx.ResetFlags resetFlags;
    internal bool shouldStop = false;
    internal bool shouldRender = true;
    internal bool forceContextLoss = false;
    internal object renderLock = new();

    public Action OnFixedUpdate;
    public Action OnUpdate;
    public Action OnInit;
    public Action OnCleanup;
    public Action<bool> OnScreenSizeChange;
    public Action<Vector2Int> OnMove;

    public int Width => width;

    public int Height => height;

    public bool HasFocus => hasFocus;

    public bool Maximized => window?.Maximized ?? false;

    public int MonitorIndex => window?.MonitorIndex ?? 0;

    public string Title
    {
        get => window.Title;

        set => window.Title = value;
    }

    private bool renderThreadReady = false;
    private Thread renderThread;
    private bgfx.Init init = new();
    private AppPlatform currentPlatform;
    private CursorLockMode lastCursorLockMode;

    public bool Paused => hasFocus == false && AppSettings.Current.runInBackground == false;

    public static RendererType CurrentRenderer { get; internal set; }

    /// <summary>
    /// Runs the window main loop
    /// </summary>
    public void Run()
    {
        ThreadHelper.Initialize();

        if (AppSettings.Current.multiThreadedRenderer)
        {
            MultiThreadedLoop();
        }
        else
        {
            SingleThreadLoop();
        }
    }

    internal void CheckContextLost()
    {
        if (window.ContextLost || forceContextLoss)
        {
            forceContextLoss = false;
            window.ContextLost = false;

            ResourceManager.instance.Destroy(ResourceManager.DestroyMode.Normal);

            bgfx.shutdown();

            InitBGFX();

            ResourceManager.instance.RecreateResources();
        }
    }

    private void SingleThreadLoop()
    {
        try
        {
            OnScreenSizeChange?.Invoke(window.IsFocused);
        }
        catch(Exception)
        {
        }

        try
        {
            OnInit?.Invoke();

            StapleHooks.ExecuteHooks(StapleHookEvent.Init, null);
        }
        catch (Exception e)
        {
            Log.Error($"RenderWindow Init Exception: {e}");

            shouldStop = true;
        }

        var last = DateTime.UtcNow;

        var fixedTimer = 0.0f;

        while (window.ShouldClose == false && shouldStop == false)
        {
            PerformanceProfilerSystem.StartFrame();

            Input.UpdateState();

            window.PollEvents();

            lock (renderLock)
            {
                shouldRender = window.Unavailable == false && (AppSettings.Current.runInBackground == true || window.IsFocused == true);
            }

            if (window.Unavailable)
            {
                continue;
            }

            var size = window.Size;

            if ((size.X != width || size.Y!= height) && window.ShouldClose == false)
            {
                width = size.X;
                height = size.Y;

                try
                {
                    OnScreenSizeChange?.Invoke(hasFocus);
                }
                catch (Exception)
                {
                }
            }

            CheckContextLost();

            if (window.IsFocused != hasFocus)
            {
                hasFocus = window.IsFocused;

                if(Platform.IsDesktopPlatform)
                {
                    if(hasFocus && lastCursorLockMode != Cursor.LockState)
                    {
                        Cursor.LockState = lastCursorLockMode;
                    }
                    else
                    {
                        lastCursorLockMode = Cursor.LockState;

                        Cursor.LockState = CursorLockMode.None;
                    }
                }

                try
                {
                    OnScreenSizeChange?.Invoke(hasFocus);
                }
                catch (Exception)
                {
                }

                if (hasFocus == false)
                {
                    continue;
                }
            }

            CheckEvents();

            if (Paused)
            {
                fixedTimer = 0;

                //Prevent CPU over-use
                Thread.Sleep(100);
            }
            else
            {
                var current = DateTime.UtcNow;

                fixedTimer += (float)(current - last).TotalSeconds;

                //Prevent hard stuck
                var currentFixedTime = 0.0f;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && currentFixedTime < AppSettings.Current.maximumFixedTimestepTime)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    OnFixedUpdate?.Invoke();

                    StapleHooks.ExecuteHooks(StapleHookEvent.FixedUpdate, null);

                    currentFixedTime += Time.fixedDeltaTime;
                }

                if(currentFixedTime >= AppSettings.Current.maximumFixedTimestepTime)
                {
                    fixedTimer = 0;
                }
            }

            RenderFrame(ref last);

            ThreadHelper.Update();
        }

        try
        {
            OnCleanup?.Invoke();

            StapleHooks.ExecuteHooks(StapleHookEvent.Cleanup, null);
        }
        catch (Exception)
        {
        }
    }

    private void MultiThreadedLoop()
    {
        renderThread = new Thread(new ThreadStart(RenderThread));

        renderThread.Start();

        for (; ; )
        {
            lock (renderLock)
            {
                if (renderThread.IsAlive == false)
                {
                    if (renderThreadReady)
                    {
                        if (bgfxReferences > 0)
                        {
                            bgfxReferences--;

                            if (bgfxReferences == 0)
                            {
                                bgfx.shutdown();
                            }
                        }

                        if (windowReferences > 0)
                        {
                            windowReferences--;

                            if (windowReferences == 0)
                            {
                                window.Terminate();
                            }
                        }

                        Environment.Exit(1);

                        return;
                    }
                }

                if (renderThreadReady)
                {
                    break;
                }
            }

            Thread.Sleep(25);
        }

        OnScreenSizeChange?.Invoke(window.IsFocused);

        try
        {
            OnInit?.Invoke();

            StapleHooks.ExecuteHooks(StapleHookEvent.Init, null);
        }
        catch (Exception e)
        {
            Log.Error($"Error initializing: {e}");

            shouldStop = true;
        }

        var last = DateTime.UtcNow;

        var fixedTimer = 0.0f;

        while (window.ShouldClose == false && shouldStop == false)
        {
            PerformanceProfilerSystem.StartFrame();

            Input.UpdateState();

            window.PollEvents();

            lock (renderLock)
            {
                shouldRender = window.Unavailable == false && (AppSettings.Current.runInBackground == true || window.IsFocused == true);
            }

            if (window.Unavailable)
            {
                continue;
            }

            var size = window.Size;

            if ((size.X != width || size.Y != height) && window.ShouldClose == false)
            {
                width = size.X;
                height = size.Y;

                try
                {
                    OnScreenSizeChange?.Invoke(hasFocus);
                }
                catch (Exception)
                {
                }
            }

            CheckContextLost();

            if (window.IsFocused != hasFocus)
            {
                hasFocus = window.IsFocused;

                try
                {
                    OnScreenSizeChange?.Invoke(hasFocus);
                }
                catch (Exception)
                {
                }

                if (hasFocus == false)
                {
                    continue;
                }
            }

            if (Paused)
            {
                fixedTimer = 0;

                //Prevent CPU over-use
                Thread.Sleep(100);
            }
            else
            {
                var current = DateTime.UtcNow;

                fixedTimer += (float)(current - last).TotalSeconds;

                //Prevent hard stuck
                var currentFixedTime = 0.0f;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && currentFixedTime < AppSettings.Current.maximumFixedTimestepTime)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    OnFixedUpdate?.Invoke();

                    StapleHooks.ExecuteHooks(StapleHookEvent.FixedUpdate, null);

                    currentFixedTime += Time.fixedDeltaTime;
                }

                if (currentFixedTime >= AppSettings.Current.maximumFixedTimestepTime)
                {
                    fixedTimer = 0;
                }
            }

            last = DateTime.UtcNow;

            ThreadHelper.Update();
        }

        lock(renderLock)
        {
            try
            {
                OnCleanup?.Invoke();

                StapleHooks.ExecuteHooks(StapleHookEvent.Cleanup, null);
            }
            catch (Exception)
            {
            }
        }

        if (renderThread != null)
        {
            lock (renderLock)
            {
                shouldStop = true;
            }

            for (; ;)
            {
                if (renderThread.IsAlive == false)
                {
                    break;
                }

                ThreadHelper.Update();

                Thread.Sleep(25);
            }
        }
    }

    /// <summary>
    /// Cleans up this window's resources
    /// </summary>
    public void Cleanup()
    {
        if (bgfxReferences > 0)
        {
            bgfxReferences--;

            if (bgfxReferences == 0)
            {
                bgfx.shutdown();
            }
        }

        if (windowReferences > 0)
        {
            windowReferences--;

            if (windowReferences == 0)
            {
                ThreadHelper.Dispatch(() =>
                {
                    window.Terminate();
                });
            }
        }
    }

    internal void InitBGFX()
    {
        var renderers = new List<RendererType>();

        init = new bgfx.Init();

        unsafe
        {
            fixed(bgfx.Init *i = &init)
            {
                bgfx.init_ctor(i);
            }

            init.platformData.ndt = null;

            var platform = Platform.CurrentPlatform;

            if (platform.HasValue == false)
            {
                Log.Error("[RenderWindow] Unsupported platform");

                bgfxReferences--;

                if(bgfxReferences == 0)
                {
                    bgfx.shutdown();
                }

                windowReferences--;

                if(windowReferences == 0)
                {
                    window.Terminate();
                }

                Environment.Exit(1);

                return;
            }

            currentPlatform = platform.Value;

            window.GetNativePlatformData(currentPlatform, out var nativeWindowType, out var windowPointer, out var monitorPointer);

            init.platformData.ndt = monitorPointer.ToPointer();
            init.platformData.nwh = windowPointer.ToPointer();

            if (nativeWindowType == NativeWindowType.Wayland)
            {
                init.platformData.type = bgfx.NativeWindowHandleType.Wayland;
            }

            Log.Debug($"[RenderWindow] platformData ndt: {(nint)init.platformData.ndt} nwh {(nint)init.platformData.nwh}");

            if (AppSettings.Current.renderers.TryGetValue(currentPlatform, out renderers) == false)
            {
                Log.Error($"[RenderWindow] No Renderers found for platform {platform}, terminating...");

                bgfxReferences--;

                if(bgfxReferences == 0)
                {
                    bgfx.shutdown();
                }

                windowReferences--;

                if(windowReferences == 0)
                {
                    window.Terminate();
                }

                Environment.Exit(1);

                return;
            }
        }

        var size = window.Size;

        (width, height) = (size.X, size.Y);

        rendererType = bgfx.RendererType.Count;

        init.resolution.width = (uint)width;
        init.resolution.height = (uint)height;
        init.resolution.reset = (uint)resetFlags;

        Log.Info($"[RenderWindow] Initializing rendering: {width}x{height}");

        var ok = false;

        if (renderers != null)
        {
            Log.Info($"[RenderWindow] Attempting to find the right renderer");

            foreach (var renderer in renderers)
            {
                switch (renderer)
                {
                    case RendererType.Direct3D11:

                        rendererType = bgfx.RendererType.Direct3D11;

                        break;

#if STAPLE_SUPPORTS_D3D12
                    case RendererType.Direct3D12:

                        rendererType = bgfx.RendererType.Direct3D12;

                        break;
#endif

                    case RendererType.OpenGL:

                        rendererType = bgfx.RendererType.OpenGL;

                        break;

                    case RendererType.OpenGLES:

                        rendererType = bgfx.RendererType.OpenGLES;

                        break;

                    case RendererType.Metal:

                        rendererType = bgfx.RendererType.Metal;

                        break;

                    case RendererType.Vulkan:

                        rendererType = bgfx.RendererType.Vulkan;

                        break;
                }

                init.type = rendererType;

                Log.Info($"[RenderWindow] Trying {renderer}");

                unsafe
                {
                    fixed(bgfx.Init *i = &init)
                    {
                        ok = bgfx.init(i);
                    }

                    if (ok)
                    {
                        Log.Info($"[RenderWindow] {renderer} OK!");

                        var capabilities = (bgfx.CapsFlags)init.capabilities;

                        if(capabilities.HasFlag(bgfx.CapsFlags.Compute) == false)
                        {
                            Log.Error($"[RenderWindow] Device doesn't have the required features to continue, ignoring...");

                            ok = false;

                            bgfx.shutdown();
                        }

                        if(ok)
                        {
                            CurrentRenderer = renderer;

                            break;
                        }
                    }
                }
            }

            if (ok == false)
            {
                Log.Error($"[RenderWindow] Failed to find a working renderer, terminating...");

                bgfxReferences--;

                if (bgfxReferences == 0)
                {
                    bgfx.shutdown();
                }

                windowReferences--;

                if(windowReferences == 0)
                {
                    window.Terminate();
                }

                Environment.Exit(1);

                return;
            }
        }

        bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0x334455FF, 1, 0);
        bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);

        switch(AppSettings.Current?.profilingMode ?? AppProfilingMode.None)
        {
            case AppProfilingMode.None:
            case AppProfilingMode.Profiler:

                bgfx.set_debug((uint)bgfx.DebugFlags.Text);

                break;

            case AppProfilingMode.RenderStats:

                bgfx.set_debug((uint)(bgfx.DebugFlags.Text | bgfx.DebugFlags.Stats));

                break;
        }
    }

    internal void CheckEvents()
    {
        var appEvent = AppEventQueue.instance.Next();

        if (appEvent != null)
        {
            switch (appEvent.type)
            {
                case AppEventType.ResetFlags:

                    bgfx.reset((uint)width, (uint)height, (uint)appEvent.reset.resetFlags, bgfx.TextureFormat.RGBA8);
                    bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);

                    break;

                case AppEventType.MouseUp:
                case AppEventType.MouseDown:

                    Input.HandleMouseButtonEvent(appEvent);

                    break;

                case AppEventType.MouseDelta:

                    Input.HandleMouseDeltaEvent(appEvent);

                    break;

                case AppEventType.KeyUp:
                case AppEventType.KeyDown:

                    Input.HandleKeyEvent(appEvent);

                    break;

                case AppEventType.Text:

                    Input.HandleTextEvent(appEvent);

                    break;

                case AppEventType.MaximizeWindow:

                    try
                    {
                        OnScreenSizeChange?.Invoke(hasFocus);
                    }
                    catch(Exception)
                    {
                    }

                    break;

                case AppEventType.MoveWindow:

                    try
                    {
                        OnMove?.Invoke(window.Position);
                    }
                    catch (Exception)
                    {
                    }

                    break;

                case AppEventType.Touch:

                    Input.HandleTouchEvent(appEvent);

                    break;
            }
        }
    }

    internal void RenderFrame(ref DateTime lastTime)
    {
        var current = DateTime.UtcNow;

        if (Paused)
        {
            lastTime = current;

            return;
        }

        Time.UpdateClock(current, lastTime);

        lastTime = current;

        World.Current?.StartFrame();

        StapleHooks.ExecuteHooks(StapleHookEvent.FrameBegin, null);

        try
        {
            OnUpdate?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"RenderWindow Render Exception: {e}");
        }

        StapleHooks.ExecuteHooks(StapleHookEvent.FrameEnd, null);

        var hasCamera = Scene.SortedCameras.Length != 0;

        if (hasCamera == false)
        {
            bgfx.touch(ClearView);
        }

        var frame = bgfx.frame(false);

        RenderSystem.Instance.OnFrame(frame);

        PerformanceProfilerSystem.FinishFrame();

        bgfx.dbg_text_clear(0, false);

        switch(AppSettings.Current?.profilingMode ?? AppProfilingMode.None)
        {
            case AppProfilingMode.Profiler:

                {
                    var counters = PerformanceProfilerSystem.AverageFrameCounters
                        .OrderByDescending(x => x.Value)
                        .ToArray();

                    for (var i = 0; i < counters.Length; i++)
                    {
                        var y = i;
                        var counter = counters[i];

                        byte attr = 0x8a;

                        if (counter.Value >= 16)
                        {
                            attr = 0x8c;
                        }

                        bgfx.dbg_text_printf(2, (ushort)y, attr, $"{counter.Key} - {counter.Value}ms", "");
                    }
                }

                break;
        }
    }

    private void RenderThread()
    {
        InitBGFX();

        lock (renderLock)
        {
            renderThreadReady = true;
        }

        var last = DateTime.UtcNow;

        for (; ; )
        {
            bool renderNow = false;

            lock (renderLock)
            {
                if (shouldStop)
                {
                    break;
                }

                renderNow = shouldRender;
            }

            CheckEvents();

            if (renderNow == false)
            {
                Thread.Sleep(100);

                continue;
            }

            RenderFrame(ref last);
        }

        Cleanup();
    }

    /// <summary>
    /// Changes the window's resolution and window mode
    /// </summary>
    /// <param name="width">The new screen width</param>
    /// <param name="height">The new screen height</param>
    /// <param name="windowMode">The new window mode</param>
    /// <returns>Whether it was set</returns>
    public bool SetResolution(int width, int height, WindowMode windowMode)
    {
        return window.SetResolution(width, height, windowMode);
    }

    /// <summary>
    /// Sets the window's icon
    /// </summary>
    /// <param name="icon">Raw image data for the icon</param>
    public void SetIcon(RawTextureData icon)
    {
        window.SetIcon(icon);
    }

    /// <summary>
    /// Creates a render window
    /// </summary>
    /// <param name="width">The window's width</param>
    /// <param name="height">The window's height</param>
    /// <param name="resizable">Whether it should be resizable</param>
    /// <param name="windowMode">The window mode</param>
    /// <param name="maximized">Whether the window should be maximized</param>
    /// <param name="position">A specific position for the window, or null for default</param>
    /// <param name="monitorIndex">The monitor index to use</param>
    /// <param name="resetFlags">The starting reset flags</param>
    /// <returns>The window, or null</returns>
    public static RenderWindow Create(int width, int height, bool resizable, WindowMode windowMode,
        Vector2Int? position, bool maximized, int monitorIndex, bgfx.ResetFlags resetFlags)
    {
        var resizableString = resizable ? "Resizable" : "Not resizable";
        var maximizedString = maximized ? "Maximized" : "Normal";
        var positionString = position.HasValue ? position.Value.ToString() : "(default)";

        Log.Info($"[RenderWindow] Creating {windowMode} window {AppSettings.Current.appName} with size {width}x{height} at {positionString} " +
            $"({resizableString}, {maximizedString}) for monitor {monitorIndex}");

        if (windowReferences > 0)
        {
            Log.Error($"[RenderWindow] Multiple windows are not supported!");

            return null;
        }

        var renderWindow = new RenderWindow()
        {
            resetFlags = resetFlags,
        };

        renderWindow.window = Platform.platformProvider.CreateWindow();

        if(renderWindow.window == null)
        {
            Log.Error($"[RenderWindow] Missing render window implementation!");

            return null;
        }

        if (windowReferences == 0)
        {
            renderWindow.window.Init();
        }

        windowReferences++;

        renderWindow.resetFlags = resetFlags;

        var originalWidth = width;
        var originalHeight = height;

        if (renderWindow.window.Create(ref width, ref height, AppSettings.Current.appName, resizable, windowMode, position, maximized, monitorIndex) == false)
        {
            Log.Error($"[RenderWindow] Failed to create {windowMode} window \"{AppSettings.Current.appName}\" with size {originalWidth}x{originalHeight}");

            windowReferences--;

            if(windowReferences == 0)
            {
                renderWindow.window.Terminate();
            }

            return null;
        }

        Input.window = renderWindow.window;
        Cursor.window = renderWindow.window;
        Screen.RefreshRate = renderWindow.window.RefreshRate;

        //Issue with Metal
        if(Platform.IsMacOS)
        {
            AppSettings.Current.multiThreadedRenderer = false;
        }

#if !ANDROID
        bgfxReferences++;

        if (AppSettings.Current.multiThreadedRenderer == false)
        {
            renderWindow.InitBGFX();
        }
#endif

        Screen.Width = width;
        Screen.Height = height;

        return renderWindow;
    }
}
