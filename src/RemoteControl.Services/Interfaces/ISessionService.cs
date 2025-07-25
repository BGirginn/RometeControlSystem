using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RemoteControl.Core.Models;

namespace RemoteControl.Services.Interfaces
{
    public interface ISessionService
    {
        Task<SessionInfo> CreateSessionAsync(string viewerId, string agentId, CancellationToken cancellationToken = default);
        Task<SessionInfo?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
        Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
        Task UpdateSessionStatusAsync(string sessionId, SessionStatus status, CancellationToken cancellationToken = default);
        Task UpdateSessionMetricsAsync(string sessionId, long bytesTransferred, long framesTransferred, CancellationToken cancellationToken = default);
        Task EndSessionAsync(string sessionId, CancellationToken cancellationToken = default);
        
        event EventHandler<SessionInfo>? SessionCreated;
        event EventHandler<SessionInfo>? SessionEnded;
        event EventHandler<SessionInfo>? SessionStatusChanged;
    }
}
