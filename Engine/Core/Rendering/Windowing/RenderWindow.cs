using Bgfx;
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
    public AppSettings appSettings;

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
    private bgfx.Init init = new bgfx.Init();
    private AppPlatform currentPlatform;

    public bool Paused => hasFocus == false && appSettings.runInBackground == false;

    public static RendererType CurrentRenderer { get; internal set; }

    /// <summary>
    /// Runs the window main loop
    /// </summary>
    public void Run()
    {
        ThreadHelper.Initialize();

        if (appSettings.multiThreadedRenderer)
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

            ResourceManager.instance.Destroy(false);

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
        }
        catch (Exception e)
        {
            Log.Error($"RenderWindow Init Exception: {e}");

            return;
        }

        if (shouldStop)
        {
            return;
        }

        var last = DateTime.Now;

        var fixedTimer = 0.0f;

        while (window.ShouldClose == false && shouldStop == false)
        {
            Input.UpdateState();

            window.PollEvents();

            lock (renderLock)
            {
                shouldRender = window.Unavailable == false && (appSettings.runInBackground == true || window.IsFocused == true);
            }

            if (window.Unavailable)
            {
                continue;
            }

            window.GetWindowSize(out var currentW, out var currentH);

            if ((currentW != width || currentH != height) && window.ShouldClose == false)
            {
                width = currentW;
                height = currentH;

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

            CheckEvents();

            if (Paused)
            {
                fixedTimer = 0;
            }
            else
            {
                var current = DateTime.Now;

                fixedTimer += (float)(current - last).TotalSeconds;

                //Prevent hard stuck
                var currentFixedTime = 0.0f;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && currentFixedTime < appSettings.maximumFixedTimestepTime)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    OnFixedUpdate?.Invoke();

                    currentFixedTime += Time.fixedDeltaTime;
                }

                if(currentFixedTime >= appSettings.maximumFixedTimestepTime)
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
        }
        catch (Exception e)
        {
            Log.Error($"Error initializing: {e}");

            shouldStop = true;
        }

        var last = DateTime.Now;

        var fixedTimer = 0.0f;

        while (window.ShouldClose == false && shouldStop == false)
        {
            Input.UpdateState();

            window.PollEvents();

            lock (renderLock)
            {
                shouldRender = window.Unavailable == false && (appSettings.runInBackground == true || window.IsFocused == true);
            }

            if (window.Unavailable)
            {
                continue;
            }

            window.GetWindowSize(out var currentW, out var currentH);

            if ((currentW != width || currentH != height) && window.ShouldClose == false)
            {
                width = currentW;
                height = currentH;

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
            }
            else
            {
                var current = DateTime.Now;

                fixedTimer += (float)(current - last).TotalSeconds;

                //Prevent hard stuck
                var currentFixedTime = 0.0f;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && currentFixedTime < appSettings.maximumFixedTimestepTime)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    OnFixedUpdate?.Invoke();

                    currentFixedTime += Time.fixedDeltaTime;
                }

                if (currentFixedTime >= appSettings.maximumFixedTimestepTime)
                {
                    fixedTimer = 0;
                }
            }

            last = DateTime.Now;

            ThreadHelper.Update();
        }

        lock(renderLock)
        {
            try
            {
                OnCleanup?.Invoke();
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

            init.platformData.ndt = window.MonitorPointer(currentPlatform).ToPointer();
            init.platformData.nwh = window.WindowPointer(currentPlatform).ToPointer();

            Log.Debug($"[RenderWindow] platformData ndt: {(nint)init.platformData.ndt} nwh {(nint)init.platformData.nwh}");

            if (appSettings.renderers.TryGetValue(currentPlatform, out renderers) == false)
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

        window.GetWindowSize(out width, out height);

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

                    case RendererType.Direct3D12:

                        rendererType = bgfx.RendererType.Direct3D12;

                        break;

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

                        CurrentRenderer = renderer;

                        break;
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

#if _DEBUG
        //bgfx.set_debug((uint)(bgfx.DebugFlags.Text | bgfx.DebugFlags.Stats));
        bgfx.set_debug((uint)bgfx.DebugFlags.Text);
#endif
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
        var current = DateTime.Now;

        if (Paused)
        {
            lastTime = current;

            return;
        }

        Time.UpdateClock(current, lastTime);

        lastTime = current;

        World.Current?.StartFrame();

        try
        {
            OnUpdate?.Invoke();
        }
        catch (Exception e)
        {
            Log.Error($"RenderWindow Render Exception: {e}");
        }

        var hasCamera = Scene.CountEntities<Camera>() != 0;

        if (hasCamera == false)
        {
            bgfx.touch(ClearView);
        }

        var frame = bgfx.frame(false);

        RenderSystem.Instance.OnFrame(frame);

        bgfx.dbg_text_clear(0, false);

        if(hasCamera == false)
        {
            bgfx.dbg_text_printf(40, 20, 1, "No cameras are Rendering", "");
        }

        bgfx.dbg_text_printf(0, 0, 1, $"FPS: {Time.FPS}", "");
    }

    private void RenderThread()
    {
        InitBGFX();

        lock (renderLock)
        {
            renderThreadReady = true;
        }

        var last = DateTime.Now;

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

    public bool SetWindowMode(WindowMode windowMode)
    {
        return window.SetWindowMode(windowMode);
    }

    /// <summary>
    /// Creates a render window
    /// </summary>
    /// <param name="width">The window's width</param>
    /// <param name="height">The window's height</param>
    /// <param name="resizable">Whether it should be resizable</param>
    /// <param name="windowMode">The window mode</param>
    /// <param name="appSettings">Application Settings</param>
    /// <param name="maximized">Whether the window should be maximized</param>
    /// <param name="position">A specific position for the window, or null for default</param>
    /// <param name="monitorIndex">The monitor index to use</param>
    /// <param name="resetFlags">The starting reset flags</param>
    /// <returns>The window, or null</returns>
    public static RenderWindow Create(int width, int height, bool resizable, WindowMode windowMode,
        AppSettings appSettings, Vector2Int? position, bool maximized, int monitorIndex, bgfx.ResetFlags resetFlags)
    {
        var resizableString = resizable ? "Resizable" : "Not resizable";
        var maximizedString = maximized ? "Maximized" : "Normal";
        var positionString = position.HasValue ? position.Value.ToString() : "(default)";

        Log.Info($"[RenderWindow] Creating {windowMode} window {appSettings.appName} with size {width}x{height} at {positionString} " +
            $"({resizableString}, {maximizedString}) for monitor {monitorIndex}");

        if (windowReferences > 0)
        {
            Log.Error($"[RenderWindow] Multiple windows are not supported!");

            return null;
        }

        var renderWindow = new RenderWindow()
        {
            appSettings = appSettings,
            resetFlags = resetFlags,
        };

#if ANDROID
        renderWindow.window = AndroidRenderWindow.Instance;
#elif IOS
        renderWindow.window = iOSRenderWindow.Instance;
#else
        if(Platform.IsWindows || Platform.IsLinux || Platform.IsMacOS)
        {
            renderWindow.window = new SDL2RenderWindow();
        }
#endif

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

        if (renderWindow.window.Create(ref width, ref height, appSettings.appName, resizable, windowMode, position, maximized, monitorIndex) == false)
        {
            Log.Error($"[RenderWindow] Failed to create {windowMode} window \"{appSettings.appName}\" with size {originalWidth}x{originalHeight}");

            windowReferences--;

            if(windowReferences == 0)
            {
                renderWindow.window.Terminate();
            }

            return null;
        }

        Input.window = renderWindow.window;

        //Issue with Metal
        if(Platform.IsMacOS)
        {
            appSettings.multiThreadedRenderer = false;
        }

#if !ANDROID
        bgfxReferences++;

        if (appSettings.multiThreadedRenderer == false)
        {
            renderWindow.InitBGFX();
        }
#endif

        Screen.Width = width;
        Screen.Height = height;

        return renderWindow;
    }
}
