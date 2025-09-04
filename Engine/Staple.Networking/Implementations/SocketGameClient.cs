using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Staple.Networking;

/// <summary>
/// TCP Socket Game Client
/// </summary>
public sealed class SocketGameClient : IGameClient
{
    public event IGameClient.GameClientConnectionDelegate OnConnected;
    public event IGameClient.GameClientConnectionDelegate OnDisconnected;
    public event IGameClient.GameClientMessageDelegate OnMessageReceived;

    public GameClientState state { get; private set; } = GameClientState.None;

    public long playerID { get; private set; }

    public bool debuggingNetworkMessages = false;
    public NetworkCompressionType compressionType;

    private Socket socket;
    private readonly List<byte> stream = [];
    private bool readingPacket = false;
    private uint expectedPacketSize = 0;
    private bool connecting = false;
    private Thread connectThread = null;
    private readonly ConcurrentQueue<Action> pendingActions = [];

    public void Connect(string host, int port)
    {
        if(connecting)
        {
            return;
        }

        connecting = true;

        state = GameClientState.Connecting;

        connectThread = new Thread(new ThreadStart(() =>
        {
            try
            {
                var addresses = Dns.GetHostAddresses(host).Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToList();

                if (addresses.Count != 0)
                {
                    state = GameClientState.Connecting;

                    void Try()
                    {
                        if (addresses.Count == 0)
                        {
                            pendingActions.Enqueue(() =>
                            {
                                Disconnect();
                                state = GameClientState.Error;
                                OnDisconnected?.Invoke();
                            });

                            return;
                        }

                        var address = addresses[0];

                        addresses.RemoveAt(0);

                        try
                        {
                            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                            socket.Connect(address, port);
                        }
                        catch (Exception)
                        {
                            Try();

                            return;
                        }

                        socket.Blocking = false;
                        socket.NoDelay = true;

                        state = GameClientState.Active;

                        pendingActions.Enqueue(() =>
                        {
                            OnConnected?.Invoke();
                        });
                    }

                    Try();

                    if (state != GameClientState.Active)
                    {
                        pendingActions.Enqueue(() =>
                        {
                            Disconnect();
                            state = GameClientState.Error;
                            OnDisconnected?.Invoke();
                        });
                    }
                }
                else
                {
                    pendingActions.Enqueue(() =>
                    {
                        Disconnect();
                        state = GameClientState.Error;
                        OnDisconnected?.Invoke();
                    });
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[{GetType().Name}] Exception while attempting to connect: {e}");

                pendingActions.Enqueue(() =>
                {
                    Disconnect();
                    state = GameClientState.Error;
                    OnDisconnected?.Invoke();
                });
            }
            finally
            {
                connecting = false;
            }
        }));

        connectThread.Start();
    }

    public void Create()
    {
        stream.Clear();

        readingPacket = false;
    }

    public void Disconnect()
    {
        if(socket != null)
        {
            SocketGameServer.ShutdownSocket(socket);
        }

        stream.Clear();
        readingPacket = false;

        state = GameClientState.Disconnected;

        OnDisconnected?.Invoke();
    }

    public void FixedUpdate()
    {
    }

    public void SendMessage(ushort messageID, INetworkMessage message)
    {
        if (state != GameClientState.Active)
        {
            return;
        }

        var writer = new ByteArrayPacketWriter();
        var tempWriter = new ByteArrayPacketWriter();

        writer.WriteUInt16(messageID);

        try
        {
            message.Serialize(tempWriter);
        }
        catch (Exception e)
        {
            Log.Error($"[{GetType().Name}] Failed to serialize message {messageID}: {e}");

            return;
        }

        var data = NetworkCompression.Compress(tempWriter.ToArray(), compressionType);

        writer.WriteBytesAndSize(data, data.Length);

        SocketGameServer.SendMessage(socket, writer.ToArray(), GetType().Name, () =>
        {
            Disconnect();
        });
    }

    public void Shutdown()
    {
        SocketGameServer.ShutdownSocket(socket);

        state = GameClientState.Disconnected;
    }

    public void Update()
    {
        while (pendingActions.TryDequeue(out var action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"[{GetType().Name}]Failed to execute a pending action: {e}");
            }
        }

        if (state == GameClientState.Disconnected || state == GameClientState.Connecting || state == GameClientState.Error)
        {
            return;
        }

        if(SocketGameServer.SocketIsConnected(socket) == false)
        {
            Disconnect();

            return;
        }

        var message = SocketGameServer.ReadMessages(socket, ref readingPacket, ref expectedPacketSize, stream, () =>
        {
            Disconnect();
        });

        if (message != null)
        {
            try
            {
                var messageID = message.ReadUInt16();
                var messageData = message.ReadBytesAndSize(message.Length - message.Position);

                messageData = NetworkCompression.Decompress(messageData, compressionType);

                if (messageID == (uint)InternalMessages.BatchMessage)
                {
                    var batchMessages = new List<NetworkBatcher.Message>(new NetworkBatcher.BatchWalker(messageData));

                    foreach (var m in batchMessages)
                    {
                        var reader = new ByteArrayPacketReader(m.data);

                        pendingActions.Enqueue(() =>
                        {
                            try
                            {
                                OnMessageReceived?.Invoke(m.ID, reader);
                            }
                            catch (Exception e)
                            {
                                Log.Error($"[{GetType().Name}] Failed to handle message {m.ID} with {m.data.Length} bytes: {e}");
                            }
                        });
                    }
                }
                else
                {
                    OnMessageReceived?.Invoke(messageID, new ByteArrayPacketReader(messageData));
                }
            }
            catch (Exception e)
            {
                Log.Error($"[{GetType().Name}] Failed to deserialize a message: {e}");
            }
        }
    }
}
