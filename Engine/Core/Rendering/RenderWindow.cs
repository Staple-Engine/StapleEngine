using Bgfx;
using GLFW;
using Staple.Internal;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

[assembly: InternalsVisibleTo("StapleEditor")]

namespace Staple
{
    internal class RenderWindow
    {
        public const int ClearView = 0;

        //In case we have more than one window in the future
        internal static int glfwReferences = 0;
        internal static int bgfxReferences = 0;

        public int screenWidth = 0;
        public int screenHeight = 0;
        public bool hasFocus = true;
        public NativeWindow window;
        public bgfx.RendererType rendererType;
        public bgfx.ResetFlags resetFlags;
        public Action OnUpdate;
        public Action OnFixedUpdate;
        public Action OnRender;
        public Action OnInit;
        public Action OnCleanup;
        public Action<bool> OnScreenSizeChange;
        public AppSettings appSettings;
        public bool shouldStop = false;
        public bool shouldRender = true;
        public object renderLock = new object();
        private bool renderThreadReady = false;
        private Thread renderThread;

        public void Run()
        {
            if(appSettings.multiThreadedRenderer)
            {
                MultiThreadedLoop();
            }
            else
            {
                SingleThreadLoop();
            }
        }

        private void SingleThreadLoop()
        {
            OnScreenSizeChange?.Invoke(window.IsFocused);

            try
            {
                OnInit?.Invoke();
            }
            catch (System.Exception e)
            {
                Log.Error($"RenderWindow Init Exception: {e}");

                return;
            }

            if (shouldStop)
            {
                return;
            }

            double last = Glfw.Time;

            var fixedTimer = 0.0f;

            while (Glfw.WindowShouldClose(window) == false && window.IsClosed == false && shouldStop == false)
            {
                Input.Character = 0;
                Input.MouseDelta = Vector2.Zero;

                Glfw.PollEvents();

                lock (renderLock)
                {
                    shouldRender = appSettings.runInBackground == true || window.IsFocused == true;
                }

                Glfw.GetFramebufferSize(window, out var currentW, out var currentH);

                if ((currentW != screenWidth || currentH != screenHeight) && window.IsClosed == false)
                {
                    screenWidth = currentW;
                    screenHeight = currentH;

                    try
                    {
                        OnScreenSizeChange?.Invoke(hasFocus);
                    }
                    catch (System.Exception)
                    {
                    }
                }

                if (appSettings.runInBackground == false && window.IsFocused != hasFocus)
                {
                    hasFocus = window.IsFocused;

                    try
                    {
                        OnScreenSizeChange?.Invoke(hasFocus);
                    }
                    catch (System.Exception)
                    {
                    }

                    if (hasFocus == false)
                    {
                        continue;
                    }
                }

                CheckEvents();

                try
                {
                    OnUpdate?.Invoke();
                }
                catch (System.Exception)
                {
                }

                double current = Glfw.Time;

                fixedTimer += (float)(current - last);

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
            catch (System.Exception)
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

                            if (glfwReferences > 0)
                            {
                                glfwReferences--;

                                if (glfwReferences == 0)
                                {
                                    Glfw.Terminate();
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
            catch (System.Exception)
            {
            }

            double last = Glfw.Time;

            var fixedTimer = 0.0f;

            while (Glfw.WindowShouldClose(window) == false && window.IsClosed == false && shouldStop == false)
            {
                Input.Character = 0;
                Input.MouseDelta = Vector2.Zero;

                Glfw.PollEvents();

                lock (renderLock)
                {
                    shouldRender = appSettings.runInBackground == true || window.IsFocused == true;
                }

                Glfw.GetFramebufferSize(window, out var currentW, out var currentH);

                if ((currentW != screenWidth || currentH != screenHeight) && window.IsClosed == false)
                {
                    screenWidth = currentW;
                    screenHeight = currentH;

                    try
                    {
                        OnScreenSizeChange?.Invoke(hasFocus);
                    }
                    catch (System.Exception)
                    {
                    }
                }

                if (appSettings.runInBackground == false && window.IsFocused != hasFocus)
                {
                    hasFocus = window.IsFocused;

                    try
                    {
                        OnScreenSizeChange?.Invoke(hasFocus);
                    }
                    catch (System.Exception)
                    {
                    }

                    if (hasFocus == false)
                    {
                        continue;
                    }
                }

                try
                {
                    OnUpdate?.Invoke();
                }
                catch (System.Exception)
                {
                }

                double current = Glfw.Time;

                fixedTimer += (float)(current - last);

                //Prevent hard stuck
                var tries = 0;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && tries < 3)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    OnFixedUpdate?.Invoke();

                    tries++;
                }

                last = Glfw.Time;
            }

            try
            {
                OnCleanup?.Invoke();
            }
            catch (System.Exception)
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


        public void Cleanup()
        {
            if(bgfxReferences > 0)
            {
                bgfxReferences--;

                if(bgfxReferences == 0)
                {
                    bgfx.shutdown();
                }
            }

            if (glfwReferences > 0)
            {
                glfwReferences--;

                if(glfwReferences == 0)
                {
                    Glfw.Terminate();
                }
            }
        }

        private void InitBGFX()
        {
            var init = new bgfx.Init();
            var activeRendererType = RendererType.OpenGL;

            unsafe
            {
                bgfx.init_ctor(&init);

                init.platformData.ndt = null;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    init.platformData.nwh = Native.GetWin32Window(window).ToPointer();

                    if (appSettings.renderers.TryGetValue(AppPlatform.Windows, out var type))
                    {
                        activeRendererType = type;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var display = Native.GetX11Display();
                    var windowHandle = Native.GetX11Window(window);

                    if (display == IntPtr.Zero || window == IntPtr.Zero)
                    {
                        display = Native.GetWaylandDisplay();
                        windowHandle = Native.GetWaylandWindow(window);
                    }

                    init.platformData.ndt = display.ToPointer();
                    init.platformData.nwh = windowHandle.ToPointer();

                    if (appSettings.renderers.TryGetValue(AppPlatform.Linux, out var type))
                    {
                        activeRendererType = type;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    init.platformData.ndt = (void*)Native.GetCocoaMonitor(window.Monitor);
                    init.platformData.nwh = Native.GetCocoaWindow(window).ToPointer();

                    if (appSettings.renderers.TryGetValue(AppPlatform.MacOSX, out var type))
                    {
                        activeRendererType = type;
                    }
                }
            }

            Glfw.GetFramebufferSize(window, out screenWidth, out screenHeight);

            rendererType = bgfx.RendererType.Count;

            switch (activeRendererType)
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
            init.resolution.width = (uint)screenWidth;
            init.resolution.height = (uint)screenHeight;
            init.resolution.reset = (uint)resetFlags;

            unsafe
            {
                if (!bgfx.init(&init))
                {
                    bgfxReferences--;

                    Glfw.Terminate();

                    glfwReferences--;

                    Environment.Exit(1);

                    return;
                }
            }

            bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0x334455FF, 0, 0);
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

        private void RenderFrame(ref double lastTime)
        {
            double current = Glfw.Time;

            Time.UpdateClock(current, lastTime);

            lastTime = current;

            try
            {
                OnRender?.Invoke();
            }
            catch (System.Exception e)
            {
                Log.Error($"RenderWindow Render Exception: {e}");
            }

            var hasCamera = Scene.current.world.CountEntities<Camera>() != 0;

            if (hasCamera == false)
            {
                bgfx.touch(ClearView);
                bgfx.dbg_text_clear(0, false);
                bgfx.dbg_text_printf(40, 20, 1, "No cameras are Rendering", "");
            }

            bgfx.touch(ClearView);
            bgfx.dbg_text_clear(0, false);
            bgfx.dbg_text_printf(0, 0, 1, $"FPS: {Time.FPS}", "");

            bgfx.frame(false);
        }

        private void RenderThread()
        {
            InitBGFX();

            lock (renderLock)
            {
                renderThreadReady = true;
            }

            double last = Glfw.Time;

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

        public static RenderWindow Create(int width, int height, bool resizable, PlayerSettings.WindowMode windowMode,
            AppSettings appSettings, int monitorIndex, bgfx.ResetFlags resetFlags)
        {
            if (glfwReferences > 0)
            {
                return null;
            }

            var renderWindow = new RenderWindow()
            {
                appSettings = appSettings,
                resetFlags = resetFlags,
            };

            if(glfwReferences == 0)
            {
                Glfw.Init();
            }

            glfwReferences++;

            Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
            Glfw.WindowHint(Hint.Resizable, resizable);

            var monitor = Glfw.Monitors.Skip(monitorIndex).FirstOrDefault();

            if (monitor == null)
            {
                monitor = Glfw.PrimaryMonitor;
            }

            renderWindow.resetFlags = resetFlags;

            switch (windowMode)
            {
                case PlayerSettings.WindowMode.Windowed:

                    renderWindow.window = new NativeWindow(width, height, appSettings.appName);

                    break;

                case PlayerSettings.WindowMode.Fullscreen:

                    renderWindow.window = new NativeWindow(width, height, appSettings.appName, monitor, Window.None);

                    break;

                case PlayerSettings.WindowMode.Borderless:

                    Glfw.WindowHint(Hint.Floating, true);
                    Glfw.WindowHint(Hint.Decorated, false);

                    var videoMode = Glfw.GetVideoMode(monitor);

                    renderWindow.window = new NativeWindow(videoMode.Width, videoMode.Height, appSettings.appName);

                    break;
            }

            if (renderWindow.window == null)
            {
                glfwReferences--;

                Glfw.Terminate();

                return null;
            }

            bgfxReferences++;

            Input.window = renderWindow.window;

            Glfw.SetKeyCallback(renderWindow.window, (_, key, scancode, action, mods) =>
            {
                Input.KeyCallback(key, scancode, action, mods);
            });

            Glfw.SetCharCallback(renderWindow.window, (_, codepoint) =>
            {
                Input.CharCallback(codepoint);
            });

            Glfw.SetCursorPositionCallback(renderWindow.window, (_, xpos, ypos) =>
            {
                Input.CursorPosCallback((float)xpos, (float)ypos);
            });

            Glfw.SetMouseButtonCallback(renderWindow.window, (_, button, state, modifiers) =>
            {
                Input.MouseButtonCallback(button, state, modifiers);
            });

            Glfw.SetScrollCallback(renderWindow.window, (_, xOffset, yOffset) =>
            {
                Input.MouseScrollCallback((float)xOffset, (float)yOffset);
            });

            if (Glfw.RawMouseMotionSupported())
            {
                Glfw.SetInputMode(renderWindow.window, InputMode.RawMouseMotion, (int)GLFW.Constants.True);
            }

            if(appSettings.multiThreadedRenderer == false)
            {
                renderWindow.InitBGFX();
            }

            AppPlayer.ScreenWidth = width;
            AppPlayer.ScreenHeight = height;

            return renderWindow;
        }
    }
}
