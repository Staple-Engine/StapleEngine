using System.IO;
using System;
using Staple.Internal;

namespace Staple.ProjectManagement;

public class WindowsBuildProcessor : IBuildPreprocessor
{
    public BuildProcessorResult OnPreprocessBuild(BuildInfo buildInfo)
    {
        if(buildInfo.platform != AppPlatform.Windows)
        {
            return BuildProcessorResult.Continue;
        }

        var basePath = buildInfo.basePath;
        var projectDirectory = buildInfo.assemblyProjectPath;

        if(StorageUtils.CopyFile(Path.Combine(buildInfo.backendResourcesPath, "Program.cs"), Path.Combine(projectDirectory, "Program.cs")) == false)
        {
            Log.Debug($"{GetType().Name}: Failed to copy program script");

            return BuildProcessorResult.Failed;
        }

        try
        {
            var iconData = File.ReadAllBytes(Path.Combine(basePath, "Settings", "Icon.png"));

            var textureData = Texture.LoadStandard(iconData, StandardTextureColorComponents.RGBA);

            if (textureData != null)
            {
                var width = 256;
                var height = (int)(textureData.height / (float)textureData.width * 256);

                if (textureData.Resize(width, height))
                {
                    var pngData = textureData.EncodePNG();

                    var stream = new MemoryStream();

                    //reserved
                    stream.WriteByte(0);
                    stream.WriteByte(0);

                    //image type (1 = icon, 2 = cursor)
                    var bytes = BitConverter.GetBytes((short)1);

                    stream.Write(bytes);

                    //number of images, same as type since we only got one
                    stream.Write(bytes);

                    //image width
                    stream.WriteByte((byte)(width % 256));

                    //image height
                    stream.WriteByte((byte)(height % 256));

                    //number of colors
                    stream.WriteByte(0);

                    //reserved
                    stream.WriteByte(0);

                    //4-5 color planes
                    stream.WriteByte(0);
                    stream.WriteByte(0);

                    //bits per pixel
                    bytes = BitConverter.GetBytes((short)32);

                    stream.Write(bytes);

                    //size of image data
                    bytes = BitConverter.GetBytes(pngData.Length);

                    stream.Write(bytes);

                    //offset of image data
                    bytes = BitConverter.GetBytes(22);

                    stream.Write(bytes);

                    stream.Write(pngData);

                    var final = stream.ToArray();

                    File.WriteAllBytes(Path.Combine(projectDirectory, "Icon.ico"), final);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"{GetType().Name}: Failed to prepare icon file: {e}");

            return BuildProcessorResult.Failed;
        }

        return BuildProcessorResult.Success;
    }
}
