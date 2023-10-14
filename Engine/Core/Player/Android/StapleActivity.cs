#if ANDROID
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Interop;
using MessagePack;
using Staple;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Staple
{
    public partial class StapleActivity : Activity, ISurfaceHolderCallback, ISurfaceHolderCallback2
    {
        private AppSettings? appSettings;
        private SurfaceView surfaceView;
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

        private FrameCallback callback;

        private void InitIfNeeded()
        {
            if (AppPlayer.instance?.renderWindow == null)
            {
                new AppPlayer(appSettings, Array.Empty<string>(), false);

                Log.Instance.onLog += (type, message) =>
                {
                    switch(type)
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

                callback = new FrameCallback()
                {
                    callback = Frame,
                };

                Choreographer.Instance.PostFrameCallback(callback);
            }
        }

        private void Frame()
        {
            if(AndroidRenderWindow.Instance.ShouldClose)
            {
                return;
            }

            Choreographer.Instance.PostFrameCallback(callback);

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

                if(tries >= 3)
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
            return Array.Empty<string>();
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

            surfaceView.Holder.SetKeepScreenOn(true);
            surfaceView.Holder.AddCallback(this);
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