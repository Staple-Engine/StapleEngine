using MessagePack;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;

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

        if (ResourceManager.instance.LoadPak(Path.Combine(baseDirectory, "Data", "DefaultResources.pak")) == false ||
            ResourceManager.instance.LoadPak(Path.Combine(baseDirectory, "Data", "Resources.pak")) == false)
        {
            Console.WriteLine($"Failed to load player resources");

            Environment.Exit(1);
        }

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

            if(AppSettings.Current == null)
            {
                throw new Exception("Failed to deserialize app settings");
            }

            LayerMask.AllLayers = AppSettings.Current.layers;
            LayerMask.AllSortingLayers = AppSettings.Current.sortingLayers;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load appsettings: {e}");

            Environment.Exit(1);

            return;
        }

        new AppPlayer(args, true).Run();
    }
}
