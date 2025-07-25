using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using RemoteControl.Core.Events;

namespace RemoteControl.Services.Interfaces
{
    public interface IVideoRenderer
    {
        Task<WriteableBitmap> RenderFrameAsync(byte[] frameData, CancellationToken cancellationToken = default);
        Task SetQualityAsync(int quality);
        Task SetFrameRateAsync(int fps);

        event EventHandler<MetricsEventArgs>? MetricsUpdated;
    }
}