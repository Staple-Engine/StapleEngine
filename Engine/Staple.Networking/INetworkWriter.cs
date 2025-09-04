namespace Staple.Networking;

/// <summary>
/// Represents a writer for network messages
/// </summary>
public interface INetworkWriter
{
    int Length { get; }

    int Position { get; }

    void WriteChar(char value);

    void WriteByte(byte value);

    void WriteSByte(sbyte value);

    void WriteBool(bool value);

    void WriteInt16(short value);

    void WriteUInt16(ushort value);

    void WriteInt32(int value);

    void WriteUInt32(uint value);

    void WriteInt64(long value);

    void WriteUInt64(ulong value);

    void WriteFloat(float value);

    void WriteDouble(double value);

    void WriteString(string value);

    void WriteBytes(byte[] value);

    void WriteBytesAndSize(byte[] value, int length);

    byte[] ToArray();
}
