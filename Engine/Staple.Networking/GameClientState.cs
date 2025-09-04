namespace Staple.Networking;

/// <summary>
/// Possible states for the game client
/// </summary>
public enum GameClientState
{
    /// <summary>
    /// Client is stationary
    /// </summary>
    None,
    /// <summary>
    /// Client errored, should disconnect if possible
    /// </summary>
    Error,
    /// <summary>
    /// Currently connecting to the server
    /// </summary>
    Connecting,
    /// <summary>
    /// Currently disconnected
    /// </summary>
    Disconnected,
    /// <summary>
    /// Currently active. Messages can be freely sent around.
    /// </summary>
    Active
}