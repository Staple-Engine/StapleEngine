using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Staple.Networking;

/// <summary>
/// TCP Socket Game Server
/// </summary>
public sealed class SocketGameServer : IGameServer
{
    internal const int VersionID = 1;

    private class ClientInfo
    {
        public NetworkBatcher networkBatcher = new();
        public long playerID;
        public Socket connection;
        public LocalClient localClient;
        public readonly List<byte> stream = [];
        public bool readingPacket = false;
        public uint expectedPacketSize = 0;
    }

    public event IGameServer.GameServerClientConnectionDelegate OnClientConnected;
    public event IGameServer.GameServerClientConnectionDelegate OnClientDisconnected;
    public event IGameServer.GameServerClientMessageReceivedDelegate OnClientMessageReceived;
    public event IGameServer.GameServerDisposedDelegate OnServerDisposed;

    private Socket listenSocket;

    internal const int MaxBufferSize = 4 * 1024 * 1024;
    private const double TimeBetweenBatches = 60 / 1000.0;

    private readonly List<ClientInfo> clients = [];

    private readonly ConcurrentQueue<Action> pendingActions = new();

    private int maxConnections;

    /// <summary>
    /// Internal counter for player IDs
    /// </summary>
    private long playerCounter = 0;

    public bool debuggingNetworkMessages = false;

    public NetworkCompressionType compressionType;

    private readonly Lock lockObject = new();
    private DateTime networkBatcherTimer = DateTime.Now;
    private bool useNetworkBatching = false;
    private static readonly byte[] receiveBuffer = new byte[MaxBufferSize];

    public void AddLocalClient(LocalClient client)
    {
        lock (lockObject)
        {
            clients.Add(new ClientInfo()
            {
                localClient = client,
                playerID = ++playerCounter,
            });

            OnClientConnected?.Invoke(playerCounter);

            client.Connected();
        }
    }

    public void AddLocalMessage(LocalClient client, ushort messageID, INetworkReader reader)
    {
        lock (lockObject)
        {
            var c = clients.FirstOrDefault(x => x.localClient == client);

            if (c != null)
            {
                pendingActions.Enqueue(() =>
                {
                    OnClientMessageReceived?.Invoke(c.playerID, messageID, reader);
                });
            }
        }
    }

    public void Create(int port, int maxConnections, bool useNetworkBatching)
    {
        this.useNetworkBatching = useNetworkBatching;
        this.maxConnections = maxConnections;

        listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            Blocking = false,
            NoDelay = true
        };

        listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));

        listenSocket.Listen(port);
    }

    public void DisconnectPlayer(long playerID)
    {
        lock (lockObject)
        {
            var client = clients.FirstOrDefault(x => x.playerID == playerID);

            if (client != null)
            {
                if (client.localClient != null)
                {
                    client.localClient.PerformDisconnect();
                }
                else
                {
                    ShutdownSocket(client.connection);
                }

                clients.Remove(client);
            }
        }
    }

    public void RemoveLocalClient(LocalClient client)
    {
        lock (lockObject)
        {
            var c = clients.FirstOrDefault(x => x.localClient == client);

            if (c != null)
            {
                pendingActions.Enqueue(() =>
                {
                    OnClientDisconnected?.Invoke(c.playerID);

                    clients.Remove(c);
                });
            }
        }
    }

    public void SendMessageToPlayer(long playerID, ushort messageID, INetworkMessage message)
    {
        lock (lockObject)
        {
            var client = clients.FirstOrDefault(x => x.playerID == playerID);

            if (client != null)
            {
                var writer = new ByteArrayPacketWriter();

                try
                {
                    message.Serialize(writer);
                }
                catch (Exception e)
                {
                    Log.Error($"[{GetType().Name}] Failed to serialize message {messageID}: {e}");

                    return;
                }

                var messageData = writer.ToArray();

                if (client.localClient != null)
                {
                    client.localClient.Message(messageID, new ByteArrayPacketReader(messageData));
                }
                else if (useNetworkBatching)
                {
                    if (debuggingNetworkMessages)
                    {
                        Log.Debug($"[{GetType().Name}] Batching message {messageID} to player {playerID} with size {messageData.Length}.");
                    }

                    if (!client.networkBatcher.Batch(messageID, messageData))
                    {
                        var outWriter = new ByteArrayPacketWriter();

                        outWriter.WriteUInt16((ushort)InternalMessages.BatchMessage);

                        var batchData = client.networkBatcher.Buffer.Take(client.networkBatcher.Length)
                                .Concat(NetworkBatcher.Encode(messageID, messageData))
                                .ToArray();

                        batchData = NetworkCompression.Compress(batchData, compressionType);

                        outWriter.WriteBytesAndSize(batchData, batchData.Length);

                        client.networkBatcher.Clear();

                        messageData = outWriter.ToArray();

                        SendMessage(client.connection, messageData);
                    }
                }
                else
                {
                    var outWriter = new ByteArrayPacketWriter();

                    outWriter.WriteUInt16(messageID);

                    messageData = NetworkCompression.Compress(writer.ToArray(), compressionType);

                    outWriter.WriteBytesAndSize(messageData, messageData.Length);

                    messageData = outWriter.ToArray();

                    SendMessage(client.connection, messageData);
                }
            }
        }
    }

    public void SendBatchMessages()
    {
        lock (lockObject)
        {
            foreach (var clientInfo in clients)
            {
                if (clientInfo.localClient == null && clientInfo.networkBatcher.Length > 0)
                {
                    var writer = new ByteArrayPacketWriter();

                    var data = clientInfo.networkBatcher.Buffer.Take(clientInfo.networkBatcher.Length).ToArray();

                    var outData = NetworkCompression.Compress(data, compressionType);

                    writer.WriteUInt16((ushort)InternalMessages.BatchMessage);

                    writer.WriteBytesAndSize(outData, outData.Length);

                    clientInfo.networkBatcher.Clear();

                    var messageData = writer.ToArray();

                    SendMessage(clientInfo.connection, messageData);
                }
            }
        }
    }

    public void Shutdown()
    {
        lock (lockObject)
        {
            foreach (var client in clients)
            {
                if (client.localClient != null)
                {
                    client.localClient.PerformDisconnect();
                }
                else
                {
                    ShutdownSocket(client.connection);
                }
            }

            clients.Clear();
        }

        ShutdownSocket(listenSocket);

        OnServerDisposed?.Invoke();
    }

    public void Update()
    {
        CheckNewConnection();

        var pendingMessages = new Dictionary<long, List<INetworkReader>>();

        lock (lockObject)
        {
            foreach (var client in clients)
            {
                if (client.localClient != null)
                {
                    continue;
                }

                if(!SocketIsConnected(client.connection))
                {
                    DisconnectPlayer(client.playerID);

                    pendingActions.Enqueue(() =>
                    {
                        OnClientDisconnected?.Invoke(client.playerID);
                    });

                    break;
                }

                var message = ReadMessages(client.connection);

                if (message != null)
                {
                    if(!pendingMessages.TryGetValue(client.playerID, out var messages))
                    {
                        messages = [];

                        pendingMessages.Add(client.playerID, messages);
                    }

                    messages.Add(message);
                }
            }
        }

        foreach (var pair in pendingMessages)
        {
            foreach (var reader in pair.Value)
            {
                try
                {
                    var messageID = reader.ReadUInt16();

                    var message = NetworkCompression.Decompress(reader.ReadBytesAndSize(reader.Length - reader.Position), compressionType);

                    pendingActions.Enqueue(() =>
                    {
                        OnClientMessageReceived?.Invoke(pair.Key, messageID, new ByteArrayPacketReader(message));
                    });
                }
                catch (Exception e)
                {
                    Log.Error($"[{GetType().Name}] Failed to process messages for player {pair.Key}: {e}");
                }
            }
        }

        if (useNetworkBatching)
        {
            lock (lockObject)
            {
                if ((DateTime.Now - networkBatcherTimer).TotalSeconds >= TimeBetweenBatches)
                {
                    networkBatcherTimer = DateTime.Now;

                    SendBatchMessages();
                }
            }
        }

        while (pendingActions.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"[{GetType().Name}] Failed to execute a pending action: {e}");
            }
        }
    }

    internal void CheckNewConnection()
    {
        if(clients.Count >= maxConnections)
        {
            return;
        }

        Socket incomingSocket;

        try
        {
            incomingSocket = listenSocket.Accept();
        }
        catch(Exception)
        {
            return;
        }

        incomingSocket.Blocking = false;

        if(incomingSocket != null)
        {
            lock (lockObject)
            {
                clients.Add(new ClientInfo()
                {
                    connection = incomingSocket,
                    playerID = ++playerCounter,
                });

                var playerID = playerCounter;

                pendingActions.Enqueue(() =>
                {
                    OnClientConnected?.Invoke(playerID);
                });
            }
        }
    }

    internal static bool SocketIsConnected(Socket socket)
    {
        try
        {
            return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal static void ShutdownSocket(Socket socket)
    {
        if (socket == null)
        {
            return;
        }

        if (socket.Connected)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(false);
        }

        socket.Dispose();
    }

    internal INetworkReader ReadMessages(Socket connection)
    {
        ClientInfo client = null;

        lock(lockObject)
        {
            client = clients.FirstOrDefault(x => x.connection == connection);
        }

        if(client == null)
        {
            return null;
        }

        return ReadMessages(connection, ref client.readingPacket, ref client.expectedPacketSize, client.stream, () =>
        {
            lock (lockObject)
            {
                clients.Remove(client);
            }

            pendingActions.Enqueue(() =>
            {
                OnClientDisconnected?.Invoke(client.playerID);
            });
        });
    }

    internal static INetworkReader ReadMessages(Socket connection, ref bool readingPacket, ref uint expectedPacketSize, List<byte> stream, Action onFailure)
    {
        INetworkReader ProcessPacket(ref bool readingPacket, ref uint expectedPacketSize)
        {
            if (readingPacket)
            {
                if (stream.Count < expectedPacketSize)
                {
                    return null;
                }
                else if (stream.Count >= expectedPacketSize)
                {
                    var data = stream.Take((int)expectedPacketSize).ToArray();

                    stream.RemoveRange(0, data.Length);

                    readingPacket = false;

                    var reader = new ByteArrayPacketReader(data);

                    return reader;
                }
            }
            else if (stream.Count >= 8)
            {
                var validHeader = stream[0] == 'F' && stream[1] == 'T' && stream[2] == 'G' &&
                    stream[3] == VersionID;

                if (validHeader)
                {
                    var packetSize = BitConverter.ToUInt32(stream.Skip(4).Take(4).ToArray());

                    if (packetSize > 0 && packetSize < MaxBufferSize)
                    {
                        stream.RemoveRange(0, 8);

                        if (stream.Count >= packetSize)
                        {
                            var data = stream.Take((int)packetSize).ToArray();

                            stream.RemoveRange(0, data.Length);

                            var reader = new ByteArrayPacketReader(data);

                            return reader;
                        }
                        else
                        {
                            readingPacket = true;
                            expectedPacketSize = packetSize;
                        }
                    }
                    else
                    {
                        Log.Error($"[SocketGameServer] Client tried to send invalid packet size {packetSize}, terminating connection...");

                        ShutdownSocket(connection);

                        onFailure?.Invoke();
                    }
                }
                else
                {
                    Log.Error($"[SocketGameServer] Client sent invalid packet header, terminating connection...");

                    ShutdownSocket(connection);

                    onFailure?.Invoke();
                }
            }

            return null;
        }

        var count = 0;

        try
        {
            count = connection.Receive(receiveBuffer, SocketFlags.None);
        }
        catch(Exception)
        {
        }

        if(count > 0)
        {
            stream.AddRange(receiveBuffer.Take(count));

            return ProcessPacket(ref readingPacket, ref expectedPacketSize);
        }

        return ProcessPacket(ref readingPacket, ref expectedPacketSize);
    }

    internal void SendMessage(Socket connection, byte[] data)
    {
        SendMessage(connection, data, GetType().Name, () =>
        {
            ClientInfo client = null;

            lock (lockObject)
            {
                client = clients.FirstOrDefault(x => x.connection == connection);

                if (client != null)
                {
                    clients.Remove(client);
                }
            }

            if (client != null)
            {
                pendingActions.Enqueue(() =>
                {
                    OnClientDisconnected?.Invoke(client.playerID);
                });
            }
        });
    }

    internal static void SendMessage(Socket connection, byte[] data, string logPrefix, Action onFailure)
    {
        var counter = 0;

        var actualData = new byte[data.Length + 8];

        actualData[0] = (byte)'F';
        actualData[1] = (byte)'T';
        actualData[2] = (byte)'G';
        actualData[3] = VersionID;

        var countBytes = BitConverter.GetBytes(data.Length);

        if(countBytes.Length != 4)
        {
            Log.Error($"[{logPrefix}] Somehow failing to get 4 bytes for encoding packet length!");

            onFailure?.Invoke();

            return;
        }

        actualData[4] = countBytes[0];
        actualData[5] = countBytes[1];
        actualData[6] = countBytes[2];
        actualData[7] = countBytes[3];

        for(var i = 0; i < data.Length; i++)
        {
            actualData[i + 8] = data[i];
        }

        while(counter < actualData.Length)
        {
            try
            {
                counter += connection.Send(actualData, counter, actualData.Length - counter, SocketFlags.None);
            }
            catch(Exception)
            {
                Log.Error($"[{logPrefix}] Failed to send a message to a connection, terminating connection...");

                ShutdownSocket(connection);

                onFailure?.Invoke();

                return;
            }
        }
    }
}
