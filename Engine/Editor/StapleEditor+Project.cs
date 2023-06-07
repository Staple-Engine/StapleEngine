using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;
using Staple.Internal;

namespace Staple.Editor
{
    partial class StapleEditor
    {
        public void LoadProject(string path)
        {
            basePath = ThumbnailCache.basePath = path;

            Log.Info($"Base Path: {basePath}");

            UpdateProjectBrowserNodes();

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache"));
            }
            catch (Exception)
            {
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Thumbnails"));
            }
            catch (Exception)
            {
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Staging"));
            }
            catch (Exception)
            {
            }

            void Recursive(List<ProjectBrowserNode> nodes)
            {
                foreach (var node in nodes)
                {
                    if(node.type == ProjectBrowserNodeType.Folder)
                    {
                        Recursive(node.subnodes);
                    }
                    else
                    {
                        if(node.TypeString.ToUpperInvariant() == "TEXTURE")
                        {
                            try
                            {
                                if(File.Exists($"{node.path}.meta") == false)
                                {
                                    var settings = new JsonSerializerSettings();

                                    settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

                                    var jsonData = JsonConvert.SerializeObject(new TextureMetadata(), Formatting.Indented, settings);

                                    File.WriteAllText($"{node.path}.meta", jsonData);
                                }
                            }
                            catch(System.Exception)
                            {
                            }
                        }
                    }
                }
            }

            Recursive(projectBrowserNodes);

            try
            {
                projectAppSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(Path.Combine(basePath, "Settings", "AppSettings.json")));
            }
            catch(Exception)
            {
            }

            if(projectAppSettings != null)
            {
                LayerMask.AllLayers = projectAppSettings.layers;
                LayerMask.AllSortingLayers = projectAppSettings.sortingLayers;

                window.appSettings.fixedTimeFrameRate = projectAppSettings.fixedTimeFrameRate;
            }

            RefreshStaging();
        }

        public void RefreshStaging()
        {
            var bakerPath = Path.Combine(Environment.CurrentDirectory, "..", "Tools", "bin", "Baker");

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Staging", "d3d11"));
            }
            catch (Exception)
            {
            }

            //TODO: Build for each API

            var args = $"-i \"{basePath}/Assets\" -o \"{basePath}/Cache/Staging/d3d11\" -r d3d11".Replace("\\", "/");

            var processInfo = new ProcessStartInfo(bakerPath, args);

            processInfo.CreateNoWindow = true;
            processInfo.RedirectStandardOutput = true;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.WorkingDirectory = Environment.CurrentDirectory;

            var process = new Process();

            process.StartInfo = processInfo;

            if(process.Start())
            {
                while(process.HasExited == false)
                {
                    var line = process.StandardOutput.ReadLine();

                    if(line != null)
                    {
                        Log.Info(line);
                    }
                }
            }
        }
    }
}