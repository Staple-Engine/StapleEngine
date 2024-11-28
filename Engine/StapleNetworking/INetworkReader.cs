namespace Staple.Networking;

/// <summary>
/// Represents a reader for network messages
/// </summary>
public interface INetworkReader
{
    int Length { get; }

    int Position { get; }

    char ReadChar();

    byte ReadByte();

    sbyte ReadSByte();

    bool ReadBool();

    short ReadInt16();

    ushort ReadUInt16();

    int ReadInt32();

    uint ReadUInt32();

    long ReadInt64();

    ulong ReadUInt64();

    float ReadFloat();

    double ReadDouble();

    string ReadString();

    byte[] ReadBytes();

    byte[] ReadBytesAndSize(int length);
}
