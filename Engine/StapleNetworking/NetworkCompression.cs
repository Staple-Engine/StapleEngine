namespace Staple.Networking;

/// <summary>
/// Type of network compression
/// </summary>
public enum NetworkCompressionType
{
    None,
}

/// <summary>
/// Handles network compression and decompression
/// </summary>
public static class NetworkCompression
{
    public static byte[] Compress(byte[] data, NetworkCompressionType compressionType)
    {
        switch(compressionType)
        {
            case NetworkCompressionType.None:

                return data;

            default:

                return data;
        }
    }

    public static byte[] Decompress(byte[] data, NetworkCompressionType compressionType)
    {
        switch(compressionType)
        {
            case NetworkCompressionType.None:

                return data;

            default:

                return data;
        }
    }
}