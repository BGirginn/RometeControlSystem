using System;
using System.Threading.Tasks;

namespace RemoteControl.Services.Interfaces;

/// <summary>
/// Interface for video encoding services
/// </summary>
public interface IVideoEncoderService
{
    /// <summary>
    /// Initialize the encoder with specified settings
    /// </summary>
    Task<bool> InitializeAsync(VideoEncoderSettings settings);

    /// <summary>
    /// Encode a raw frame to compressed format
    /// </summary>
    Task<EncodedFrame?> EncodeFrameAsync(byte[] rawFrameData, int width, int height);

    /// <summary>
    /// Update encoder settings during operation
    /// </summary>
    Task UpdateSettingsAsync(VideoEncoderSettings settings);

    /// <summary>
    /// Get list of supported encoding formats
    /// </summary>
    Task<string[]> GetSupportedEncodingsAsync();

    /// <summary>
    /// Check if H.264 hardware encoding is available
    /// </summary>
    Task<bool> IsH264SupportedAsync();
}

/// <summary>
/// Video encoder configuration settings
/// </summary>
public class VideoEncoderSettings
{
    public string Codec { get; set; } = "h264";
    public int Bitrate { get; set; } = 2000000; // 2 Mbps
    public int Quality { get; set; } = 80;
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int FrameRate { get; set; } = 30;
    public bool HardwareAcceleration { get; set; } = true;
}

/// <summary>
/// Encoded video frame
/// </summary>
public class EncodedFrame
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public string Encoding { get; set; } = string.Empty;
    public bool IsKeyFrame { get; set; }
    public DateTime CaptureTime { get; set; }
    public double EncodeTimeMs { get; set; }
}
