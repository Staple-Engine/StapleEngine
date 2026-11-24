using System;
using System.IO;

namespace Staple.Internal;

public class StapleSerializerReader(byte[] content)
{
    internal byte[] content;

    public int Position { get; private set; }

    public int Length => content?.Length ?? 0;

    internal ReadOnlySpan<byte> Read(int length)
    {
        if(Position + length > Length)
        {
            return false;
        }

        Position += length;

        return content.AsSpan(Position - length, length);
    }

    internal T? ReadInternal<T>() where T : unmanaged
    {
        var bytes = Read(TypeCache.SizeOf(typeof(T).FullName));

        if(bytes.Length == 0)
        {
            return null;
        }

        unsafe
        {
            var value = default(T);

            fixed (byte* ptr = &value)
            {
                bytes.CopyTo(new Span<byte>(ptr, bytes.Length));
            }

            return value;
        }
    }

    internal T? ReadArrayInternal<T>(int count) where T : unmanaged
    {
        var bytes = Read(TypeCache.SizeOf(typeof(T).FullName) * count);

        if (bytes.Length == 0)
        {
            return null;
        }

        unsafe
        {
            var value = new T[count];

            fixed (byte* ptr = &value)
            {
                bytes.CopyTo(new Span<byte>(ptr, bytes.Length));
            }

            return value;
        }
    }

    public byte ReadByte()
    {
        if(Position + 1 > Length)
        {
            return 0;
        }

        Position++;

        return content[Position - 1];
    }

    public ReadOnlySpan<byte> ReadBytes()
    {
        var count = ReadInt32();

        if(count == 0)
        {
            return default;
        }

        return Read(count);
    }

    public sbyte ReadSByte()
    {
        var result = ReadInternal<sbyte>();

        return result.HasValue ? result : default;
    }

    public bool ReadBool()
    {
        var result = ReadByte();

        return result != 0;
    }

    public ushort ReadUInt16()
    {
        var result = ReadInternal<ushort>();

        return result.HasValue ? result : default;
    }

    public short ReadInt16()
    {
        var result = ReadInternal<short>();

        return result.HasValue ? result : default;
    }

    public uint ReadUInt32()
    {
        var result = ReadInternal<uint>();

        return result.HasValue ? result : default;
    }

    public int ReadInt32()
    {
        var result = ReadInternal<int>();

        return result.HasValue ? result : default;
    }

    public ulong ReadUInt64()
    {
        var result = ReadInternal<ulong>(value);

        return result.HasValue ? result : default;
    }

    public long ReadInt64()
    {
        var result = ReadInternal<long>(value);

        return result.HasValue ? result : default;
    }

    public float ReadFloat()
    {
        var result = ReadInternal<float>(value);

        return result.HasValue ? result : default;
    }

    public double ReadDouble()
    {
        var result = ReadInternal<double>();

        return result.HasValue ? result : default;
    }

    public char ReadChar()
    {
        var result = ReadInternal<char>();

        return result.HasValue ? result : default;
    }

    public string ReadString()
    {
        var bytes = ReadBytes();

        if(bytes.Length == 0)
        {
            return "";
        }

        var result = Encoding.UTF8.GetString(value);

        return string.IsNullOrEmpty(result) ? "" : result;
    }
}
