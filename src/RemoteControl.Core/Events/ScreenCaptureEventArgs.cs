namespace RemoteControl.Core.Events;

/// <summary>
/// Event args for screen capture events
/// </summary>
public class ScreenCaptureEventArgs : EventArgs
{
    public byte[] FrameData { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public double CaptureTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
}
