using Android.Content.Res;
using Android.OS;
using Staple.Internal;
using System.IO;

namespace Staple;

internal class AndroidPlatformProvider : IPlatformProvider
{
    public class AndroidAssetStream : Stream
    {
        private readonly Stream baseStream;
        private readonly long start;
        private readonly long length;
        private long position;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get => position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new System.NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new System.NotSupportedException();

        public override void Write(System.ReadOnlySpan<byte> buffer) => throw new System.NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(position >= length)
            {
                return 0;
            }

            if(position + count > length)
            {
                count = (int)(length - position);
            }

            var read = baseStream.Read(buffer, offset, count);

            position += read;

            return read;
        }

        public override int Read(System.Span<byte> buffer)
        {
            if (position >= length)
            {
                return 0;
            }

            var read = baseStream.Read(buffer);

            position += read;

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => position + offset,
                SeekOrigin.End => length + offset,
                _ => throw new System.ArgumentOutOfRangeException(nameof(origin), origin, null),
            };

            if(target < 0 || target > length)
            {
                throw new System.ArgumentOutOfRangeException(nameof(offset), "Invalid seek position");
            }

            baseStream.Seek(start + target, SeekOrigin.Begin);

            position = target;

            return position;
        }

        public AndroidAssetStream(AssetFileDescriptor descriptor)
        {
            baseStream = descriptor.CreateInputStream();
            start = descriptor.StartOffset;
            length = descriptor.Length;

            baseStream.Seek(start, SeekOrigin.Begin);
        }
    }

    internal AssetManager assetManager;

    public static readonly AndroidPlatformProvider Instance = new();

    public string StorageBasePath => Environment.ExternalStorageDirectory.AbsolutePath;

    public IRenderWindow CreateWindow() => AndroidRenderWindow.Instance;

    public void ConsoleLog(object message) => Android.Util.Log.Debug("Staple Engine", $"{message}");

    public Stream OpenFile(string path)
    {
        try
        {
            var s = assetManager.OpenFd(path);

            return new AndroidAssetStream(s);
        }
        catch (System.Exception)
        {
            var s = assetManager.Open(path);

            var stream = new MemoryStream();

            s.CopyTo(stream);

            stream.Position = 0;

            return stream;
        }
    }
}
