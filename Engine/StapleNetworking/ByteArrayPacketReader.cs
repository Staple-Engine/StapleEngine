using System;
using System.Linq;
using System.Text;

namespace Staple.Networking;

/// <summary>
/// Reads packet data from a byte array
/// </summary>
public class ByteArrayPacketReader(byte[] data) : INetworkReader
{
    private readonly byte[] data = data;
    private int position;

    public int Length => data?.Length ?? 0;

    public int Position => position;

    public bool ReadBool()
    {
        if(position + 1 > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        return data[position++] != 0;
    }

    public byte ReadByte()
    {
        if (position + 1 > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        return data[position++];
    }

    public byte[] ReadBytes()
    {
        var length = ReadUInt32();

        return ReadBytesAndSize((int)length);
    }

    public byte[] ReadBytesAndSize(int length)
    {
        if (position + length > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var outValue = data.Skip(position).Take(length).ToArray();

        position += length;

        return outValue;
    }

    public char ReadChar()
    {
        var size = ReadUInt16();

        if (position + size > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var inValue = data.Skip(position).Take(size).ToArray();

        position += size;

        var character = BitConverter.ToChar(inValue);

        return character;
    }

    public double ReadDouble()
    {
        if (position + sizeof(double) > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var t = data.Skip(position).Take(sizeof(double)).ToArray();

        position += sizeof(double);

        return BitConverter.ToDouble(t);
    }

    public float ReadFloat()
    {
        if (position + sizeof(float) > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var t = data.Skip(position).Take(sizeof(float)).ToArray();

        position += sizeof(float);

        return BitConverter.ToSingle(t);
    }

    public short ReadInt16()
    {
        if (position + sizeof(short) > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var t = data.Skip(position).Take(sizeof(short)).ToArray();

        position += sizeof(short);

        return BitConverter.ToInt16(t);
    }

    public int ReadInt32()
    {
        if (position + sizeof(int) > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var t = data.Skip(position).Take(sizeof(int)).ToArray();

        position += sizeof(int);

        return BitConverter.ToInt32(t);
    }

    public long ReadInt64()
    {
        if (position + sizeof(long) > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var t = data.Skip(position).Take(sizeof(long)).ToArray();

        position += sizeof(long);

        return BitConverter.ToInt64(t);
    }

    public sbyte ReadSByte()
    {
        if (position + 1 > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        return (sbyte)data[position++];
    }

    public string ReadString()
    {
        var length = ReadUInt16();

        if(position + length > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var str = Encoding.UTF8.GetString(data, position, length);

        position += length;

        return str;
    }

    public ushort ReadUInt16()
    {
        if (position + sizeof(ushort) > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var t = data.Skip(position).Take(sizeof(ushort)).ToArray();

        position += sizeof(ushort);

        return BitConverter.ToUInt16(t);
    }

    public uint ReadUInt32()
    {
        if (position + sizeof(uint) > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var t = data.Skip(position).Take(sizeof(uint)).ToArray();

        position += sizeof(uint);

        return BitConverter.ToUInt32(t);
    }

    public ulong ReadUInt64()
    {
        if (position + sizeof(ulong) > Length)
        {
            throw new Exception("Exceeded packet length");
        }

        var t = data.Skip(position).Take(sizeof(ulong)).ToArray();

        position += sizeof(ulong);

        return BitConverter.ToUInt64(t);
    }
}