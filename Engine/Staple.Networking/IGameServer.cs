namespace Staple.Networking;

internal enum InternalMessages : ushort
{
    BatchMessage = ushort.MaxValue - 1,
}

/// <summary>
/// Authorative Game Server to deal with synchronizing clients
/// </summary>
public interface IGameServer
{
    public delegate void GameServerClientConnectionDelegate(long playerID);
    public delegate void GameServerClientMessageReceivedDelegate(long playerID, ushort messageID, INetworkReader reader);
    public delegate void GameServerDisposedDelegate();

    /// <summary>
    /// Called when a client connects to the server
    /// </summary>
    event GameServerClientConnectionDelegate OnClientConnected;

    /// <summary>
    /// Called when a client disconnects from the server
    /// </summary>
    event GameServerClientConnectionDelegate OnClientDisconnected;

    /// <summary>
    /// Called when we receive a message from a client
    /// </summary>
    event GameServerClientMessageReceivedDelegate OnClientMessageReceived;

    /// <summary>
    /// Called when the server has been disposed
    /// </summary>
    event GameServerDisposedDelegate OnServerDisposed;

    /// <summary>
    /// Creates the server instance. You should overwrite this to register your custom message handlers.
    /// </summary>
    /// <param name="port">The port to listne to</param>
    /// <param name="maxConnections">The maximum amount of connections</param>
    /// <param name="useNetworkBatching">Whether to use network batching.
    /// This can provide better network usage at the cost of a slight delay, while generally supporting more players.</param>
    void Create(int port, int maxConnections, bool useNetworkBatching);

    /// <summary>
    /// Shuts down the server, disconnecting all clients
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Updates the server and its connections. Optional.
    /// </summary>
    void Update();

    /// <summary>
    /// Sends a message to a player
    /// </summary>
    /// <param name="playerID">The player's ID</param>
    /// <param name="messageID">The message ID</param>
    /// <param name="message">The message data</param>
    /// <remarks>If the player's connection doesn't exist, it won't do anything</remarks>
    void SendMessageToPlayer(long playerID, ushort messageID, INetworkMessage message);

    /// <summary>
    /// Disconnects a player from the server
    /// </summary>
    /// <param name="playerID">The player's ID</param>
    void DisconnectPlayer(long playerID);

    /// <summary>
    /// Adds a local client. This client doesn't use networking.
    /// </summary>
    /// <param name="client">The client to add</param>
    void AddLocalClient(LocalClient client);

    /// <summary>
    /// Remove a local client
    /// </summary>
    /// <param name="client">The client to remove</param>
    void RemoveLocalClient(LocalClient client);

    /// <summary>
    /// Adds a message to a local client
    /// </summary>
    /// <param name="client">The client</param>
    /// <param name="messageID">The message ID</param>
    /// <param name="reader">The message data</param>
    void AddLocalMessage(LocalClient client, ushort messageID, INetworkReader reader);
}
