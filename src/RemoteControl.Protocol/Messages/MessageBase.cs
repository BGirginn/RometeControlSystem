using System.Text.Json.Serialization;

namespace RemoteControl.Protocol.Messages;

/// <summary>
/// Base class for all protocol messages
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RegisterAgentMessage), "RegisterAgent")]
[JsonDerivedType(typeof(AgentRegisteredMessage), "AgentRegistered")]
[JsonDerivedType(typeof(RequestSessionMessage), "RequestSession")]
[JsonDerivedType(typeof(SessionRequestReceivedMessage), "SessionRequestReceived")]
[JsonDerivedType(typeof(SessionDecisionMessage), "SessionDecision")]
[JsonDerivedType(typeof(SessionStartedMessage), "SessionStarted")]
[JsonDerivedType(typeof(SessionEndedMessage), "SessionEnded")]
[JsonDerivedType(typeof(FrameMessage), "Frame")]
[JsonDerivedType(typeof(InputEventMessage), "InputEvent")]
[JsonDerivedType(typeof(KeepAliveMessage), "KeepAlive")]
[JsonDerivedType(typeof(ErrorMessage), "Error")]
[JsonDerivedType(typeof(SelectMonitorMessage), "SelectMonitor")]
[JsonDerivedType(typeof(UpdateQualityMessage), "UpdateQuality")]
public abstract class MessageBase
{
    /// <summary>
    /// Message type identifier
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    /// <summary>
    /// Message timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional message ID for tracking
    /// </summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; } = Guid.NewGuid().ToString();
} 