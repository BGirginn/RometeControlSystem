using System.Text.Json.Serialization;

namespace RemoteControl.Protocol.Messages;

/// <summary>
/// Message sent by viewer to request a session with an agent
/// </summary>
public class RequestSessionMessage : MessageBase
{
    public override string Type => "RequestSession";

    [JsonPropertyName("payload")]
    public RequestSessionPayload Payload { get; set; } = new();
}

public class RequestSessionPayload
{
    [JsonPropertyName("targetAgentId")]
    public string TargetAgentId { get; set; } = string.Empty;

    [JsonPropertyName("viewerUsername")]
    public string ViewerUsername { get; set; } = string.Empty;

    [JsonPropertyName("viewerMachineName")]
    public string ViewerMachineName { get; set; } = string.Empty;

    [JsonPropertyName("requestReason")]
    public string? RequestReason { get; set; }

    [JsonPropertyName("viewerCapabilities")]
    public ViewerCapabilities ViewerCapabilities { get; set; } = new();
}

public class ViewerCapabilities
{
    [JsonPropertyName("supportedEncodings")]
    public string[] SupportedEncodings { get; set; } = { "h264", "jpeg" };

    [JsonPropertyName("preferredEncoding")]
    public string PreferredEncoding { get; set; } = "h264";

    [JsonPropertyName("maxResolution")]
    public ResolutionInfo MaxResolution { get; set; } = new() { Width = 1920, Height = 1080 };

    [JsonPropertyName("preferredFps")]
    public int PreferredFps { get; set; } = 30;
}

public class ResolutionInfo
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

/// <summary>
/// Response from agent indicating whether session request is accepted
/// </summary>
public class SessionDecisionMessage : MessageBase
{
    public override string Type => "SessionDecision";

    [JsonPropertyName("payload")]
    public SessionDecisionPayload Payload { get; set; } = new();
}

public class SessionDecisionPayload
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("sessionSettings")]
    public SessionSettings? SessionSettings { get; set; }
}

public class SessionSettings
{
    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "h264";

    [JsonPropertyName("quality")]
    public int Quality { get; set; } = 80;

    [JsonPropertyName("fps")]
    public int Fps { get; set; } = 30;

    [JsonPropertyName("resolution")]
    public ResolutionInfo Resolution { get; set; } = new();

    [JsonPropertyName("enableInput")]
    public bool EnableInput { get; set; } = true;
}

/// <summary>
/// Notification that session has started
/// </summary>
public class SessionStartedMessage : MessageBase
{
    public override string Type => "SessionStarted";

    [JsonPropertyName("payload")]
    public SessionStartedPayload Payload { get; set; } = new();
}

public class SessionStartedPayload
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("viewerId")]
    public string ViewerId { get; set; } = string.Empty;

    [JsonPropertyName("sessionSettings")]
    public SessionSettings SessionSettings { get; set; } = new();
}

/// <summary>
/// Notification that session has ended
/// </summary>
public class SessionEndedMessage : MessageBase
{
    public override string Type => "SessionEnded";

    [JsonPropertyName("payload")]
    public SessionEndedPayload Payload { get; set; } = new();
}

public class SessionEndedPayload
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("statistics")]
    public SessionStatistics? Statistics { get; set; }
}

public class SessionStatistics
{
    [JsonPropertyName("duration")]
    public TimeSpan Duration { get; set; }

    [JsonPropertyName("framesSent")]
    public long FramesSent { get; set; }

    [JsonPropertyName("bytesTransferred")]
    public long BytesTransferred { get; set; }

    [JsonPropertyName("averageFps")]
    public double AverageFps { get; set; }

    [JsonPropertyName("averageLatency")]
    public double AverageLatency { get; set; }
}

/// <summary>
/// Message forwarded to agent when a session request is received
/// </summary>
public class SessionRequestReceivedMessage : MessageBase
{
    public override string Type => "SessionRequestReceived";

    [JsonPropertyName("payload")]
    public SessionRequestReceivedPayload Payload { get; set; } = new();
}

public class SessionRequestReceivedPayload
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("viewerUsername")]
    public string ViewerUsername { get; set; } = string.Empty;

    [JsonPropertyName("viewerDisplayName")]
    public string ViewerDisplayName { get; set; } = string.Empty;

    [JsonPropertyName("viewerMachineName")]
    public string ViewerMachineName { get; set; } = string.Empty;

    [JsonPropertyName("requestTime")]
    public DateTime RequestTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("requestReason")]
    public string? RequestReason { get; set; }

    [JsonPropertyName("autoAcceptTimeout")]
    public int AutoAcceptTimeout { get; set; } = 30000;

    [JsonPropertyName("viewerCapabilities")]
    public ViewerCapabilities ViewerCapabilities { get; set; } = new();
}

/// <summary>
/// Message to select a specific monitor for screen capture
/// </summary>
public class SelectMonitorMessage : MessageBase
{
    public override string Type => "SelectMonitor";

    [JsonPropertyName("payload")]
    public SelectMonitorPayload Payload { get; set; } = new();
}

public class SelectMonitorPayload
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("monitorIndex")]
    public int MonitorIndex { get; set; } = 0;

    [JsonPropertyName("bounds")]
    public MonitorBounds Bounds { get; set; } = new();
}

public class MonitorBounds
{
    [JsonPropertyName("left")]
    public int Left { get; set; }

    [JsonPropertyName("top")]
    public int Top { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

/// <summary>
/// Message to update streaming quality settings during a session
/// </summary>
public class UpdateQualityMessage : MessageBase
{
    public override string Type => "UpdateQuality";

    [JsonPropertyName("payload")]
    public UpdateQualityPayload Payload { get; set; } = new();
}

public class UpdateQualityPayload
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("frameRate")]
    public int FrameRate { get; set; } = 30;

    [JsonPropertyName("quality")]
    public int Quality { get; set; } = 80;

    [JsonPropertyName("adaptive")]
    public bool Adaptive { get; set; } = true;

    [JsonPropertyName("encoding")]
    public string? Encoding { get; set; }
} 