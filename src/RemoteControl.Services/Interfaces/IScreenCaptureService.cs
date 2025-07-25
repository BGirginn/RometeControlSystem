using System;
using System.Threading;
using System.Threading.Tasks;
using RemoteControl.Core.Models;

namespace RemoteControl.Services.Interfaces
{
    public interface IScreenCaptureService
    {
        Task<ScreenFrame> CaptureScreenAsync(int quality = 75, CancellationToken cancellationToken = default);
        Task<ScreenFrame> CaptureRegionAsync(int x, int y, int width, int height, int quality = 75, CancellationToken cancellationToken = default);
        (int Width, int Height) GetScreenDimensions();
        Task StartContinuousCaptureAsync(Action<ScreenFrame> onFrameCaptured, int frameRate = 30, CancellationToken cancellationToken = default);
        Task StopContinuousCaptureAsync();
        bool IsCapturing { get; }
    }
}
