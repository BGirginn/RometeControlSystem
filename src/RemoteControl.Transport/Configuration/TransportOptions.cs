namespace RemoteControl.Transport.Configuration;

/// <summary>
/// Configuration options for transport layer
/// </summary>
public class TransportOptions
{
    public const string SectionName = "Transport";

    /// <summary>
    /// WebSocket connection timeout in milliseconds
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Keep-alive interval in milliseconds
    /// </summary>
    public int KeepAliveIntervalMs { get; set; } = 30000;

    /// <summary>
    /// Maximum message size in bytes
    /// </summary>
    public int MaxMessageSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Reconnection settings
    /// </summary>
    public ReconnectionOptions Reconnection { get; set; } = new();

    /// <summary>
    /// WebSocket buffer sizes
    /// </summary>
    public BufferOptions Buffers { get; set; } = new();

    /// <summary>
    /// JWT authentication settings
    /// </summary>
    public JwtOptions Jwt { get; set; } = new();
}

public class ReconnectionOptions
{
    /// <summary>
    /// Enable automatic reconnection
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of reconnection attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Initial reconnection delay in milliseconds
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum reconnection delay in milliseconds
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Reconnection delay multiplier (exponential backoff)
    /// </summary>
    public double DelayMultiplier { get; set; } = 2.0;
}

public class BufferOptions
{
    /// <summary>
    /// WebSocket send buffer size in bytes
    /// </summary>
    public int SendBufferSize { get; set; } = 4096;

    /// <summary>
    /// WebSocket receive buffer size in bytes
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    /// Frame queue capacity
    /// </summary>
    public int FrameQueueCapacity { get; set; } = 10;
}

public class JwtOptions
{
    /// <summary>
    /// JWT signing key
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT issuer
    /// </summary>
    public string Issuer { get; set; } = "RemoteControlSystem";

    /// <summary>
    /// JWT audience
    /// </summary>
    public string Audience { get; set; } = "RemoteControlSystem";

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
} 