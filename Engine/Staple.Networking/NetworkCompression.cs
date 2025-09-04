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
    /// <summary>
    /// Compresses data
    /// </summary>
    /// <param name="data">The data to compress</param>
    /// <param name="compressionType">The compression type</param>
    /// <returns>The compressed data</returns>
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

    /// <summary>
    /// Decompresses data
    /// </summary>
    /// <param name="data">The data to decompress</param>
    /// <param name="compressionType">The compression type</param>
    /// <returns>The decompressed data</returns>
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