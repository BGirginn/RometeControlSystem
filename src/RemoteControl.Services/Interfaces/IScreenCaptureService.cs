using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using RemoteControl.Core.Events;

namespace RemoteControl.Services.Interfaces;

/// <summary>
/// Interface for screen capture services
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// Initialize the screen capture service
    /// </summary>
    Task<bool> InitializeAsync(int monitorIndex = 0);

    /// <summary>
    /// Capture a single frame
    /// </summary>
    Task<byte[]?> CaptureFrameAsync();

    /// <summary>
    /// Get bounds of all available monitors
    /// </summary>
    Task<Rectangle[]> GetMonitorBoundsAsync();

    /// <summary>
    /// Set specific capture area
    /// </summary>
    Task SetCaptureAreaAsync(Rectangle area);

    /// <summary>
    /// Check if screen capture is available on this system
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Start continuous frame capture for streaming
    /// </summary>
    Task StartContinuousCaptureAsync(Action<byte[]> onFrameCaptured, int fps = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop continuous frame capture
    /// </summary>
    Task StopContinuousCaptureAsync();

    /// <summary>
    /// Event fired when a frame is captured
    /// </summary>
    event EventHandler<ScreenCaptureEventArgs>? FrameCaptured;
}
