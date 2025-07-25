using System;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteControl.Services.Interfaces
{
    public interface ITcpServerService
    {
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        bool IsListening { get; }
        int Port { get; }
        
        event EventHandler<string>? ClientConnected;
        event EventHandler<string>? ClientDisconnected;
    }
}
