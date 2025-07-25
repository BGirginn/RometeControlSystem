using System.Text.Json.Serialization;

namespace RemoteControl.Protocol.Messages;

/// <summary>
/// Message containing video frame data
/// </summary>
public class FrameMessage : MessageBase
{
    public override string Type => "Frame";

    [JsonPropertyName("payload")]
    public FramePayload Payload { get; set; } = new();
}

public class FramePayload
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("frameNumber")]
    public long FrameNumber { get; set; }

    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "h264";

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("quality")]
    public int Quality { get; set; } = 80;

    [JsonPropertyName("isKeyFrame")]
    public bool IsKeyFrame { get; set; }

    [JsonPropertyName("dataSize")]
    public int DataSize { get; set; }

    [JsonPropertyName("captureTime")]
    public DateTime CaptureTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("encodeTimeMs")]
    public double EncodeTimeMs { get; set; }

    [JsonPropertyName("compressionRatio")]
    public double CompressionRatio { get; set; }

    [JsonPropertyName("monitorIndex")]
    public int MonitorIndex { get; set; } = 0;

    /// <summary>
    /// Frame data is sent separately as binary data through SignalR
    /// This property is used for smaller frames or JPEG fallback
    /// </summary>
    [JsonPropertyName("data")]
    public byte[]? Data { get; set; }

    /// <summary>
    /// For H.264 streams, indicates if this frame contains codec configuration
    /// </summary>
    [JsonPropertyName("hasCodecConfig")]
    public bool HasCodecConfig { get; set; }
} 