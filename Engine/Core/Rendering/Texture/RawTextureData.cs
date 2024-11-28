using System;
using System.IO;
using StbImageResizeSharp;
using StbImageWriteSharp;

namespace Staple;

public class RawTextureData
{
    public StandardTextureColorComponents colorComponents;
    public int width;
    public int height;
    public byte[] data;

    public Color32[] ToColorArray()
    {
        var outValue = new Color32[width * height];

        switch(colorComponents)
        {
            case StandardTextureColorComponents.Greyscale:

                for(var i = 0; i < data.Length; i++)
                {
                    outValue[i] = new(data[i], data[i], data[i], 255);
                }

                break;

            case StandardTextureColorComponents.GreyscaleAlpha:

                for (int i = 0, index = 0; i < data.Length; i += 2)
                {
                    outValue[index++] = new(data[i], data[i], data[i], data[i + 1]);
                }

                break;

            case StandardTextureColorComponents.RGB:

                for (int i = 0, index = 0; i < data.Length; i += 3)
                {
                    outValue[index++] = new(data[i], data[i + 1], data[i + 2], 255);
                }

                break;

            case StandardTextureColorComponents.RGBA:

                for (int i = 0, index = 0; i < data.Length; i += 4)
                {
                    outValue[index++] = new(data[i], data[i + 1], data[i + 2], data[i + 3]);
                }

                break;

            default:

                return [];
        }

        return outValue;
    }

    public void Blit(int sourceX, int sourceY, int sourceWidth, int sourceHeight, int sourcePitch, byte[] sourceData, int destX, int destY)
    {
        if(colorComponents != StandardTextureColorComponents.RGBA)
        {
            return;
        }

        if(destX + sourceWidth > width ||
            destY + sourceHeight > height ||
            destX < 0 ||
            destY < 0)
        {
            return;
        }

        var rowSize = sourceWidth * 4;
        var destPitch = width * 4;

        for(int y = 0, yPos = sourceY * sourcePitch, destYPos = destY * destPitch; y < sourceHeight; y++, yPos += sourcePitch, destYPos += destPitch)
        {
            Buffer.BlockCopy(sourceData, yPos + sourceX * 4, data, destYPos + destX * 4, rowSize);
        }
    }

    public bool Resize(int newWidth, int newHeight)
    {
        var channels = 0;

        switch(colorComponents)
        {
            case StandardTextureColorComponents.RGBA:

                channels = 4;

                break;

            case StandardTextureColorComponents.RGB:

                channels = 3;

                break;

            case StandardTextureColorComponents.GreyscaleAlpha:

                channels = 2;

                break;

            case StandardTextureColorComponents.Greyscale:

                channels = 1;

                break;
        }

        var newData = new byte[newWidth * newHeight * channels];

        unsafe
        {
            fixed(void *dataPtr = data)
            {
                fixed(void *newDataPtr = newData)
                {
                    if (StbImageResize.stbir_resize(dataPtr, width, height, width * channels, newDataPtr, newWidth, newHeight, newWidth * channels,
                        StbImageResize.stbir_datatype.STBIR_TYPE_UINT8, channels, 3, StbImageResize.stbir__resize_flag.None,
                        StbImageResize.stbir_edge.STBIR_EDGE_CLAMP, StbImageResize.stbir_edge.STBIR_EDGE_CLAMP,
                        StbImageResize.stbir_filter.STBIR_FILTER_BOX, StbImageResize.stbir_filter.STBIR_FILTER_BOX,
                        StbImageResize.stbir_colorspace.STBIR_COLORSPACE_SRGB) == 1)
                    {
                        data = newData;
                        width = newWidth;
                        height = newHeight;

                        return true;
                    }
                }
            }

        }

        return false;
    }

    public byte[] EncodePNG()
    {
        ColorComponents components;

        switch(colorComponents)
        {
            case StandardTextureColorComponents.RGBA:

                components = ColorComponents.RedGreenBlueAlpha;

                break;

            case StandardTextureColorComponents.RGB:

                components = ColorComponents.RedGreenBlue;

                break;

            case StandardTextureColorComponents.Greyscale:

                components = ColorComponents.Grey;

                break;

            case StandardTextureColorComponents.GreyscaleAlpha:

                components = ColorComponents.GreyAlpha;

                break;

            default:

                return null;
        }

        using var stream = new MemoryStream();

        var writer = new ImageWriter();

        try
        {
            writer.WritePng(data, width, height, components, stream);
        }
        catch(Exception)
        {
            return null;
        }

        return stream.ToArray();
    }
}
