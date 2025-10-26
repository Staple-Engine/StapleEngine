using MessagePack;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Staple;

public static class StaplePlayer
{
    public static void Run(string[] args)
    {
        MessagePackInit.Initialize();

        var baseDirectory = AppContext.BaseDirectory;

#if _DEBUG
        baseDirectory = Environment.CurrentDirectory;
#endif

        var skipFlow =
#if _DEBUG
            args.Any(x => x == "-skip-flow");
#else
            false;
#endif

        try
        {
            var pakFiles = Directory.GetFiles(Path.Combine(baseDirectory, "Data"), "*.pak");

            foreach (var file in pakFiles)
            {
                if (ResourceManager.instance.LoadPak(file) == false)
                {
                    Platform.platformProvider.ConsoleLog($"Failed to load player resources");

                    Environment.Exit(1);
                }
            }
        }
        catch(Exception)
        {
            Platform.platformProvider.ConsoleLog($"Failed to load player resources");

            Environment.Exit(1);
        }

        if(skipFlow == false)
        {
            try
            {
                var data = ResourceManager.instance.LoadFile("AppSettings");

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<AppSettingsHeader>(stream);

                if (header == null || header.header.SequenceEqual(AppSettingsHeader.ValidHeader) == false ||
                    header.version != AppSettingsHeader.ValidVersion)
                {
                    throw new Exception($"Invalid app settings header");
                }

                AppSettings.Current = MessagePackSerializer.Deserialize<AppSettings>(stream);

                if (AppSettings.Current == null)
                {
                    throw new Exception("Failed to deserialize app settings");
                }

                LayerMask.SetLayers(CollectionsMarshal.AsSpan(AppSettings.Current.layers),
                    CollectionsMarshal.AsSpan(AppSettings.Current.sortingLayers));
            }
            catch (Exception e)
            {
                Platform.platformProvider.ConsoleLog($"Failed to load appsettings: {e}");

                Environment.Exit(1);

                return;
            }
        }
        else
        {
            AppSettings.Current = AppSettings.Default;

            LayerMask.SetLayers(CollectionsMarshal.AsSpan(AppSettings.Current.layers),
                CollectionsMarshal.AsSpan(AppSettings.Current.sortingLayers));
        }

        new AppPlayer(args, true, skipFlow).Run();
    }
}
