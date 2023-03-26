using MessagePack;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple
{
    public static class StaplePlayer
    {
        public static void Run(string[] args)
        {
            AppSettings settings = null;

            var baseDirectory = AppContext.BaseDirectory;

#if _DEBUG
            baseDirectory = Environment.CurrentDirectory;
#endif

            try
            {
                var data = File.ReadAllBytes($"{baseDirectory}/Data/AppSettings");

                using var stream = new MemoryStream(data);

                var header = MessagePackSerializer.Deserialize<AppSettingsHeader>(stream);

                if (header == null || header.header.SequenceEqual(AppSettingsHeader.ValidHeader) == false ||
                    header.version != AppSettingsHeader.ValidVersion)
                {
                    throw new Exception($"Invalid app settings header");
                }

                settings = MessagePackSerializer.Deserialize<AppSettings>(stream);

                if(settings == null)
                {
                    throw new Exception("Failed to deserialize app settings");
                }
            }
            catch (Exception e)
            {
                Environment.Exit(1);

                return;
            }

            new AppPlayer(settings, args).Run();
        }
    }
}
