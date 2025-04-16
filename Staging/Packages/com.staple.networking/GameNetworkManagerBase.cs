namespace Staple.Networking;

/// <summary>
/// Manages a network server and optionally a client
/// </summary>
[AbstractComponent]
public class GameNetworkManagerBase : CallbackComponent
{
    /// <summary>
    /// The game's port
    /// </summary>
    public int gamePort = 2048;

    /// <summary>
    /// The game's max connections
    /// </summary>
    public int maxConnections = 4;

    /// <summary>
    /// Whether to create a self host server/client when this starts
    /// </summary>
    public bool connectToSelfOnStart = false;

    /// <summary>
    /// Whether we shouldn't destroy on load
    /// </summary>
    public bool dontDestroyOnLoad = true;

    /// <summary>
    /// The game server instance
    /// </summary>
    public IGameServer server;

    /// <summary>
    /// The game client instance
    /// </summary>
    public IGameClient client;

    /// <summary>
    /// The manager's instance, accessible from a static
    /// </summary>
    public static GameNetworkManagerBase Instance
    {
        get;
        private set;
    }

    /// <summary>
    /// The Awake method. Can be overwritten if necessary.
    /// </summary>
    public override void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if(dontDestroyOnLoad)
            {
                //TODO
            }
        }
        else
        {
            entity.Destroy();
        }
    }

    /// <summary>
    /// The Start method. Can be overwritten if necessary.
    /// </summary>
    public override void Start()
    {
        if(connectToSelfOnStart)
        {
            ConnectToSelf(maxConnections);
        }
    }

    /// <summary>
    /// Destroys the client and server
    /// </summary>
    public override void OnDestroy()
    {
        CloseClient();
        CloseServer();
    }

    /// <summary>
    /// Closes the client instance, disconnecting it
    /// </summary>
    public void CloseClient()
    {
        if (client != null)
        {
            client.Shutdown();
        }
    }

    /// <summary>
    /// Closes the server instance, disconnecting it
    /// </summary>
    public void CloseServer()
    {
        if (server != null)
        {
            server.Shutdown();
        }
    }

    /// <summary>
    /// Creates a client instance. Override to add your own implementation.
    /// </summary>
    public virtual void CreateClient()
    {
        CloseClient();
    }

    /// <summary>
    /// Creates a server instance. Override to add your own implementation. This call will make the server start immediately.
    /// </summary>
    /// <param name="port">The port to listen to</param>
    /// <param name="maxConnections">The maximum amount of connections for the server</param>
    public virtual void CreateServer(int port, int maxConnections)
    {
        CloseServer();
    }

    /// <summary>
    /// Creates a server and client and connects them to each other
    /// </summary>
    /// <param name="maxConnections">The maximum amount of connections on the server</param>
    public virtual void ConnectToSelf(int maxConnections)
    {
        CreateServer(gamePort, maxConnections);

        CreateClient();

        client?.Connect("localhost", gamePort);
    }

    /// <summary>
    /// Creates a client and connects to a host using the game port.
    /// </summary>
    /// <param name="host">The host to connect to</param>
    public virtual void Connect(string host)
    {
        CreateClient();

        client?.Connect(host, gamePort);
    }

    /// <summary>
    /// The update event. Might be overwritten. Should call `base.Update();` at some point.
    /// </summary>
    public override void Update()
    {
        server?.Update();

        client?.Update();
    }

    public override void FixedUpdate()
    {
        client?.FixedUpdate();
    }
}
