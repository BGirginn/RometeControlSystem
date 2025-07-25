using System;
using System.Threading;
using System.Threading.Tasks;
using RemoteControl.Core.Models;
using RemoteControl.Core.Events;

namespace RemoteControl.Services.Interfaces
{
    public interface ITransportService
    {
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        Task ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        Task<bool> IsConnectedAsync();

        event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        event EventHandler<byte[]>? FrameDataReceived;
    }
}