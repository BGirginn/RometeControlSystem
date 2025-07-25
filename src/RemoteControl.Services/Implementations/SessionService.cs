using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    public class SessionService : ISessionService
    {
        private readonly ILogger<SessionService> _logger;
        private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();

        public event EventHandler<SessionInfo>? SessionCreated;
        public event EventHandler<SessionInfo>? SessionEnded;
        public event EventHandler<SessionInfo>? SessionStatusChanged;

        public SessionService(ILogger<SessionService> logger)
        {
            _logger = logger;
        }

        public async Task<SessionInfo> CreateSessionAsync(string viewerId, string agentId, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            var sessionId = Guid.NewGuid().ToString();
            var session = new SessionInfo
            {
                SessionId = sessionId,
                ViewerId = viewerId,
                AgentId = agentId,
                StartTime = DateTime.UtcNow,
                Status = SessionStatus.Connecting,
                IsActive = true
            };

            _sessions[sessionId] = session;
            _logger.LogInformation("Session created: {SessionId} between {ViewerId} and {AgentId}", 
                sessionId, viewerId, agentId);

            SessionCreated?.Invoke(this, session);
            return session;
        }

        public async Task<SessionInfo?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }

        public async Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return _sessions.Values.Where(s => s.IsActive);
        }

        public async Task<IEnumerable<SessionInfo>> GetSessionsForAgentAsync(string agentId, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return _sessions.Values.Where(s => s.ViewerId == agentId || s.AgentId == agentId);
        }

        public async Task UpdateSessionStatusAsync(string sessionId, SessionStatus status, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            if (_sessions.TryGetValue(sessionId, out var session))
            {
                var oldStatus = session.Status;
                session.Status = status;
                session.LastActivity = DateTime.UtcNow;

                _logger.LogDebug("Session status changed: {SessionId} from {OldStatus} to {NewStatus}", 
                    sessionId, oldStatus, status);

                SessionStatusChanged?.Invoke(this, session);
            }
        }

        public async Task UpdateSessionMetricsAsync(string sessionId, long bytesTransferred, long framesTransferred, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.LastActivity = DateTime.UtcNow;
                // Here you could update metrics like bytes transferred, frame count, etc.
                // For now, we'll just update the last activity time
                _logger.LogTrace("Session metrics updated: {SessionId}, Bytes: {Bytes}, Frames: {Frames}", 
                    sessionId, bytesTransferred, framesTransferred);
            }
        }

        public async Task EndSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.IsActive = false;
                session.EndTime = DateTime.UtcNow;
                session.Status = SessionStatus.Disconnected;

                _logger.LogInformation("Session ended: {SessionId}, Duration: {Duration}", 
                    sessionId, session.EndTime - session.StartTime);

                SessionEnded?.Invoke(this, session);
            }
        }

        public async Task UpdateSessionMetricsAsync(string sessionId, long bytesTransferred, int framesSent, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.BytesTransferred += bytesTransferred;
                session.FramesSent += framesSent;
                session.LastActivity = DateTime.UtcNow;

                _logger.LogTrace("Session metrics updated: {SessionId}, Bytes: {BytesTransferred}, Frames: {FramesSent}", 
                    sessionId, session.BytesTransferred, session.FramesSent);
            }
        }

        public async Task<bool> ValidateSessionAccessAsync(string sessionId, string agentId, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            if (_sessions.TryGetValue(sessionId, out var session))
            {
                var hasAccess = session.ViewerId == agentId || session.AgentId == agentId;
                
                _logger.LogDebug("Session access validation: {SessionId} for {AgentId} - Access: {HasAccess}", 
                    sessionId, agentId, hasAccess);
                
                return hasAccess && session.IsActive;
            }

            return false;
        }

        public async Task CleanupInactiveSessionsAsync(TimeSpan maxInactiveTime, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            var cutoffTime = DateTime.UtcNow - maxInactiveTime;
            var inactiveSessions = _sessions.Values
                .Where(s => s.IsActive && s.LastActivity < cutoffTime)
                .ToList();

            foreach (var session in inactiveSessions)
            {
                await EndSessionAsync(session.SessionId, cancellationToken);
                _logger.LogInformation("Inactive session cleaned up: {SessionId}", session.SessionId);
            }
        }
    }
}
