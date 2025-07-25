using System;
using System.Threading;
using System.Threading.Tasks;
using RemoteControl.Core.Models;
using RemoteControl.Core.Events;
using RemoteControl.Protocol.Messages;

namespace RemoteControl.Services.Interfaces
{
    public interface ITransportService
    {
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        Task ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        Task<bool> IsConnectedAsync();

        // Input event methods
        Task SendInputEventAsync(InputEventMessage inputEvent, CancellationToken cancellationToken = default);
        Task SendMouseMoveAsync(string sessionId, double relativeX, double relativeY, CancellationToken cancellationToken = default);
        Task SendMouseDownAsync(string sessionId, string button, CancellationToken cancellationToken = default);
        Task SendMouseUpAsync(string sessionId, string button, CancellationToken cancellationToken = default);
        Task SendMouseWheelAsync(string sessionId, int delta, CancellationToken cancellationToken = default);
        Task SendKeyDownAsync(string sessionId, int virtualKey, CancellationToken cancellationToken = default);
        Task SendKeyUpAsync(string sessionId, int virtualKey, CancellationToken cancellationToken = default);

        event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        event EventHandler<byte[]>? FrameDataReceived;
    }
}