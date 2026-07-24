using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple.Internal;

/// <summary>
/// Window for rendering
/// </summary>
internal class RenderWindow
{
    public const int ClearView = 0;

    internal const float TimeBetweenGCChecks = 30.0f;

    internal static readonly string LogTag = "RenderWindow";

    //In case we have more than one window in the future
    internal static int windowReferences = 0;
    internal static int rendererReferences = 0;

    internal int width = 0;
    internal int height = 0;
    internal bool hasFocus = true;
    internal IRenderWindow window;
    internal RenderModeFlags renderFlags;
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
    private AppPlatform currentPlatform;
    private CursorLockMode lastCursorLockMode;
    private uint frameCounter = 0;

    public bool Paused => !hasFocus && !AppSettings.Active.runInBackground;

    public static RendererType CurrentRenderer { get; internal set; }

    /// <summary>
    /// Runs the window main loop
    /// </summary>
    public void Run()
    {
        ThreadHelper.Initialize();

        if (AppSettings.Active.multiThreadedRenderer)
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

            RenderSystem.Backend.Destroy();

            InitializeRenderer();

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
            Log.Error($"RenderWindow Init Exception: {e}", LogTag);

            shouldStop = true;
        }

        var last = DateTime.UtcNow;

        var fixedTimer = 0.0f;
        var GCTimer = 0.0f;

        while (!window.ShouldClose && !shouldStop)
        {
            PerformanceProfilerSystem.StartFrame();

            Input.UpdateState();

            window.PollEvents();

            CheckEvents();

            lock (renderLock)
            {
                shouldRender = !window.Unavailable && (AppSettings.Active.runInBackground || window.IsFocused);
            }

            if (window.Unavailable)
            {
                continue;
            }

            if(!Paused)
            {
                RenderSystem.Backend.BeginFrame();
            }

            var size = window.Size;

            if ((size.X != width || size.Y!= height) && !window.ShouldClose)
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
                GCTimer += (float)(current - last).TotalSeconds;

                if(GCTimer >= TimeBetweenGCChecks)
                {
                    GCTimer -= TimeBetweenGCChecks;

                    MemoryUtils.GarbageCollect(false);
                }

                //Prevent hard stuck
                var currentFixedTime = 0.0f;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && currentFixedTime < AppSettings.Active.maximumFixedTimestepTime)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    OnFixedUpdate?.Invoke();

                    StapleHooks.ExecuteHooks(StapleHookEvent.FixedUpdate, null);

                    currentFixedTime += Time.fixedDeltaTime;
                }

                if(currentFixedTime >= AppSettings.Active.maximumFixedTimestepTime)
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
                if (!renderThread.IsAlive)
                {
                    if (renderThreadReady)
                    {
                        if (rendererReferences > 0)
                        {
                            rendererReferences--;

                            if (rendererReferences == 0)
                            {
                                RenderSystem.Backend.Destroy();
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
            Log.Error($"Error initializing: {e}", LogTag);

            shouldStop = true;
        }

        var last = DateTime.UtcNow;

        var fixedTimer = 0.0f;
        var GCTimer = 0.0f;

        while (!window.ShouldClose && !shouldStop)
        {
            PerformanceProfilerSystem.StartFrame();

            Input.UpdateState();

            window.PollEvents();

            lock (renderLock)
            {
                shouldRender = !window.Unavailable && (AppSettings.Active.runInBackground || window.IsFocused);
            }

            if (window.Unavailable)
            {
                RenderSystem.Backend.EndFrame();

                continue;
            }

            var size = window.Size;

            if ((size.X != width || size.Y != height) && !window.ShouldClose)
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
                GCTimer += (float)(current - last).TotalSeconds;

                if (GCTimer >= TimeBetweenGCChecks)
                {
                    GCTimer -= TimeBetweenGCChecks;

                    MemoryUtils.GarbageCollect(false);
                }

                //Prevent hard stuck
                var currentFixedTime = 0.0f;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && currentFixedTime < AppSettings.Active.maximumFixedTimestepTime)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    OnFixedUpdate?.Invoke();

                    StapleHooks.ExecuteHooks(StapleHookEvent.FixedUpdate, null);

                    currentFixedTime += Time.fixedDeltaTime;
                }

                if (currentFixedTime >= AppSettings.Active.maximumFixedTimestepTime)
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
                if (!renderThread.IsAlive)
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
        if (rendererReferences > 0)
        {
            rendererReferences--;

            if (rendererReferences == 0)
            {
                RenderSystem.Backend.Destroy();
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

    internal void InitializeRenderer()
    {
        var renderers = new List<RendererType>();

        var platform = Platform.CurrentPlatform;

        if (!platform.HasValue)
        {
            Log.Error("Unsupported platform", LogTag);

            rendererReferences--;

            if(rendererReferences == 0)
            {
                RenderSystem.Backend.Destroy();
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

        if (!AppSettings.Active.renderers.TryGetValue(currentPlatform, out renderers))
        {
            Log.Error($"No Renderers found for platform {platform}, terminating...", LogTag);

            rendererReferences--;

            if(rendererReferences == 0)
            {
                RenderSystem.Backend.Destroy();
            }

            windowReferences--;

            if(windowReferences == 0)
            {
                window.Terminate();
            }

            Environment.Exit(1);

            return;
        }

        var size = window.Size;

        (width, height) = (size.X, size.Y);

        Log.Info($"Initializing rendering: {width}x{height}", LogTag);

        var ok = false;

        if (renderers != null)
        {
            Log.Info($"Attempting to find the right renderer", LogTag);

#if _DEBUG
            bool debug = true;
#else
            bool debug = false;
#endif

            foreach (var renderer in renderers)
            {
                Log.Info($"Trying {renderer}", LogTag);

                unsafe
                {
                    ok = RenderSystem.Backend.Initialize(renderer, debug, window, renderFlags);

                    if (ok)
                    {
                        Log.Info($"{renderer} OK!", LogTag);

                        CurrentRenderer = renderer;

                        break;
                    }
                }
            }

            if (!ok)
            {
                Log.Error($"Failed to find a working renderer, terminating...", LogTag);

                rendererReferences--;

                if (rendererReferences == 0)
                {
                    RenderSystem.Backend.Destroy();
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
    }

    internal void CheckEvents()
    {
        for(; ; )
        {
            var appEvent = AppEventQueue.instance.Next();

            if (appEvent == null)
            {
                break;
            }

            switch (appEvent.type)
            {
                case AppEventType.ResetFlags:

                    RenderSystem.Backend.UpdateRenderMode(appEvent.reset.flags);
                    RenderSystem.Backend.UpdateViewport(width, height);

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

                    RenderSystem.Backend.UpdateViewport(width, height);

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

        RenderSystem.Instance.OnStartFrame();

        StapleHooks.ExecuteHooks(StapleHookEvent.FrameBegin, null);

        try
        {
            OnUpdate?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"Render Exception: {e}", LogTag);
        }

        StapleHooks.ExecuteHooks(StapleHookEvent.FrameEnd, null);

        frameCounter++;

        RenderSystem.Instance.OnEndFrame(frameCounter);

        RenderSystem.Backend.EndFrame();

        PerformanceProfilerSystem.FinishFrame();
    }

    private void RenderThread()
    {
        InitializeRenderer();

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

            if (!renderNow)
            {
                Thread.Sleep(100);

                continue;
            }

            if(!Paused)
            {
                RenderSystem.Backend.BeginFrame();
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
    /// Shows text input UI. Call when you need the user to write something.
    /// </summary>
    /// <remarks>May not show anything depending on platform.</remarks>
    public void ShowTextInput()
    {
        window.ShowTextInput();
    }

    /// <summary>
    /// Hides text input UI.
    /// </summary>
    public void HideTextInput()
    {
        window.HideTextInput();
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
    /// <param name="renderFlags">The starting render flags</param>
    /// <returns>The window, or null</returns>
    public static RenderWindow Create(int width, int height, bool resizable, WindowMode windowMode,
        Vector2Int? position, bool maximized, int monitorIndex, RenderModeFlags renderFlags)
    {
        var resizableString = resizable ? "Resizable" : "Not resizable";
        var maximizedString = maximized ? "Maximized" : "Normal";
        var positionString = position.HasValue ? position.Value.ToString() : "(default)";

        Log.Info($"Creating {windowMode} window {AppSettings.Active.appName} with size {width}x{height} at {positionString} " +
            $"({resizableString}, {maximizedString}) for monitor {monitorIndex}", LogTag);

        if (windowReferences > 0)
        {
            Log.Error($"Multiple windows are not supported!", LogTag);

            return null;
        }

        var renderWindow = new RenderWindow()
        {
            renderFlags = renderFlags,
            window = Platform.platformProvider.CreateWindow(),
        };

        if (renderWindow.window == null)
        {
            Log.Error($"Missing render window implementation!", LogTag);

            return null;
        }

        if (windowReferences == 0)
        {
            renderWindow.window.Init();
        }

        windowReferences++;

        var originalWidth = width;
        var originalHeight = height;

        if (!renderWindow.window.Create(ref width, ref height, AppSettings.Active.appName, resizable, windowMode, position, maximized,
            monitorIndex))
        {
            Log.Error($"Failed to create {windowMode} window \"{AppSettings.Active.appName}\" with size {originalWidth}x{originalHeight}", LogTag);

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

#if !ANDROID
        rendererReferences++;

        if (!AppSettings.Active.multiThreadedRenderer)
        {
            renderWindow.InitializeRenderer();
        }
#endif

        Screen.Width = width;
        Screen.Height = height;

        return renderWindow;
    }
}
