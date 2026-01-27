using Android.OS;
using Android.Telecom;
using Android.Views;
using MessagePack;
using Org.Libsdl.App;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Staple;

public partial class StapleActivity : SDLActivity
{
    public static bool verbose = false;

    private static readonly string LogTag = "Staple Engine";

    private static void LogError(string message)
    {
        Android.Util.Log.Error(LogTag, message);
    }

    private static void LogInfo(string message)
    {
        Android.Util.Log.Info(LogTag, message);
    }

    private static void LogDebug(string message)
    {
        Android.Util.Log.Debug(LogTag, message);
    }

    private static void LogWarning(string message)
    {
        Android.Util.Log.Warn(LogTag, message);
    }

    private DateTime lastTime;
    private float fixedTimer = 0.0f;

    private void InitIfNeeded()
    {
        if (AppPlayer.instance?.renderWindow == null)
        {
            new AppPlayer(Array.Empty<string>(), false, false);

            Log.Instance.onLog += (type, message) =>
            {
                switch (type)
                {
                    case Log.LogType.Info:

                        LogInfo(message);

                        break;

                    case Log.LogType.Error:

                        LogError(message);

                        break;

                    case Log.LogType.Warning:

                        LogWarning(message);

                        break;

                    case Log.LogType.Debug:

                        LogDebug(message);

                        break;
                }
            };

            AppPlayer.instance.Create();

            if (AppPlayer.instance.renderWindow == null)
            {
                LogError("Failed to create render window, exiting...");

                System.Environment.Exit(1);
            }

            var renderWindow = AppPlayer.instance.renderWindow;

            renderWindow.InitializeRenderer();

            try
            {
                renderWindow.OnScreenSizeChange?.Invoke(renderWindow.window.IsFocused);
            }
            catch (Exception)
            {
            }

            try
            {
                renderWindow.OnInit?.Invoke();

                StapleHooks.ExecuteHooks(StapleHookEvent.Init, null);
            }
            catch (Exception e)
            {
                LogError($"RenderWindow Init Exception: {e}");

                System.Environment.Exit(1);

                return;
            }

            if (renderWindow.shouldStop)
            {
                if(verbose)
                {
                    LogDebug("RenderWindow ShouldStop");
                }

                System.Environment.Exit(1);

                return;
            }
        }
    }

    private void Frame()
    {
        PerformanceProfilerSystem.StartFrame();

        Input.UpdateState();

        var renderWindow = AppPlayer.instance.renderWindow;

        renderWindow.window.PollEvents();

        lock (renderWindow.renderLock)
        {
            renderWindow.shouldRender = !renderWindow.window.Unavailable && renderWindow.window.IsFocused;
        }

        if (renderWindow.window.Unavailable)
        {
            return;
        }

        if (!renderWindow.Paused)
        {
            RenderSystem.Backend.BeginFrame();
        }

        var size = renderWindow.window.Size;

        if ((size.X != renderWindow.width || size.Y != renderWindow.height) && !renderWindow.window.ShouldClose)
        {
            renderWindow.width = size.X;
            renderWindow.height = size.Y;

            try
            {
                renderWindow.OnScreenSizeChange?.Invoke(renderWindow.hasFocus);
            }
            catch (Exception)
            {
            }
        }

        renderWindow.CheckContextLost();

        if (renderWindow.window.IsFocused != renderWindow.hasFocus)
        {
            renderWindow.hasFocus = renderWindow.window.IsFocused;

            try
            {
                renderWindow.OnScreenSizeChange?.Invoke(renderWindow.hasFocus);
            }
            catch (Exception)
            {
            }

            if (!renderWindow.hasFocus)
            {
                return;
            }
        }

        renderWindow.CheckEvents();

        if (renderWindow.Paused)
        {
            fixedTimer = 0;

            //Prevent CPU over-use
            Thread.Sleep(100);
        }
        else
        {
            var current = DateTime.UtcNow;

            fixedTimer += (float)(current - lastTime).TotalSeconds;

            //Prevent hard stuck
            var currentFixedTime = 0.0f;

            while (Time.fixedDeltaTime > 0 && fixedTimer >= Time.fixedDeltaTime && currentFixedTime < AppSettings.Current.maximumFixedTimestepTime)
            {
                fixedTimer -= Time.fixedDeltaTime;

                renderWindow.OnFixedUpdate?.Invoke();

                StapleHooks.ExecuteHooks(StapleHookEvent.FixedUpdate, null);

                currentFixedTime += Time.fixedDeltaTime;
            }

            if (currentFixedTime >= AppSettings.Current.maximumFixedTimestepTime)
            {
                fixedTimer = 0;
            }
        }

        renderWindow.RenderFrame(ref lastTime);

        ThreadHelper.Update();
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

    protected override string[] GetLibraries() => ["SDL3"];

    protected override void Main()
    {
        InitIfNeeded();

        while (!(AppPlayer.instance?.renderWindow.shouldStop ?? false))
        {
            Frame();
        }
    }

    protected override void OnCreate(Bundle savedInstanceState)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            WindowManagerLayoutParams p = Window.Attributes;

            var modes = WindowManager.DefaultDisplay.GetSupportedModes();

            var maxMode = 0;
            var maxHZ = 60.0f;

            foreach (var mode in modes)
            {
                if (maxHZ < mode.RefreshRate)
                {
                    maxHZ = mode.RefreshRate;
                    maxMode = mode.ModeId;
                }
            }

            p.PreferredDisplayModeId = maxMode;

            //AndroidRenderWindow.Instance.refreshRate = (int)maxHZ;

            Window.Attributes = p;
        }

        base.OnCreate(savedInstanceState);

        foreach (var library in AdditionalLibraries())
        {
            Java.Lang.JavaSystem.LoadLibrary(library);
        }

        AndroidPlatformProvider.Instance.assetManager = Assets;

        ThreadHelper.Initialize();

        MessagePackInit.Initialize();

        var packages = Assets.List("").Where(x => x.EndsWith(".gif")).ToArray();

        LogInfo($"Loading {packages.Length} packages");

        foreach (var file in packages)
        {
            if (!ResourceManager.instance.LoadPak(file))
            {
                LogError("Failed to load player resources");

                System.Environment.Exit(1);
            }
        }

        try
        {
            var data = ResourceManager.instance.LoadFile("AppSettings");

            using var stream = new MemoryStream(data);

            var header = MessagePackSerializer.Deserialize<AppSettingsHeader>(stream);

            if (header == null || !header.header.SequenceEqual(AppSettingsHeader.ValidHeader) ||
                header.version != AppSettingsHeader.ValidVersion)
            {
                throw new Exception("Invalid app settings header");
            }

            AppSettings.Current = MessagePackSerializer.Deserialize<AppSettings>(stream);

            if (AppSettings.Current == null)
            {
                throw new Exception("Failed to deserialize app settings");
            }

            LayerMask.SetLayers(CollectionsMarshal.AsSpan(AppSettings.Current.layers), CollectionsMarshal.AsSpan(AppSettings.Current.sortingLayers));
        }
        catch (Exception e)
        {
            LogError($"Failed to load appsettings: {e}");

            System.Environment.Exit(1);

            return;
        }
    }

    protected override void OnPause()
    {
        base.OnPause();

        AudioSystem.Instance.EnterBackground();
    }

    protected override void OnResume()
    {
        base.OnResume();

        AudioSystem.Instance.EnterForeground();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        AppPlayer.instance?.renderWindow.shouldStop = true;
        AppPlayer.instance?.renderWindow.shouldRender = false;

        try
        {
            AppPlayer.instance?.renderWindow.OnCleanup?.Invoke();

            StapleHooks.ExecuteHooks(StapleHookEvent.Cleanup, null);
        }
        catch (Exception)
        {
        }
    }
}
