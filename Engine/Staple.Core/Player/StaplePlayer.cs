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
                if (!ResourceManager.instance.LoadPak(file))
                {
                    Platform.ConsoleLog($"Failed to load player resources");

                    Environment.Exit(1);
                }
            }
        }
        catch(Exception)
        {
            Platform.ConsoleLog($"Failed to load player resources");

            Environment.Exit(1);
        }

        if(!skipFlow)
        {
            try
            {
                var data = ResourceManager.instance.LoadFile("AppSettings");

                var header = SerializationUtils.MessagePackDeserialize<AppSettingsHeader>(data.AsMemory(), out var offset);

                if (header == null || !header.header.SequenceEqual(AppSettingsHeader.ValidHeader) ||
                    header.version != AppSettingsHeader.ValidVersion)
                {
                    throw new Exception($"Invalid app settings header");
                }

                AppSettings.Current = SerializationUtils.MessagePackDeserialize<AppSettings>(data.AsMemory(offset), out _) ??
                    throw new Exception("Failed to deserialize app settings");

                LayerMask.SetLayers(CollectionsMarshal.AsSpan(AppSettings.Active.layers),
                    CollectionsMarshal.AsSpan(AppSettings.Active.sortingLayers));
            }
            catch (Exception e)
            {
                Platform.ConsoleLog($"Failed to load appsettings: {e}");

                Environment.Exit(1);

                return;
            }
        }
        else
        {
            AppSettings.Current = AppSettings.Default;

            LayerMask.SetLayers(CollectionsMarshal.AsSpan(AppSettings.Active.layers),
                CollectionsMarshal.AsSpan(AppSettings.Active.sortingLayers));
        }

        new AppPlayer(args, true, skipFlow).Run();
    }
}
