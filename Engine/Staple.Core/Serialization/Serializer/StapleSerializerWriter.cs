using System;
using System.IO;

namespace Staple.Internal;

public class StapleSerializerWriter(MemoryStream content)
{
    internal MemoryStream content = content;

    public int Length => content?.Length ?? 0;

    public byte[] ToArray() => content?.ToArray() ?? [];

    internal void Write(ReadOnlySpan<byte> data)
    {
        content.Write(data);
    }

    internal void WriteInternal<T>(T value) where T : unmanaged
    {
        unsafe
        {
            fixed (byte* ptr = &value)
            {
                Write(new ReadOnlySpan<byte>(ptr, TypeCache.SizeOf(typeof(T).FullName)));
            }
        }
    }

    public void WriteByte(byte value)
    {
        content.Write(value);
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        WriteInt32(data.Length);
        Write(data);
    }

    public void WriteSByte(sbyte value)
    {
        WriteInternal(value);
    }

    public void WriteBool(bool value)
    {
        WriteInternal((byte)(value ? 1 : 0));
    }

    public void WriteUInt16(ushort value)
    {
        WriteInternal(value);
    }

    public void WriteInt16(short value)
    {
        WriteInternal(value);
    }

    public void WriteUInt32(uint value)
    {
        WriteInternal(value);
    }

    public void WriteInt32(int value)
    {
        WriteInternal(value);
    }

    public void WriteUInt64(ulong value)
    {
        WriteInternal(value);
    }

    public void WriteInt64(long value)
    {
        WriteInternal(value);
    }

    public void WriteFloat(float value)
    {
        WriteInternal(value);
    }

    public void WriteDouble(double value)
    {
        WriteInternal(value);
    }

    public void WriteChar(char value)
    {
        WriteInternal(value);
    }

    public void WriteString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteInt32(0);

            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);

        WriteBytes(bytes);
    }
}
