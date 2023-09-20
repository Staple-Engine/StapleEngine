using Android.App;
using Android.OS;
using Staple;
using Staple.Internal;
using System;
using System.Threading;

namespace Player
{
    [Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.NoTitleBar.Fullscreen")]
    public class StapleActivity : Activity
    {
        private Thread loopThread;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Java.Lang.JavaSystem.LoadLibrary("log");
            Java.Lang.JavaSystem.LoadLibrary("bgfx");

            var renderWindow = AndroidRenderWindow.Instance;

            renderWindow.window = Window.Handle;
            renderWindow.screenWidth = Resources.DisplayMetrics.WidthPixels;
            renderWindow.screenHeight = Resources.DisplayMetrics.HeightPixels;

            Log.SetLog(new ConsoleLog());

            loopThread = new Thread(new ParameterizedThreadStart((o) =>
            {
                try
                {
                    var gameWindow = RenderWindow.Create(0, 0, false, WindowMode.Fullscreen, AppSettings.Default, 0, Bgfx.bgfx.ResetFlags.Vsync | Bgfx.bgfx.ResetFlags.SrgbBackbuffer);

                    if (gameWindow == null)
                    {
                        Finish();

                        return;
                    }

                    gameWindow.Run();
                }
                catch(Exception e)
                {
                    Android.Util.Log.Debug("Staple", $"Exception while running player: {e}");
                }
                finally
                {
                    AndroidRenderWindow.Instance.shouldClose = true;

                    Finish();
                }
            }));

            loopThread.Start();
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
    }
}