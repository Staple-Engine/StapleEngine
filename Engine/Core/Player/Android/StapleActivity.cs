#if ANDROID
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MessagePack;
using Staple;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Staple
{
    public partial class StapleActivity : Activity, ISurfaceHolderCallback, ISurfaceHolderCallback2
    {
        private Thread? loopThread;
        private AppSettings? appSettings;
        private SurfaceView surfaceView;

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

        private void InitIfNeeded()
        {
            if(loopThread == null)
            {
                loopThread = new Thread(new ParameterizedThreadStart((o) =>
                {
                    try
                    {
                        new AppPlayer(appSettings, new string[0]).Run();
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"Exception while running player: {e}");
                    }
                    finally
                    {
                        Log.Debug("Finishing Staple activity");

                        AndroidRenderWindow.Instance.shouldClose = true;

                        Finish();
                    }
                }));

                loopThread.Start();
            }
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            void Finish()
            {
                AndroidRenderWindow.Instance.Mutate((renderWindow) =>
                {
                    if (renderWindow.window != nint.Zero)
                    {
                        ANativeWindow_release(renderWindow.window);
                    }

                    var nativeWindow = ANativeWindow_fromSurface(JNIEnv.Handle, holder.Surface.Handle);

                    Log.Debug($"Surface Changed - Screen size: {width}x{height}. Is creating: {holder.IsCreating}. nativeWindow: {nativeWindow.ToString("X")}, format: {format}");

                    renderWindow.screenWidth = width;
                    renderWindow.screenHeight = height;
                    renderWindow.window = nativeWindow;
                    renderWindow.unavailable = false;

                    if (loopThread != null)
                    {
                        Log.Debug($"Context Lost");

                        renderWindow.ContextLost = true;

                        return;
                    }
                });

                InitIfNeeded();
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

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Java.Lang.JavaSystem.LoadLibrary("android");
            Java.Lang.JavaSystem.LoadLibrary("nativewindow");
            Java.Lang.JavaSystem.LoadLibrary("log");
            Java.Lang.JavaSystem.LoadLibrary("bgfx");
            Java.Lang.JavaSystem.LoadLibrary("joltc");

            Threading.Initialize();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Always;
            }

            if(Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);
                Window.InsetsController.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                Window.InsetsController.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
            else if(Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
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

            surfaceView = new SurfaceView(this);

            SetContentView(surfaceView);

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

            AndroidRenderWindow.Instance.shouldClose = true;
        }

        public void SurfaceRedrawNeeded(ISurfaceHolder holder)
        {
        }
    }
}
#endif