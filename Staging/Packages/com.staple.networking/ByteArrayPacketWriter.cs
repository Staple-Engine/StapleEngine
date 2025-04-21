using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Staple.Networking;

/// <summary>
/// Writes packet data to a byte array
/// </summary>
public class ByteArrayPacketWriter : INetworkWriter
{
    private readonly List<byte> buffer = [];

    private int position;

    public int Length => buffer.Count;

    public int Position => position;

    public byte[] ToArray()
    {
        return buffer.ToArray();
    }

    public void WriteBool(bool value)
    {
        position++;

        buffer.Add((byte)(value ? 1 : 0));
    }

    public void WriteByte(byte value)
    {
        position++;

        buffer.Add(value);
    }

    public void WriteBytes(byte[] value)
    {
        WriteUInt32((uint)value.Length);

        position += value.Length;

        buffer.AddRange(value);
    }

    public void WriteBytesAndSize(byte[] value, int length)
    {
        position += length;

        buffer.AddRange(value.Take(length));
    }

    public void WriteChar(char value)
    {
        var t = Encoding.UTF8.GetBytes(new char[] { value });

        WriteUInt16((ushort)t.Length);

        position += t.Length;

        buffer.AddRange(t);
    }

    public void WriteDouble(double value)
    {
        var bytes = BitConverter.GetBytes(value);

        position += bytes.Length;

        buffer.AddRange(bytes);
    }

    public void WriteFloat(float value)
    {
        var bytes = BitConverter.GetBytes(value);

        position += bytes.Length;

        buffer.AddRange(bytes);
    }

    public void WriteInt16(short value)
    {
        var bytes = BitConverter.GetBytes(value);

        position += bytes.Length;

        buffer.AddRange(bytes);
    }

    public void WriteInt32(int value)
    {
        var bytes = BitConverter.GetBytes(value);

        position += bytes.Length;

        buffer.AddRange(bytes);
    }

    public void WriteInt64(long value)
    {
        var bytes = BitConverter.GetBytes(value);

        position += bytes.Length;

        buffer.AddRange(bytes);
    }

    public void WriteSByte(sbyte value)
    {
        position++;

        buffer.Add((byte)value);
    }

    public void WriteString(string value)
    {
        if(value == null)
        {
            WriteBytes([]);

            return;
        }

        var t = Encoding.UTF8.GetBytes(value);

        WriteUInt16((ushort)t.Length);

        WriteBytesAndSize(t, t.Length);
    }

    public void WriteUInt16(ushort value)
    {
        var bytes = BitConverter.GetBytes(value);

        position += bytes.Length;

        buffer.AddRange(bytes);
    }

    public void WriteUInt32(uint value)
    {
        var bytes = BitConverter.GetBytes(value);

        position += bytes.Length;

        buffer.AddRange(bytes);
    }

    public void WriteUInt64(ulong value)
    {
        var bytes = BitConverter.GetBytes(value);

        position += bytes.Length;

        buffer.AddRange(bytes);
    }
}
