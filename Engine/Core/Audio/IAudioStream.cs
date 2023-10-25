using System;

namespace Staple
{
    internal interface IAudioStream
    {
        int Channels { get; }

        int SampleRate { get; }

        int BitsPerSample { get; }

        TimeSpan TotalTime { get; }

        TimeSpan CurrentTime { get; }

        int Read(short[] buffer, int count);

        short[] ReadAll();

        void Open();

        void Close();
    }
}
