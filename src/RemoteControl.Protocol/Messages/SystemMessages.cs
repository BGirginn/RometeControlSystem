using System.Text.Json.Serialization;

namespace RemoteControl.Protocol.Messages;

/// <summary>
/// Keep-alive message to maintain connection
/// </summary>
public class KeepAliveMessage : MessageBase
{
    public override string Type => "KeepAlive";

    [JsonPropertyName("payload")]
    public KeepAlivePayload Payload { get; set; } = new();
}

public class KeepAlivePayload
{
    [JsonPropertyName("senderId")]
    public string SenderId { get; set; } = string.Empty;

    [JsonPropertyName("senderType")]
    public string SenderType { get; set; } = string.Empty; // "Agent" or "Viewer"

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("metrics")]
    public ConnectionMetrics? Metrics { get; set; }
}

public class ConnectionMetrics
{
    [JsonPropertyName("latencyMs")]
    public double LatencyMs { get; set; }

    [JsonPropertyName("bandwidthMbps")]
    public double BandwidthMbps { get; set; }

    [JsonPropertyName("packetLossPercent")]
    public double PacketLossPercent { get; set; }

    [JsonPropertyName("cpuUsagePercent")]
    public double CpuUsagePercent { get; set; }

    [JsonPropertyName("memoryUsageMB")]
    public double MemoryUsageMB { get; set; }

    [JsonPropertyName("framesSent")]
    public long FramesSent { get; set; }

    [JsonPropertyName("framesDropped")]
    public long FramesDropped { get; set; }

    [JsonPropertyName("averageFps")]
    public double AverageFps { get; set; }

    [JsonPropertyName("encodingTimeMs")]
    public double EncodingTimeMs { get; set; }

    [JsonPropertyName("networkJitterMs")]
    public double NetworkJitterMs { get; set; }

    [JsonPropertyName("bufferHealth")]
    public double BufferHealth { get; set; }
}

/// <summary>
/// Error message for reporting issues
/// </summary>
public class ErrorMessage : MessageBase
{
    public override string Type => "Error";

    [JsonPropertyName("payload")]
    public ErrorPayload Payload { get; set; } = new();
}

public class ErrorPayload
{
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Error";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "General";

    [JsonPropertyName("isRecoverable")]
    public bool IsRecoverable { get; set; } = true;
}

/// <summary>
/// Error severity levels
/// </summary>
public static class ErrorSeverity
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string Critical = "Critical";
}

/// <summary>
/// Error categories
/// </summary>
public static class ErrorCategory
{
    public const string General = "General";
    public const string Network = "Network";
    public const string Authentication = "Authentication";
    public const string Encoding = "Encoding";
    public const string Input = "Input";
    public const string Session = "Session";
    public const string Configuration = "Configuration";
}

/// <summary>
/// Common error codes
/// </summary>
public static class ErrorCode
{
    public const string UnknownError = "UNKNOWN_ERROR";
    public const string NetworkConnectionLost = "NETWORK_CONNECTION_LOST";
    public const string AuthenticationFailed = "AUTHENTICATION_FAILED";
    public const string SessionNotFound = "SESSION_NOT_FOUND";
    public const string EncodingFailed = "ENCODING_FAILED";
    public const string DecodingFailed = "DECODING_FAILED";
    public const string InputProcessingFailed = "INPUT_PROCESSING_FAILED";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string ResourceNotAvailable = "RESOURCE_NOT_AVAILABLE";
    public const string ConfigurationError = "CONFIGURATION_ERROR";
    public const string ServerOverloaded = "SERVER_OVERLOADED";
} 