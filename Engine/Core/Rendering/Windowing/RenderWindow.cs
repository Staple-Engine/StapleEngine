using Bgfx;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("StapleEditor")]

namespace Staple.Internal
{
    /// <summary>
    /// Window for rendering
    /// </summary>
    internal class RenderWindow
    {
        public const int ClearView = 0;

        //In case we have more than one window in the future
        internal static int windowReferences = 0;
        internal static int bgfxReferences = 0;

        public int screenWidth = 0;
        public int screenHeight = 0;
        public bool hasFocus = true;
        public IRenderWindow window;
        public bgfx.RendererType rendererType;
        public bgfx.ResetFlags resetFlags;
        public Action OnFixedUpdate;
        public Action OnUpdate;
        public Action OnInit;
        public Action OnCleanup;
        public Action<bool> OnScreenSizeChange;
        public AppSettings appSettings;
        public bool shouldStop = false;
        public bool shouldRender = true;
        public object renderLock = new();
        private bool renderThreadReady = false;
        private Thread renderThread;
        private bgfx.Init init = new bgfx.Init();
        private AppPlatform currentPlatform;

        public static RendererType CurrentRenderer;

        /// <summary>
        /// Runs the window main loop
        /// </summary>
        public void Run()
        {
            if (appSettings.multiThreadedRenderer)
            {
                MultiThreadedLoop();
            }
            else
            {
                SingleThreadLoop();
            }
        }

        private void CheckContextLost()
        {
            if (window.ContextLost)
            {
                window.ContextLost = false;

                ResourceManager.instance.Destroy();

                bgfx.shutdown();

                InitBGFX();

                ResourceManager.instance.RecreateResources();
            }
        }

        private void SingleThreadLoop()
        {
            OnScreenSizeChange?.Invoke(window.IsFocused);

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
                Input.Character = 0;
                Input.MouseDelta = Vector2.Zero;
                Input.MouseRelativePosition = Vector2.Zero;

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

                if ((currentW != screenWidth || currentH != screenHeight) && window.ShouldClose == false)
                {
                    screenWidth = currentW;
                    screenHeight = currentH;

                    try
                    {
                        OnScreenSizeChange?.Invoke(hasFocus);
                    }
                    catch (Exception)
                    {
                    }
                }

                CheckContextLost();

                if (appSettings.runInBackground == false && window.IsFocused != hasFocus)
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

                var current = DateTime.Now;

                fixedTimer += (float)(current - last).TotalSeconds;

                //Prevent hard stuck
                var tries = 0;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && tries < 3)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    OnFixedUpdate?.Invoke();

                    tries++;
                }

                RenderFrame(ref last);
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
                Input.Character = 0;
                Input.MouseDelta = Vector2.Zero;
                Input.MouseRelativePosition = Vector2.Zero;

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

                if ((currentW != screenWidth || currentH != screenHeight) && window.ShouldClose == false)
                {
                    screenWidth = currentW;
                    screenHeight = currentH;

                    try
                    {
                        OnScreenSizeChange?.Invoke(hasFocus);
                    }
                    catch (Exception)
                    {
                    }
                }

                CheckContextLost();

                if (appSettings.runInBackground == false && window.IsFocused != hasFocus)
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

                var current = DateTime.Now;

                fixedTimer += (float)(current - last).TotalSeconds;

                //Prevent hard stuck
                var tries = 0;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && tries < 3)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    OnFixedUpdate?.Invoke();

                    tries++;
                }

                last = DateTime.Now;
            }

            try
            {
                OnCleanup?.Invoke();
            }
            catch (Exception)
            {
            }

            if (renderThread != null)
            {
                lock (renderLock)
                {
                    shouldStop = true;
                }

                for (; ; )
                {
                    if (renderThread.IsAlive == false)
                    {
                        break;
                    }

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
                    window.Terminate();
                }
            }
        }

        private void InitBGFX()
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

            window.GetWindowSize(out screenWidth, out screenHeight);

            rendererType = bgfx.RendererType.Count;

            init.resolution.width = (uint)screenWidth;
            init.resolution.height = (uint)screenHeight;
            init.resolution.reset = (uint)resetFlags;

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
            bgfx.set_debug((uint)bgfx.DebugFlags.Text);
#endif
        }

        private void CheckEvents()
        {
            var appEvent = AppEventQueue.instance.Next();

            if (appEvent != null)
            {
                switch (appEvent.type)
                {
                    case AppEventType.ResetFlags:

                        bgfx.reset((uint)screenWidth, (uint)screenHeight, (uint)appEvent.resetFlags, bgfx.TextureFormat.RGBA8);
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
                }
            }
        }

        private void RenderFrame(ref DateTime lastTime)
        {
            var current = DateTime.Now;

            Time.UpdateClock(current, lastTime);

            lastTime = current;

            try
            {
                OnUpdate?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"RenderWindow Render Exception: {e}");
            }

            var hasCamera = (Scene.current?.world.CountEntities<Camera>() ?? 0) != 0;

            if (hasCamera == false)
            {
                bgfx.touch(ClearView);
                bgfx.dbg_text_clear(0, false);
                bgfx.dbg_text_printf(40, 20, 1, "No cameras are Rendering", "");
            }

            bgfx.touch(ClearView);
            bgfx.dbg_text_clear(0, false);
            bgfx.dbg_text_printf(0, 0, 1, $"FPS: {Time.FPS}", "");

            _ = bgfx.frame(false);
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

        /// <summary>
        /// Creates a render window
        /// </summary>
        /// <param name="width">The window's width</param>
        /// <param name="height">The window's height</param>
        /// <param name="resizable">Whether it should be resizable</param>
        /// <param name="windowMode">The window mode</param>
        /// <param name="appSettings">Application Settings</param>
        /// <param name="monitorIndex">The monitor index to use</param>
        /// <param name="resetFlags">The starting reset flags</param>
        /// <returns>The window, or null</returns>
        public static RenderWindow Create(int width, int height, bool resizable, WindowMode windowMode,
            AppSettings appSettings, int monitorIndex, bgfx.ResetFlags resetFlags)
        {
            var resizableString = resizable ? "Resizable" : "Not resizable";

            Log.Info($"[RenderWindow] Creating {windowMode} window {appSettings.appName} with size {width}x{height} ({resizableString}) for monitor {monitorIndex}");

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
#else
            if(Platform.IsWindows || Platform.IsLinux || Platform.IsMacOS)
            {
                renderWindow.window = new GLFWRenderWindow();
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

            if (renderWindow.window.Create(ref width, ref height, appSettings.appName, resizable, windowMode, monitorIndex) == false)
            {
                Log.Error($"[RenderWindow] Failed to create {windowMode} window \"{appSettings.appName}\" with size {originalWidth}x{originalHeight}");

                windowReferences--;

                if(windowReferences == 0)
                {
                    renderWindow.window.Terminate();
                }

                return null;
            }

            bgfxReferences++;

            Input.window = renderWindow.window;

            if (appSettings.multiThreadedRenderer == false)
            {
                renderWindow.InitBGFX();
            }

            AppPlayer.ScreenWidth = width;
            AppPlayer.ScreenHeight = height;

            return renderWindow;
        }
    }
}
