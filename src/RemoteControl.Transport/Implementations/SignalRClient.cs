using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteControl.Protocol.Messages;
using RemoteControl.Protocol.Serialization;
using RemoteControl.Transport.Configuration;
using RemoteControl.Transport.Interfaces;

namespace RemoteControl.Transport.Implementations;

/// <summary>
/// SignalR client implementation with automatic reconnection
/// </summary>
public class SignalRClient : ISignalRClient, IDisposable
{
    private readonly ILogger<SignalRClient> _logger;
    private readonly TransportOptions _options;
    private HubConnection? _connection;
    private CancellationTokenSource? _reconnectionCts;
    private bool _disposed;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;
    public string? ClientId { get; private set; }

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<TransportErrorEventArgs>? ErrorOccurred;

    public SignalRClient(ILogger<SignalRClient> logger, IOptions<TransportOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task ConnectAsync(string hubUrl, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SignalRClient));

        try
        {
            _logger.LogInformation("Connecting to SignalR hub: {HubUrl}", hubUrl);

            var builder = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(accessToken);
                    }
                })
                .WithAutomaticReconnect(new CustomRetryPolicy(_options.Reconnection))
                .ConfigureLogging(logging => logging.AddProvider(new LoggerProvider(_logger)));

            _connection = builder.Build();

            // Setup event handlers
            SetupEventHandlers();

            // Connect with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.ConnectionTimeoutMs);

            await _connection.StartAsync(timeoutCts.Token);

            ClientId = _connection.ConnectionId;
            _logger.LogInformation("Connected to SignalR hub with ID: {ConnectionId}", ClientId);

            OnConnectionStateChanged("Disconnected", "Connected", "Successfully connected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub: {HubUrl}", hubUrl);
            OnErrorOccurred("CONNECTION_FAILED", $"Failed to connect: {ex.Message}", ex);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_connection != null)
        {
            try
            {
                _logger.LogInformation("Disconnecting from SignalR hub");
                
                _reconnectionCts?.Cancel();
                await _connection.StopAsync(cancellationToken);
                
                OnConnectionStateChanged("Connected", "Disconnected", "Manually disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnect");
                OnErrorOccurred("DISCONNECT_ERROR", $"Error during disconnect: {ex.Message}", ex);
            }
        }
    }

    public async Task SendMessageAsync(MessageBase message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected to SignalR hub");

        try
        {
            var json = MessageSerializer.Serialize(message);
            await _connection!.InvokeAsync("SendMessage", json, cancellationToken);
            
            _logger.LogTrace("Sent message: {MessageType}", message.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message: {MessageType}", message.Type);
            OnErrorOccurred("SEND_MESSAGE_FAILED", $"Failed to send message: {ex.Message}", ex);
            throw;
        }
    }

    public async Task SendMessageToClientAsync(string clientId, MessageBase message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected to SignalR hub");

        try
        {
            var json = MessageSerializer.Serialize(message);
            await _connection!.InvokeAsync("SendMessageToClient", clientId, json, cancellationToken);
            
            _logger.LogTrace("Sent message to client {ClientId}: {MessageType}", clientId, message.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to client {ClientId}: {MessageType}", clientId, message.Type);
            OnErrorOccurred("SEND_MESSAGE_TO_CLIENT_FAILED", $"Failed to send message to client: {ex.Message}", ex);
            throw;
        }
    }

    public async Task JoinGroupAsync(string groupName, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected to SignalR hub");

        try
        {
            await _connection!.InvokeAsync("JoinGroup", groupName, cancellationToken);
            _logger.LogDebug("Joined group: {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join group: {GroupName}", groupName);
            OnErrorOccurred("JOIN_GROUP_FAILED", $"Failed to join group: {ex.Message}", ex);
            throw;
        }
    }

    public async Task LeaveGroupAsync(string groupName, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected to SignalR hub");

        try
        {
            await _connection!.InvokeAsync("LeaveGroup", groupName, cancellationToken);
            _logger.LogDebug("Left group: {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave group: {GroupName}", groupName);
            OnErrorOccurred("LEAVE_GROUP_FAILED", $"Failed to leave group: {ex.Message}", ex);
            throw;
        }
    }

    public async Task SendMessageToGroupAsync(string groupName, MessageBase message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected to SignalR hub");

        try
        {
            var json = MessageSerializer.Serialize(message);
            await _connection!.InvokeAsync("SendMessageToGroup", groupName, json, cancellationToken);
            
            _logger.LogTrace("Sent message to group {GroupName}: {MessageType}", groupName, message.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to group {GroupName}: {MessageType}", groupName, message.Type);
            OnErrorOccurred("SEND_MESSAGE_TO_GROUP_FAILED", $"Failed to send message to group: {ex.Message}", ex);
            throw;
        }
    }

    private void SetupEventHandlers()
    {
        if (_connection == null) return;

        // Handle incoming messages
        _connection.On<string>("ReceiveMessage", (json) =>
        {
            try
            {
                if (MessageSerializer.TryDeserialize(json, out var message) && message != null)
                {
                    OnMessageReceived(message);
                }
                else
                {
                    _logger.LogWarning("Received invalid message: {Json}", json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing received message");
                OnErrorOccurred("MESSAGE_PROCESSING_ERROR", $"Error processing message: {ex.Message}", ex);
            }
        });

        // Handle connection state changes
        _connection.Closed += async (error) =>
        {
            OnConnectionStateChanged("Connected", "Disconnected", error?.Message);
            
            if (error != null)
            {
                _logger.LogError(error, "Connection closed with error");
                OnErrorOccurred("CONNECTION_LOST", $"Connection lost: {error.Message}", error);
            }
            else
            {
                _logger.LogInformation("Connection closed");
            }

            // Start reconnection if enabled and not manually disconnected
            if (_options.Reconnection.Enabled && error != null && !_disposed)
            {
                await StartReconnectionAsync();
            }
        };

        _connection.Reconnecting += (error) =>
        {
            OnConnectionStateChanged("Disconnected", "Reconnecting", error?.Message);
            _logger.LogInformation("Attempting to reconnect...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += (connectionId) =>
        {
            ClientId = connectionId;
            OnConnectionStateChanged("Reconnecting", "Connected", "Reconnected successfully");
            _logger.LogInformation("Reconnected with ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };
    }

    private async Task StartReconnectionAsync()
    {
        if (_reconnectionCts != null && !_reconnectionCts.IsCancellationRequested)
            return;

        _reconnectionCts?.Cancel();
        _reconnectionCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(_options.Reconnection.InitialDelayMs), _reconnectionCts.Token);
            // SignalR handles automatic reconnection, we just need to wait
        }
        catch (OperationCanceledException)
        {
            // Reconnection was cancelled
        }
    }

    private void OnMessageReceived(MessageBase message)
    {
        MessageReceived?.Invoke(this, new MessageReceivedEventArgs
        {
            Message = message,
            SenderId = null, // SignalR doesn't provide sender ID in this context
            ReceivedAt = DateTime.UtcNow
        });
    }

    private void OnConnectionStateChanged(string oldState, string newState, string? message)
    {
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            OldState = oldState,
            NewState = newState,
            Message = message
        });
    }

    private void OnErrorOccurred(string errorCode, string message, Exception? exception)
    {
        ErrorOccurred?.Invoke(this, new TransportErrorEventArgs
        {
            ErrorCode = errorCode,
            Message = message,
            Exception = exception,
            IsRecoverable = true // Most SignalR errors are recoverable
        });
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _reconnectionCts?.Cancel();
        _reconnectionCts?.Dispose();
        _connection?.DisposeAsync().AsTask().Wait();
    }
}

/// <summary>
/// Custom retry policy for SignalR reconnection
/// </summary>
public class CustomRetryPolicy : IRetryPolicy
{
    private readonly ReconnectionOptions _options;

    public CustomRetryPolicy(ReconnectionOptions options)
    {
        _options = options;
    }

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (retryContext.PreviousRetryCount >= _options.MaxAttempts)
            return null;

        var delay = Math.Min(
            _options.InitialDelayMs * Math.Pow(_options.DelayMultiplier, retryContext.PreviousRetryCount),
            _options.MaxDelayMs);

        return TimeSpan.FromMilliseconds(delay);
    }
}

/// <summary>
/// Logger provider wrapper for SignalR
/// </summary>
public class LoggerProvider : ILoggerProvider
{
    private readonly ILogger _logger;

    public LoggerProvider(ILogger logger)
    {
        _logger = logger;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
} 