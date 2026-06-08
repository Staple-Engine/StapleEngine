namespace Staple.Networking;

/// <summary>
/// Game Client base for dealing with synchronizing server messages
/// </summary>
public interface IGameClient
{
    public delegate void GameClientMessageDelegate(ushort messageID, INetworkReader reader);
    public delegate void GameClientConnectionDelegate();

    /// <summary>
    /// The client's current state
    /// </summary>
    GameClientState state { get; }

    /// <summary>
    /// Local Player ID
    /// </summary>
    long playerID { get; }

    /// <summary>
    /// Called when we are connected
    /// </summary>
    event GameClientConnectionDelegate OnConnected;

    /// <summary>
    /// Called when we are disconnected
    /// </summary>
    event GameClientConnectionDelegate OnDisconnected;

    /// <summary>
    /// Called when we receive a message
    /// </summary>
    event GameClientMessageDelegate OnMessageReceived;

    /// <summary>
    /// Creates an internal client instance.
    /// </summary>
    void Create();

    /// <summary>
    /// Attempts to connect to a server
    /// </summary>
    /// <param name="host">The host address or IP</param>
    /// <param name="port">The port to connect to</param>
    void Connect(string host, int port);

    /// <summary>
    /// Updates the client. Optional.
    /// </summary>
    void Update();

    /// <summary>
    /// Updates the client in fixed tick time. Optional.
    /// </summary>
    void FixedUpdate();

    /// <summary>
    /// Disconnects the client from the server
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Shuts down the client system
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Sends a message to the server
    /// </summary>
    /// <param name="messageID">The message's ID</param>
    /// <param name="message">The message's contents</param>
    void SendMessage(ushort messageID, INetworkMessage message);
}
