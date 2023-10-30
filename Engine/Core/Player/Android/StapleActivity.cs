#if ANDROID
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using MessagePack;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Staple
{
    public partial class StapleActivity : Activity, ISurfaceHolderCallback, ISurfaceHolderCallback2
    {
        private static Dictionary<Keycode, KeyCode> keyMap = new()
        {
            { Keycode.Space, KeyCode.Space },
            { Keycode.Apostrophe, KeyCode.Apostrophe },
            { Keycode.Comma, KeyCode.Comma },
            { Keycode.Minus, KeyCode.Minus },
            { Keycode.Period, KeyCode.Period },
            { Keycode.Slash, KeyCode.Slash },
            { Keycode.Semicolon, KeyCode.SemiColon },
            { Keycode.Equals, KeyCode.Equal },
            { Keycode.A, KeyCode.A },
            { Keycode.B, KeyCode.B },
            { Keycode.C, KeyCode.C },
            { Keycode.D, KeyCode.D },
            { Keycode.E, KeyCode.E },
            { Keycode.F, KeyCode.F },
            { Keycode.G, KeyCode.G },
            { Keycode.H, KeyCode.H },
            { Keycode.I, KeyCode.I },
            { Keycode.J, KeyCode.J },
            { Keycode.K, KeyCode.K },
            { Keycode.L, KeyCode.L },
            { Keycode.M, KeyCode.M },
            { Keycode.N, KeyCode.N },
            { Keycode.O, KeyCode.O },
            { Keycode.P, KeyCode.P },
            { Keycode.Q, KeyCode.Q },
            { Keycode.R, KeyCode.R },
            { Keycode.S, KeyCode.S },
            { Keycode.T, KeyCode.T },
            { Keycode.U, KeyCode.U },
            { Keycode.V, KeyCode.V },
            { Keycode.W, KeyCode.W },
            { Keycode.X, KeyCode.X },
            { Keycode.Y, KeyCode.Y },
            { Keycode.Z, KeyCode.Z },
            { Keycode.Num0, KeyCode.Alpha0 },
            { Keycode.Num1, KeyCode.Alpha1 },
            { Keycode.Num2, KeyCode.Alpha2 },
            { Keycode.Num3, KeyCode.Alpha3 },
            { Keycode.Num4, KeyCode.Alpha4 },
            { Keycode.Num5, KeyCode.Alpha5 },
            { Keycode.Num6, KeyCode.Alpha6 },
            { Keycode.Num7, KeyCode.Alpha7 },
            { Keycode.Num8, KeyCode.Alpha8 },
            { Keycode.Num9, KeyCode.Alpha9 },
            { Keycode.LeftBracket, KeyCode.LeftBracket },
            { Keycode.Backslash, KeyCode.Backslash },
            { Keycode.RightBracket, KeyCode.Right },
            { Keycode.Grave, KeyCode.GraveAccent },
            { Keycode.Escape, KeyCode.Escape },
            { Keycode.Enter, KeyCode.Enter },
            { Keycode.Tab, KeyCode.Tab },
            { Keycode.Del, KeyCode.Backspace },
            { Keycode.Insert, KeyCode.Insert },
            { Keycode.ForwardDel, KeyCode.Delete },
            { Keycode.DpadRight, KeyCode.Right },
            { Keycode.DpadLeft, KeyCode.Left },
            { Keycode.DpadDown, KeyCode.Down },
            { Keycode.DpadUp, KeyCode.Up },
            { Keycode.PageUp, KeyCode.PageUp },
            { Keycode.PageDown, KeyCode.PageDown },
            { Keycode.Home, KeyCode.Home },
            { Keycode.MoveEnd, KeyCode.End },
            { Keycode.CapsLock, KeyCode.CapsLock },
            { Keycode.ScrollLock, KeyCode.ScrollLock },
            { Keycode.NumLock, KeyCode.NumLock },
            { Keycode.F1, KeyCode.F1 },
            { Keycode.F2, KeyCode.F2 },
            { Keycode.F3, KeyCode.F3 },
            { Keycode.F4, KeyCode.F4 },
            { Keycode.F5, KeyCode.F5 },
            { Keycode.F6, KeyCode.F6 },
            { Keycode.F7, KeyCode.F7 },
            { Keycode.F8, KeyCode.F8 },
            { Keycode.F9, KeyCode.F9 },
            { Keycode.F10, KeyCode.F10 },
            { Keycode.F11, KeyCode.F11 },
            { Keycode.F12, KeyCode.F12 },
            { Keycode.Numpad0, KeyCode.Numpad0 },
            { Keycode.Numpad1, KeyCode.Numpad1 },
            { Keycode.Numpad2, KeyCode.Numpad2 },
            { Keycode.Numpad3, KeyCode.Numpad3 },
            { Keycode.Numpad4, KeyCode.Numpad4 },
            { Keycode.Numpad5, KeyCode.Numpad5 },
            { Keycode.Numpad6, KeyCode.Numpad6 },
            { Keycode.Numpad7, KeyCode.Numpad7 },
            { Keycode.Numpad8, KeyCode.Numpad8 },
            { Keycode.Numpad9, KeyCode.Numpad9 },
            { Keycode.NumpadDot, KeyCode.NumpadDecimal },
            { Keycode.NumpadDivide, KeyCode.NumpadDivide },
            { Keycode.NumpadMultiply, KeyCode.NumpadMultiply },
            { Keycode.NumpadSubtract, KeyCode.NumpadSubtract },
            { Keycode.NumpadAdd, KeyCode.NumpadAdd },
            { Keycode.NumpadEnter, KeyCode.NumpadEnter },
            { Keycode.NumpadEquals, KeyCode.NumpadEqual },
            { Keycode.ShiftLeft, KeyCode.LeftShift },
            { Keycode.CtrlLeft, KeyCode.LeftControl },
            { Keycode.AltLeft, KeyCode.LeftAlt },
            { Keycode.ShiftRight, KeyCode.RightShift },
            { Keycode.CtrlRight, KeyCode.RightControl },
            { Keycode.AltRight, KeyCode.RightAlt },
        };

        private AppSettings? appSettings;
        private SurfaceView? surfaceView;
        private DateTime lastTime;
        private float fixedTimer = 0.0f;

        [LibraryImport("android")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial nint ANativeWindow_fromSurface(nint env, nint surface);

        [LibraryImport("nativewindow")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial int ANativeWindow_getWidth(nint window);

        [LibraryImport("nativewindow")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial int ANativeWindow_getHeight(nint window);

        [LibraryImport("nativewindow")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial nint ANativeWindow_release(nint window);

        class FrameCallback : Java.Lang.Object, Choreographer.IFrameCallback
        {
            public Action callback;

            public void DoFrame(long frameTimeNanos)
            {
                callback?.Invoke();
            }
        }

        class TouchCallback : Java.Lang.Object, View.IOnTouchListener
        {
            public bool OnTouch(View? v, MotionEvent? e)
            {
                for (var i = 0; i < e.PointerCount; i++)
                {
                    var ID = e.GetPointerId(i);
                    var coords = new MotionEvent.PointerCoords();

                    e.GetPointerCoords(i, coords);

                    var inputState = (e.Action & MotionEventActions.Mask) switch
                    {
                        MotionEventActions.Down => InputState.Press,
                        MotionEventActions.Up => InputState.Release,
                        _ => InputState.Repeat
                    };

                    if(inputState == InputState.Repeat)
                    {
                        continue;
                    }

                    AppEventQueue.instance.Add(AppEvent.Touch(ID, new Vector2(coords.X, coords.Y), inputState));
                }

                return true;
            }
        }

        class KeyCallback : Java.Lang.Object, View.IOnKeyListener
        {
            public bool OnKey(View? v, [GeneratedEnum] Keycode keyCode, KeyEvent? e)
            {
                ModifierKeys Modifiers()
                {
                    var modifiers = (ModifierKeys)0;

                    if(e.Modifiers.HasFlag(MetaKeyStates.AltOn))
                    {
                        modifiers |= ModifierKeys.Alt;
                    }

                    if (e.Modifiers.HasFlag(MetaKeyStates.ShiftOn))
                    {
                        modifiers |= ModifierKeys.Shift;
                    }

                    if (e.Modifiers.HasFlag(MetaKeyStates.CtrlOn))
                    {
                        modifiers |= ModifierKeys.Control;
                    }

                    if (e.Modifiers.HasFlag(MetaKeyStates.CapsLockOn))
                    {
                        modifiers |= ModifierKeys.CapsLock;
                    }

                    return modifiers;
                }

                var code = keyMap.TryGetValue(keyCode, out var key) ? key : Staple.KeyCode.Unknown;

                var modifiers = Modifiers();

                switch(e.Action)
                {
                    case KeyEventActions.Down:

                        AppEventQueue.instance.Add(AppEvent.Key(code, 0, InputState.Press, modifiers));

                        break;

                    case KeyEventActions.Up:

                        AppEventQueue.instance.Add(AppEvent.Key(code, 0, InputState.Release, modifiers));

                        break;

                    case KeyEventActions.Multiple:

                        if(e.KeyCode == Keycode.Unknown)
                        {
                            if (e.Characters != null)
                            {
                                foreach (var c in e.Characters)
                                {
                                    AppEventQueue.instance.Add(AppEvent.Text(c));
                                }
                            }
                        }
                        else if(e.KeyCharacterMap != null)
                        {
                            var repeatCount = e.RepeatCount;

                            var character = e.KeyCharacterMap.Get(e.KeyCode, e.MetaState);

                            for(var i = 0; i < repeatCount; i++)
                            {
                                AppEventQueue.instance.Add(AppEvent.Text((uint)character));
                            }
                        }

                        break;
                }

                return true;
            }
        }

        private FrameCallback? frameCallback;
        private TouchCallback? touchCallback;
        private KeyCallback? keyCallback;

        private void InitIfNeeded()
        {
            if (AppPlayer.instance?.renderWindow == null)
            {
                new AppPlayer(appSettings, Array.Empty<string>(), false);

                Log.Instance.onLog += (type, message) =>
                {
                    switch (type)
                    {
                        case Log.LogType.Info:

                            Android.Util.Log.Info("Staple", message);

                            break;

                        case Log.LogType.Error:

                            Android.Util.Log.Error("Staple", message);

                            break;

                        case Log.LogType.Warning:

                            Android.Util.Log.Warn("Staple", message);

                            break;

                        case Log.LogType.Debug:

                            Android.Util.Log.Debug("Staple", message);

                            break;
                    }
                };

                AppPlayer.instance.Create();

                if (AppPlayer.instance.renderWindow == null)
                {
                    System.Environment.Exit(1);
                }

                AndroidRenderWindow.Instance.Mutate((renderWindow) =>
                {
                    renderWindow.contextLost = false;
                });

                var renderWindow = AppPlayer.instance.renderWindow;

                renderWindow.InitBGFX();

                try
                {
                    renderWindow.OnScreenSizeChange?.Invoke(renderWindow.window.IsFocused);
                }
                catch (System.Exception)
                {
                }

                try
                {
                    renderWindow.OnInit?.Invoke();
                }
                catch (System.Exception e)
                {
                    Log.Error($"RenderWindow Init Exception: {e}");

                    System.Environment.Exit(1);

                    return;
                }

                if (renderWindow.shouldStop)
                {
                    System.Environment.Exit(1);

                    return;
                }

                frameCallback = new FrameCallback()
                {
                    callback = Frame,
                };

                Choreographer.Instance.PostFrameCallback(frameCallback);
            }
        }

        private void Frame()
        {
            if (AndroidRenderWindow.Instance.ShouldClose)
            {
                return;
            }

            Choreographer.Instance.PostFrameCallback(frameCallback);

            Input.Character = 0;
            Input.MouseDelta = Vector2.Zero;
            Input.MouseRelativePosition = Vector2.Zero;

            Input.UpdateState();

            var renderWindow = AppPlayer.instance.renderWindow;

            renderWindow.window.PollEvents();

            lock (renderWindow.renderLock)
            {
                renderWindow.shouldRender = renderWindow.window.Unavailable == false && (appSettings.runInBackground == true || renderWindow.window.IsFocused == true);
            }

            if (renderWindow.window.Unavailable)
            {
                return;
            }

            renderWindow.window.GetWindowSize(out var currentW, out var currentH);

            if ((currentW != renderWindow.width || currentH != renderWindow.height) && renderWindow.window.ShouldClose == false)
            {
                renderWindow.width = currentW;
                renderWindow.height = currentH;

                try
                {
                    renderWindow.OnScreenSizeChange?.Invoke(renderWindow.hasFocus);
                }
                catch (Exception)
                {
                }
            }

            renderWindow.CheckContextLost();

            if (appSettings.runInBackground == false && renderWindow.window.IsFocused != renderWindow.hasFocus)
            {
                renderWindow.hasFocus = renderWindow.window.IsFocused;

                try
                {
                    renderWindow.OnScreenSizeChange?.Invoke(renderWindow.hasFocus);
                }
                catch (Exception)
                {
                }

                if (renderWindow.hasFocus == false)
                {
                    return;
                }
            }

            renderWindow.CheckEvents();

            if (renderWindow.Paused)
            {
                fixedTimer = 0;
            }
            else
            {
                var current = DateTime.Now;

                fixedTimer += (float)(current - lastTime).TotalSeconds;

                //Prevent hard stuck
                var tries = 0;

                while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && tries < 3)
                {
                    fixedTimer -= Time.fixedDeltaTime;

                    renderWindow.OnFixedUpdate?.Invoke();

                    tries++;
                }

                if (tries >= 3)
                {
                    fixedTimer = 0;
                }
            }

            renderWindow.RenderFrame(ref lastTime);
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            void Finish()
            {
                var nativeWindow = ANativeWindow_fromSurface(JNIEnv.Handle, holder.Surface.Handle);

                AndroidRenderWindow.Instance.Mutate((renderWindow) =>
                {
                    if (renderWindow.window != nint.Zero)
                    {
                        ANativeWindow_release(renderWindow.window);
                    }

                    renderWindow.screenWidth = width;
                    renderWindow.screenHeight = height;
                    renderWindow.window = nativeWindow;
                    renderWindow.unavailable = false;

                    renderWindow.ContextLost = true;
                });

                new Handler(Looper.MainLooper).Post(() =>
                {
                    try
                    {
                        InitIfNeeded();
                    }
                    catch (Exception e)
                    {
                        Android.Util.Log.Error("Staple", $"Exception: {e}");
                    }
                });

                Log.Debug($"Surface Changed - Screen size: {width}x{height}. Is creating: {holder.IsCreating}. nativeWindow: {nativeWindow.ToString("X")}, format: {format}");
            }

            void Delay()
            {
                if (holder.IsCreating)
                {
                    Task.Delay(TimeSpan.FromMilliseconds(100)).ContinueWith((t) => Delay());
                }
                else
                {
                    Finish();
                }
            }

            Delay();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            Log.Debug("Surface Created");
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            Log.Debug("Surface Destroyed");

            AndroidRenderWindow.Instance.Mutate((renderWindow) =>
            {
                renderWindow.unavailable = true;
            });
        }

        protected virtual string[] AdditionalLibraries()
        {
            var outValue = new HashSet<string>();

            foreach(var type in TypeCache.AllTypes())
            {
                var attributes = type.GetCustomAttributes(true);

                foreach(var attribute in attributes)
                {
                    if(attribute is AdditionalLibraryAttribute library && library.platform == AppPlatform.Android)
                    {
                        outValue.Add(library.path);
                    }
                }
            }

            return outValue.ToArray();
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Java.Lang.JavaSystem.LoadLibrary("android");
            Java.Lang.JavaSystem.LoadLibrary("nativewindow");
            Java.Lang.JavaSystem.LoadLibrary("log");
            Java.Lang.JavaSystem.LoadLibrary("bgfx");
            Java.Lang.JavaSystem.LoadLibrary("joltc");
            Java.Lang.JavaSystem.LoadLibrary("freetype6");

            foreach (var library in AdditionalLibraries())
            {
                Java.Lang.JavaSystem.LoadLibrary(library);
            }

            if(Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                WindowManagerLayoutParams p = Window.Attributes;

                var modes = Display.GetSupportedModes();

                var maxMode = 0;
                var maxHZ = 60.0f;

                foreach(var mode in modes)
                {
                    if(maxHZ < mode.RefreshRate)
                    {
                        maxHZ = mode.RefreshRate;
                        maxMode = mode.ModeId;
                    }
                }

                p.PreferredDisplayModeId = maxMode;

                Window.Attributes = p;
            }

            Threading.Initialize();

            surfaceView = new SurfaceView(this);

            SetContentView(surfaceView);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Always;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);
                Window.InsetsController.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                Window.InsetsController.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                Window.DecorView.SystemUiVisibility = StatusBarVisibility.Hidden;
            }

            MessagePackInit.Initialize();

            ResourceManager.instance.assetManager = Assets;

            if (ResourceManager.instance.LoadPak("DefaultResources.pak") == false ||
                ResourceManager.instance.LoadPak("Resources.pak") == false)
            {
                Console.WriteLine("Failed to load player resources");

                System.Environment.Exit(1);
            }

            try
            {
                var data = ResourceManager.instance.LoadFile("AppSettings");

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<AppSettingsHeader>(stream);

                if (header == null || header.header.SequenceEqual(AppSettingsHeader.ValidHeader) == false ||
                    header.version != AppSettingsHeader.ValidVersion)
                {
                    throw new Exception("Invalid app settings header");
                }

                appSettings = MessagePackSerializer.Deserialize<AppSettings>(stream);

                if (appSettings == null)
                {
                    throw new Exception("Failed to deserialize app settings");
                }

                LayerMask.AllLayers = appSettings.layers;
                LayerMask.AllSortingLayers = appSettings.sortingLayers;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load appsettings: {e}");

                System.Environment.Exit(1);

                return;
            }

            touchCallback = new();
            keyCallback = new();

            surfaceView.Holder.SetKeepScreenOn(true);
            surfaceView.Holder.AddCallback(this);
            surfaceView.SetOnTouchListener(touchCallback);
            surfaceView.SetOnKeyListener(keyCallback);
        }

        protected override void OnPause()
        {
            base.OnPause();

            AndroidRenderWindow.Instance.EnterBackground();
        }

        protected override void OnResume()
        {
            base.OnResume();

            AndroidRenderWindow.Instance.EnterForeground();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            AndroidRenderWindow.Instance.Mutate((renderWindow) =>
            {
                renderWindow.shouldClose = true;
            });

            try
            {
                AppPlayer.instance?.renderWindow.OnCleanup?.Invoke();
            }
            catch (Exception)
            {
            }
        }

        public void SurfaceRedrawNeeded(ISurfaceHolder holder)
        {
        }
    }
}
#endif