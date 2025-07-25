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
    public int Quality { get; set; }

    [JsonPropertyName("isKeyFrame")]
    public bool IsKeyFrame { get; set; }

    [JsonPropertyName("data")]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("dataSize")]
    public int DataSize { get; set; }

    [JsonPropertyName("captureTime")]
    public DateTime CaptureTime { get; set; }

    [JsonPropertyName("encodeTime")]
    public double EncodeTimeMs { get; set; }
} 