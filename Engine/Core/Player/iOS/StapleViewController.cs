#if IOS
using Foundation;
using MessagePack;
using ObjCRuntime;
using UIKit;
using System;
using System.Linq;
using System.IO;
using Staple.Internal;

namespace Staple;

[Register("StapleViewController")]
public class StapleViewController : UIViewController
{
    private bool needsInit = true;
    private AppSettings? appSettings;
    private DateTime lastTime;
    private float fixedTimer = 0.0f;

    public StapleViewController(NativeHandle handle) : base(handle)
    {
    }

    public override void ViewDidAppear(bool animated)
    {
        base.ViewDidAppear(animated);

        if (needsInit)
        {
            needsInit = false;

            Threading.Initialize();

            MessagePackInit.Initialize();

            ResourceManager.instance.resourcePaths.Add("Data");

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

            iOSRenderWindow.Instance.metalView = View as MetalView;

            if (AppPlayer.instance?.renderWindow == null)
            {
                new AppPlayer(appSettings, Array.Empty<string>(), false);

                Log.Instance.onLog += (type, message) =>
                {
                    switch (type)
                    {
                        case Log.LogType.Info:

                            CoreFoundation.OSLog.Default.Log(CoreFoundation.OSLogLevel.Default, message);

                            break;

                        case Log.LogType.Error:

                            CoreFoundation.OSLog.Default.Log(CoreFoundation.OSLogLevel.Error, message);

                            break;

                        case Log.LogType.Warning:

                            CoreFoundation.OSLog.Default.Log(CoreFoundation.OSLogLevel.Error, message);

                            break;

                        case Log.LogType.Debug:

                            CoreFoundation.OSLog.Default.Log(CoreFoundation.OSLogLevel.Debug, message);

                            break;
                    }
                };

                AppPlayer.instance.Create();

                if (AppPlayer.instance.renderWindow == null)
                {
                    System.Environment.Exit(1);
                }

                iOSRenderWindow.Instance.Mutate((renderWindow) =>
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
            }
        }
    }
}
#endif
