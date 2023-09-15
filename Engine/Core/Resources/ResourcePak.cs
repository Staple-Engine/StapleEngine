using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Internal
{
    public class ResourcePak : IDisposable
    {
        public readonly static char[] ValidHeader = new char[] { 'S', 'T', 'P', 'A', 'K' };
        public const byte ValidVersion = 1;

        [MessagePackObject]
        public class Header
        {
            [Key(0)]
            public char[] header = ValidHeader;

            [Key(1)]
            public byte version = ValidVersion;
        }

        [MessagePackObject]
        public class Entry
        {
            [Key(0)]
            public byte[] guid;

            [Key(1)]
            public string path;

            [Key(2)]
            public long size;

            [IgnoreMember]
            public Stream stream;
        }

        public class ResourceStream : Stream
        {
            internal Stream owner;
            internal long offset;
            internal long length;
            internal long position;

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length => length;

            public override long Position
            {
                get
                {
                    lock(fileLock)
                    {
                        return position;
                    }
                }

                set
                {
                    lock(fileLock)
                    {
                        if (value < 0 || value >= length)
                        {
                            return;
                        }

                        position = value;
                    }
                }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                lock(fileLock)
                {
                    owner.Position = this.offset + position;

                    if (position + count > length)
                    {
                        count = (int)(length - position);
                    }

                    var result = owner.Read(buffer, offset, count);

                    position += result;

                    return result;
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                lock(fileLock)
                {
                    switch (origin)
                    {
                        case SeekOrigin.Begin:

                            position = 0;

                            return 0;

                        case SeekOrigin.End:

                            position = length;

                            return length;

                        case SeekOrigin.Current:

                            position += offset;

                            if (position >= length)
                            {
                                position = length;
                            }

                            return length;
                    }
                }

                return 0;
            }

            public override void SetLength(long value)
            {
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
            }
        }

        public class FileInfo
        {
            public string guid;
            public string path;
            public long size;
        }

        private List<Entry> entries = new();
        private List<long> offsets = new();
        private List<FileInfo> files = new();
        private Stream backend;
        internal static object fileLock = new();

        public IEnumerable<FileInfo> Files => files;

        public void Clear()
        {
            entries.Clear();
        }

        public void AddEntry(string guid, string path, Stream stream)
        {
            if(Guid.TryParse(guid, out var _guid) == false)
            {
                _guid = Guid.NewGuid();
            }

            var entry = new Entry
            {
                guid = _guid.ToByteArray(),
                path = path,
                size = stream.Length,
                stream = stream,
            };

            entries.Add(entry);
        }

        public Stream Open(string path)
        {
            foreach(var entry in entries)
            {
                if(entry.path == path)
                {
                    return new ResourceStream()
                    {
                        owner = backend,
                        position = 0,
                        offset = offsets[entries.IndexOf(entry)],
                        length = entry.size,
                    };
                }
            }

            return null;
        }

        public Stream OpenGuid(byte[] guid)
        {
            foreach (var entry in entries)
            {
                if (entry.guid.SequenceEqual(guid))
                {
                    return new ResourceStream()
                    {
                        owner = backend,
                        position = 0,
                        offset = offsets[entries.IndexOf(entry)],
                        length = entry.size,
                    };
                }
            }

            return null;
        }

        public Stream OpenGuid(string guid)
        {
            if(Guid.TryParse(guid, out var _guid))
            {
                return OpenGuid(_guid.ToByteArray());
            }

            return null;
        }

        internal bool Serialize(Stream writer)
        {
            try
            {
                var headerBuffer = MessagePackSerializer.Serialize(new Header(), MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));

                var length = headerBuffer.Length;

                var bytes = BitConverter.GetBytes(length);

                writer.Write(bytes);

                writer.Write(headerBuffer);

                foreach (var entry in entries)
                {
                    var buffer = MessagePackSerializer.Serialize(entry, MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));

                    length = buffer.Length;

                    bytes = BitConverter.GetBytes(length);

                    writer.Write(bytes);

                    writer.Write(buffer);

                    entry.stream.CopyTo(writer);
                }
            }
            catch(Exception e)
            {
                return false;
            }

            return true;
        }

        internal bool Deserialize(Stream reader)
        {
            entries.Clear();
            offsets.Clear();
            files.Clear();

            try
            {
                var length = 0;

                var intBytes = new byte[sizeof(int)];

                if(reader.Read(intBytes) != sizeof(int))
                {
                    return false;
                }

                length = BitConverter.ToInt32(intBytes);

                var buffer = new byte[length];

                if(reader.Read(buffer) != length)
                {
                    return false;
                }

                var header = MessagePackSerializer.Deserialize<Header>(buffer, MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));

                if(header.header.SequenceEqual(ValidHeader) == false ||
                    header.version != ValidVersion)
                {
                    return false;
                }

                while (reader.Position < reader.Length)
                {
                    if(reader.Read(intBytes) != sizeof(int))
                    {
                        return false;
                    }

                    length = BitConverter.ToInt32(intBytes);

                    buffer = new byte[length];

                    if(reader.Read(buffer) != length)
                    {
                        return false;
                    }

                    var entry = MessagePackSerializer.Deserialize<Entry>(buffer, MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));

                    entries.Add(entry);
                    offsets.Add(reader.Position);

                    files.Add(new FileInfo()
                    {
                        guid = new Guid(entry.guid).ToString(),
                        path = entry.path,
                        size = entry.size,
                    });

                    reader.Position = reader.Position + entry.size;
                }
            }
            catch (Exception e)
            {
                return false;
            }

            backend = reader;

            return true;
        }

        public void Dispose()
        {
            if(backend != null)
            {
                backend.Dispose();
            }
        }
    }
}
