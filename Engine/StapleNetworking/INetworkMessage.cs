namespace Staple.Networking;

/// <summary>
/// Represents a network message
/// </summary>
public interface INetworkMessage
{
    /// <summary>
    /// Serializes this message's contents into a network writer
    /// </summary>
    /// <param name="writer">The writer</param>
    void Serialize(INetworkWriter writer);

    /// <summary>
    /// Deserializes this message's contents from a network reader
    /// </summary>
    /// <param name="reader">The reader</param>
    void Deserialize(INetworkReader reader);
}
