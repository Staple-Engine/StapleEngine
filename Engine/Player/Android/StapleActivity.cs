using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using MessagePack;
using Staple;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Player
{
    [Activity(Label = "@string/app_name",
        MainLauncher = true,
        Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
        ResizeableActivity = false,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
        AlwaysRetainTaskState = true,
        Exported = true,
        LaunchMode = LaunchMode.SingleInstance)]
    public class StapleActivity : Activity, ISurfaceHolderCallback, ISurfaceHolderCallback2
    {
        private Thread? loopThread;
        private AppSettings? appSettings;

        [DllImport("android", CallingConvention = CallingConvention.Cdecl)]
        private extern static nint ANativeWindow_fromSurface(nint env, nint surface);

        [DllImport("nativewindow", CallingConvention = CallingConvention.Cdecl)]
        private extern static nint ANativeWindow_release(nint window);

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
            Log.Debug($"Surface Changed - Screen size: {width}x{height}");

            var nativeWindow = ANativeWindow_fromSurface(JNIEnv.Handle, holder.Surface.Handle);

            AndroidRenderWindow.Instance.Mutate((renderWindow) =>
            {
                renderWindow.screenWidth = width;
                renderWindow.screenHeight = height;
                renderWindow.window = nativeWindow;
                renderWindow.unavailable = false;

                if (loopThread != null)
                {
                    renderWindow.ContextLost = true;

                    return;
                }
            });

            InitIfNeeded();
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

            SetContentView(Resource.Layout.activity_main);

            MessagePackInit.Initialize();

            TypeCacheRegistration.RegisterAll();

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

            var surfaceView = FindViewById<SurfaceView>(Resource.Id.surface);

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

        public void SurfaceRedrawNeeded(ISurfaceHolder holder)
        {
        }
    }
}