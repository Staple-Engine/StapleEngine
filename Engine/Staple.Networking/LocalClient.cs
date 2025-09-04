using System;

namespace Staple.Networking;

/// <summary>
/// Local Game Client that uses no networking
/// </summary>
public class LocalClient(IGameServer server) : IGameClient
{
    public GameClientState state
    {
        get;
        protected set;
    } = GameClientState.None;

    public long playerID
    {
        get;
        protected set;
    } = 0;

    public event IGameClient.GameClientConnectionDelegate OnConnected;
    public event IGameClient.GameClientConnectionDelegate OnDisconnected;
    public event IGameClient.GameClientMessageDelegate OnMessageReceived;

    public bool debuggingNetworkMessages = false;

    public IGameServer server = server;

    public void Create()
    {
    }

    public void Connect(string host, int port)
    {
        server.AddLocalClient(this);
    }

    public void Disconnect()
    {
        if (state != GameClientState.Active)
        {
            return;
        }

        server.RemoveLocalClient(this);
    }

    public void SendMessage(ushort messageID, INetworkMessage message)
    {
        var writer = new ByteArrayPacketWriter();

        try
        {
            message.Serialize(writer);
        }
        catch (Exception e)
        {
            Log.Error($"[LocalClient] Failed to serialize message {messageID}: {e}");

            return;
        }

        var data = writer.ToArray();

        var reader = new ByteArrayPacketReader(data);

        server.AddLocalMessage(this, messageID, reader);
    }

    public void Update()
    {
    }

    public void FixedUpdate()
    {
    }

    public void Shutdown()
    {
    }

    #region Internal Handlers
    public void Connected()
    {
        state = GameClientState.Active;

        OnConnected?.Invoke();
    }

    public void Message(ushort messageID, INetworkReader reader)
    {
        OnMessageReceived?.Invoke(messageID, reader);
    }

    public void PerformDisconnect()
    {
        state = GameClientState.Disconnected;

        OnDisconnected?.Invoke();
    }
    #endregion
}
