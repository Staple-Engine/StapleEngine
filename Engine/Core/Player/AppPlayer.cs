using Bgfx;
using GLFW;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Staple
{
    internal class AppPlayer
    {
        public readonly AppSettings appSettings;

        private const ushort ClearView = 0;

        private PlayerSettings playerSettings;

        public static int ScreenWidth { get; private set; }

        public static int ScreenHeight { get; private set; }

        public static bgfx.RendererType ActiveRendererType { get; private set; }

        public static AppPlayer active;

        internal Assembly playerAssembly;

        public AppPlayer(AppSettings settings, string[] args)
        {
            appSettings = settings;
            active = this;

            var baseDirectory = AppContext.BaseDirectory;

#if _DEBUG
            baseDirectory = Environment.CurrentDirectory;
#endif

            ResourceManager.instance.basePath = Path.Combine(baseDirectory, "Data");

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-datadir")
                {
                    if(i + 1 < args.Length)
                    {
                        ResourceManager.instance.basePath = args[i + 1];
                    }
                }
            }
        }

        private bgfx.ResetFlags ResetFlags
        {
            get
            {
                var resetFlags = bgfx.ResetFlags.SrgbBackbuffer;

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.Vsync))
                {
                    resetFlags |= bgfx.ResetFlags.Vsync;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX2))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX2;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX4))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX4;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX8))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX8;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX16))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX16;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.HDR10))
                {
                    resetFlags |= bgfx.ResetFlags.Hdr10;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.HiDpi))
                {
                    resetFlags |= bgfx.ResetFlags.Hidpi;
                }

                return resetFlags;
            }
        }

        public void ResetRendering(bool hasFocus)
        {
            var flags = ResetFlags;

            if(hasFocus == false && appSettings.runInBackground == false)
            {
                flags |= bgfx.ResetFlags.Suspend;
            }

            AppEventQueue.instance.Add(AppEvent.ResetFlags(flags));
        }

        public void Run()
        {
            try
            {
                playerAssembly = Assembly.LoadFrom("Data/Game.dll");
            }
            catch(System.Exception)
            {
                Console.WriteLine($"Error: Failed to load player assembly");

                return;
            }

            playerSettings = new PlayerSettings()
            {
                screenWidth = 1024,
                screenHeight = 768,
            };

            Glfw.Init();

            Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
            Glfw.WindowHint(Hint.Resizable, false);

            NativeWindow window = null;

            var monitor = Glfw.Monitors.Skip(playerSettings.monitorIndex).FirstOrDefault();

            if (monitor == null)
            {
                monitor = Glfw.PrimaryMonitor;
            }

            switch (playerSettings.windowMode)
            {
                case PlayerSettings.WindowMode.Windowed:

                    window = new NativeWindow(playerSettings.screenWidth, playerSettings.screenHeight, appSettings.appName);

                    break;

                case PlayerSettings.WindowMode.Fullscreen:

                    window = new NativeWindow(playerSettings.screenWidth, playerSettings.screenHeight, appSettings.appName, monitor, Window.None);

                    break;

                case PlayerSettings.WindowMode.Borderless:

                    Glfw.WindowHint(Hint.Floating, true);
                    Glfw.WindowHint(Hint.Decorated, false);

                    var videoMode = Glfw.GetVideoMode(monitor);

                    window = new NativeWindow(videoMode.Width, videoMode.Height, appSettings.appName);

                    break;
            }

            if (window == null)
            {
                Glfw.Terminate();

                return;
            }

            bool shouldStop = false;
            bool shouldRender = true;
            bool renderThreadReady = false;
            bool subsystemsReady = false;
            object renderLock = new object();

            void RenderThread()
            {
                var init = new bgfx.Init();
                var rendererType = RendererType.OpenGL;

                unsafe
                {
                    bgfx.init_ctor(&init);

                    init.platformData.ndt = null;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        init.platformData.nwh = Native.GetWin32Window(window).ToPointer();

                        if (appSettings.renderers.TryGetValue(AppPlatform.Windows, out var type))
                        {
                            rendererType = type;
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
                            rendererType = type;
                        }
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        init.platformData.ndt = (void*)Native.GetCocoaMonitor(window.Monitor);
                        init.platformData.nwh = Native.GetCocoaWindow(window).ToPointer();

                        if (appSettings.renderers.TryGetValue(AppPlatform.MacOSX, out var type))
                        {
                            rendererType = type;
                        }
                    }
                }

                Glfw.GetFramebufferSize(window, out playerSettings.screenWidth, out playerSettings.screenHeight);

                ScreenWidth = playerSettings.screenWidth;
                ScreenHeight = playerSettings.screenHeight;

                ActiveRendererType = bgfx.RendererType.Count;

                switch (rendererType)
                {
                    case RendererType.Direct3D11:

                        ActiveRendererType = bgfx.RendererType.Direct3D11;

                        break;

                    case RendererType.Direct3D12:

                        ActiveRendererType = bgfx.RendererType.Direct3D12;

                        break;

                    case RendererType.OpenGL:

                        ActiveRendererType = bgfx.RendererType.OpenGL;

                        break;

                    case RendererType.OpenGLES:

                        ActiveRendererType = bgfx.RendererType.OpenGLES;

                        break;

                    case RendererType.Metal:

                        ActiveRendererType = bgfx.RendererType.Metal;

                        break;

                    case RendererType.Vulkan:

                        ActiveRendererType = bgfx.RendererType.Vulkan;

                        break;
                }

                init.type = ActiveRendererType;
                init.resolution.width = (uint)ScreenWidth;
                init.resolution.height = (uint)ScreenHeight;
                init.resolution.reset = (uint)ResetFlags;

                unsafe
                {
                    if (!bgfx.init(&init))
                    {
                        Glfw.Terminate();

                        Environment.Exit(1);

                        return;
                    }
                }

                bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0x334455FF, 0, 0);
                bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);

#if _DEBUG
                bgfx.set_debug((uint)bgfx.DebugFlags.Text);
#endif

                lock (renderLock)
                {
                    renderThreadReady = true;
                }

                for(; ;)
                {
                    lock(renderLock)
                    {
                        if(subsystemsReady)
                        {
                            break;
                        }
                    }

                    Thread.Sleep(25);
                }

                double last = Glfw.Time;

                for (; ; )
                {
                    bool renderNow = false;

                    lock(renderLock)
                    {
                        if(shouldStop)
                        {
                            Scene.current?.Cleanup();

                            SubsystemManager.instance.Destroy();

                            ResourceManager.instance.Destroy();

                            bgfx.shutdown();

                            return;
                        }

                        renderNow = shouldRender;
                    }

                    var appEvent = AppEventQueue.instance.Next();

                    if(appEvent != null)
                    {
                        switch(appEvent.type)
                        {
                            case AppEventType.ResetFlags:

                                bgfx.reset((uint)ScreenWidth, (uint)ScreenHeight, (uint)appEvent.resetFlags, bgfx.TextureFormat.RGBA8);
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

                    if(shouldRender == false)
                    {
                        Thread.Sleep(100);

                        continue;
                    }

                    double current = Glfw.Time;

                    Time.UpdateClock(current, last);

                    last = current;

                    SubsystemManager.instance.Update();

                    var hasCamera = Scene.current.GetComponents<Camera>().ToArray().Length != 0;

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
            }

            var renderThread = new Thread(new ThreadStart(RenderThread));

            renderThread.Start();

            for(; ; )
            {
                lock(renderLock)
                {
                    if(renderThread.IsAlive == false)
                    {
                        if(renderThreadReady)
                        {
                            bgfx.shutdown();
                        }

                        Glfw.Terminate();

                        Environment.Exit(1);

                        return;
                    }

                    if(renderThreadReady)
                    {
                        break;
                    }
                }

                Thread.Sleep(25);
            }

            bool hasFocus = window.IsFocused;

            Scene.sceneList = ResourceManager.instance.LoadSceneList();

            if(Scene.sceneList == null || Scene.sceneList.Count == 0)
            {
                Console.WriteLine($"Failed to load scene list");

                bgfx.shutdown();
                Glfw.Terminate();

                return;
            }

            Scene.current = ResourceManager.instance.LoadScene(Scene.sceneList[0]);

            if(Scene.current == null)
            {
                Console.WriteLine($"Failed to load main scene");

                bgfx.shutdown();
                Glfw.Terminate();

                return;
            }

            var renderSystem = new RenderSystem();

            SubsystemManager.instance.RegisterSubsystem(renderSystem, RenderSystem.Priority);
            SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.instance, EntitySystemManager.Priority);

            if(playerAssembly != null)
            {
                var types = playerAssembly.GetTypes()
                    .Where(x => typeof(IEntitySystem).IsAssignableFrom(x));

                foreach(var type in types)
                {
                    try
                    {
                        var instance = (IEntitySystem)Activator.CreateInstance(type);

                        if(instance != null)
                        {
                            EntitySystemManager.instance.RegisterSystem(instance);
                        }
                    }
                    catch(System.Exception e)
                    {
                        Console.WriteLine($"Player: Failed to load entity system {type.FullName}: {e}");
                    }
                }
            }

            Input.window = window;

            Glfw.SetKeyCallback(window, (_, key, scancode, action, mods) =>
            {
                Input.KeyCallback(key, scancode, action, mods);
            });

            Glfw.SetCharCallback(window, (_, codepoint) =>
            {
                Input.CharCallback(codepoint);
            });

            Glfw.SetCursorPositionCallback(window, (_, xpos, ypos) =>
            {
                Input.CursorPosCallback((float)xpos, (float)ypos);
            });

            Glfw.SetMouseButtonCallback(window, (_, button, state, modifiers) =>
            {
                Input.MouseButtonCallback(button, state, modifiers);
            });

            Glfw.SetScrollCallback(window, (_, xOffset, yOffset) =>
            {
                Input.MouseScrollCallback((float)xOffset, (float)yOffset);
            });

            if(Glfw.RawMouseMotionSupported())
            {
                Glfw.SetInputMode(window, InputMode.RawMouseMotion, (int)GLFW.Constants.True);
            }

            lock(renderLock)
            {
                subsystemsReady = true;
            }

            while (!Glfw.WindowShouldClose(window) && window.IsClosed == false)
            {
                Glfw.PollEvents();

                lock(renderLock)
                {
                    shouldRender = appSettings.runInBackground == true || window.IsFocused == true;
                }

                Glfw.GetFramebufferSize(window, out var currentW, out var currentH);

                if (currentW != ScreenWidth || currentH != ScreenHeight)
                {
                    playerSettings.screenWidth = ScreenWidth = currentW;
                    playerSettings.screenHeight = ScreenHeight = currentH;

                    ResetRendering(hasFocus);
                }

                if(appSettings.runInBackground == false && window.IsFocused != hasFocus)
                {
                    hasFocus = window.IsFocused;

                    ResetRendering(hasFocus);

                    if(hasFocus == false)
                    {
                        continue;
                    }
                }
            }

            lock(renderLock)
            {
                shouldStop = true;
            }

            for(; ; )
            {
                if(renderThread.IsAlive == false)
                {
                    break;
                }

                Thread.Sleep(25);
            }

            Glfw.Terminate();
        }

    }
}
