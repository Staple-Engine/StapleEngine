using OggVorbisEncoder;
using System.IO;
using System;

namespace Baker;

public static class AudioUtils
{
    public static byte[] EncodeOGG(Span<float> samples, int channels, int sampleRate, float quality)
    {
        var splitSamples = new float[channels][];

        if (channels == 1)
        {
            splitSamples[0] = samples.ToArray();
        }
        else
        {
            for (var k = 0; k < channels; k++)
            {
                splitSamples[k] = new float[samples.Length / channels];
            }

            for (int k = 0, l = 0; k < samples.Length; k += channels, l++)
            {
                for (var c = 0; c < channels; c++)
                {
                    splitSamples[c][l] = samples[k];
                }
            }
        }

        using var outStream = new MemoryStream();

        var info = VorbisInfo.InitVariableBitRate(channels, sampleRate, quality);

        var serial = new Random().Next();

        var oggStream = new OggStream(serial);

        var comments = new Comments();

        var infoPacket = HeaderPacketBuilder.BuildInfoPacket(info);
        var commentsPacket = HeaderPacketBuilder.BuildCommentsPacket(comments);
        var booksPacket = HeaderPacketBuilder.BuildBooksPacket(info);

        oggStream.PacketIn(infoPacket);
        oggStream.PacketIn(commentsPacket);
        oggStream.PacketIn(booksPacket);

        void FlushPages(bool force)
        {
            while (oggStream.PageOut(out var page, force))
            {
                outStream.Write(page.Header, 0, page.Header.Length);
                outStream.Write(page.Body, 0, page.Body.Length);
            }
        }

        FlushPages(true);

        var processingState = ProcessingState.Create(info);

        processingState.WriteData(splitSamples, samples.Length / channels, 0);
        processingState.WriteEndOfStream();

        while (oggStream.Finished == false && processingState.PacketOut(out var packet))
        {
            oggStream.PacketIn(packet);

            FlushPages(false);
        }

        FlushPages(true);

        return outStream.ToArray();
    }
}
