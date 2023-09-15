using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Internal
{
    internal class ResourcePak
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

        private List<Entry> entries = new();
        private List<long> offsets = new();
        private Stream backend;
        internal static object fileLock = new();

        public void Clear()
        {
            entries.Clear();
        }

        public void AddEntry(string guid, string path, Stream stream)
        {
            var entry = new Entry
            {
                guid = Enumerable.Range(0, guid.Length / 2)
                    .Select(x => Convert.ToByte(guid.Substring(x * 2, 2), 16))
                    .ToArray(),
                path = path,
                size = stream.Length,
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
            var bytes = Enumerable.Range(0, guid.Length / 2)
                .Select(x => Convert.ToByte(guid.Substring(x * 2, 2), 16))
                .ToArray();

            return OpenGuid(bytes);
        }

        public bool Serialize(Stream writer)
        {
            try
            {
                var headerBuffer = MessagePackSerializer.Serialize(new Header(), MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));

                writer.Write(headerBuffer);

                foreach (var entry in entries)
                {
                    var buffer = MessagePackSerializer.Serialize(entry, MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));

                    writer.Write(buffer);

                    entry.stream.CopyTo(writer);
                }
            }
            catch(Exception)
            {
                return false;
            }

            return true;
        }

        public bool Deserialize(Stream reader)
        {
            entries.Clear();

            try
            {
                var header = MessagePackSerializer.Deserialize<Header>(reader);

                if(header.header.SequenceEqual(ValidHeader) == false ||
                    header.version != ValidVersion)
                {
                    return false;
                }

                while (reader.Position < reader.Length)
                {
                    var entry = MessagePackSerializer.Deserialize<Entry>(reader);

                    entries.Add(entry);
                    offsets.Add(reader.Position);

                    reader.Position = reader.Position + (long)entry.size;
                }
            }
            catch (Exception)
            {
                return false;
            }

            backend = reader;

            return true;
        }
    }
}
