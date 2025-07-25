using RemoteControl.Protocol.Messages;

namespace RemoteControl.Transport.Interfaces;

/// <summary>
/// SignalR client interface for real-time communication
/// </summary>
public interface ISignalRClient
{
    /// <summary>
    /// Connection state
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Client identifier
    /// </summary>
    string? ClientId { get; }

    /// <summary>
    /// Connect to SignalR hub
    /// </summary>
    Task ConnectAsync(string hubUrl, string? accessToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from SignalR hub
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to the hub
    /// </summary>
    Task SendMessageAsync(MessageBase message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to a specific client
    /// </summary>
    Task SendMessageToClientAsync(string clientId, MessageBase message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Join a group (session)
    /// </summary>
    Task JoinGroupAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leave a group (session)
    /// </summary>
    Task LeaveGroupAsync(string groupName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a message to a group
    /// </summary>
    Task SendMessageToGroupAsync(string groupName, MessageBase message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a message is received
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Event raised when connection state changes
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Event raised when an error occurs
    /// </summary>
    event EventHandler<TransportErrorEventArgs>? ErrorOccurred;
}

/// <summary>
/// Event args for message received
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public MessageBase Message { get; set; } = null!;
    public string? SenderId { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event args for connection state changes
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    public string OldState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
    public string? Message { get; set; }
    public Exception? Exception { get; set; }
}

/// <summary>
/// Event args for transport errors
/// </summary>
public class TransportErrorEventArgs : EventArgs
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public bool IsRecoverable { get; set; } = true;
} 