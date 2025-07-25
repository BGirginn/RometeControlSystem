using System.Text.Json.Serialization;

namespace RemoteControl.Protocol.Messages;

/// <summary>
/// Message sent by agent to register with the control server
/// </summary>
public class RegisterAgentMessage : MessageBase
{
    public override string Type => "RegisterAgent";

    [JsonPropertyName("payload")]
    public RegisterAgentPayload Payload { get; set; } = new();
}

public class RegisterAgentPayload
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("machineName")]
    public string MachineName { get; set; } = string.Empty;

    [JsonPropertyName("operatingSystem")]
    public string OperatingSystem { get; set; } = string.Empty;

    [JsonPropertyName("screenWidth")]
    public int ScreenWidth { get; set; }

    [JsonPropertyName("screenHeight")]
    public int ScreenHeight { get; set; }

    [JsonPropertyName("capabilities")]
    public AgentCapabilities Capabilities { get; set; } = new();
}

public class AgentCapabilities
{
    [JsonPropertyName("h264")]
    public bool H264 { get; set; } = true;

    [JsonPropertyName("maxFps")]
    public int MaxFps { get; set; } = 60;

    [JsonPropertyName("supportedEncodings")]
    public string[] SupportedEncodings { get; set; } = { "h264", "jpeg" };

    [JsonPropertyName("hasAudio")]
    public bool HasAudio { get; set; } = false;
}

/// <summary>
/// Response sent by server after agent registration
/// </summary>
public class AgentRegisteredMessage : MessageBase
{
    public override string Type => "AgentRegistered";

    [JsonPropertyName("payload")]
    public AgentRegisteredPayload Payload { get; set; } = new();
}

public class AgentRegisteredPayload
{
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("serverCapabilities")]
    public ServerCapabilities ServerCapabilities { get; set; } = new();
}

public class ServerCapabilities
{
    [JsonPropertyName("maxConcurrentSessions")]
    public int MaxConcurrentSessions { get; set; } = 10;

    [JsonPropertyName("compressionSupported")]
    public bool CompressionSupported { get; set; } = true;

    [JsonPropertyName("encryptionRequired")]
    public bool EncryptionRequired { get; set; } = true;
} 