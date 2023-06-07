using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Baker
{
    static partial class Program
    {
        private static void ProcessAppSettings(string inputPath, string outputPath)
        {
            Console.WriteLine($"Processing AppSettings");

            string appSettingsText;

            try
            {
                appSettingsText = File.ReadAllText(Path.Combine(inputPath, "..", "Settings", "AppSettings.json"));
            }
            catch (Exception)
            {
                Console.WriteLine($"\t\tError: Failed to read app settings");

                return;
            }

            if ((appSettingsText?.Length ?? 0) > 0)
            {
                AppSettings settings;

                try
                {
                    settings = JsonConvert.DeserializeObject<AppSettings>(appSettingsText);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\t\tError: Failed to load app settings: {e.Message}");

                    return;
                }

                if (settings != null)
                {
                    var outputFile = Path.Combine(outputPath, "AppSettings");

                    try
                    {
                        File.Delete(outputFile);
                    }
                    catch (Exception)
                    {
                    }

                    var header = new AppSettingsHeader();

                    using (var stream = File.OpenWrite(outputFile))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            var encoded = MessagePackSerializer.Serialize(header)
                                .Concat(MessagePackSerializer.Serialize(settings));

                            writer.Write(encoded.ToArray());
                        }
                    }

                    Console.WriteLine($"\tProcessed app settings");
                }
            }

        }
    }
}
