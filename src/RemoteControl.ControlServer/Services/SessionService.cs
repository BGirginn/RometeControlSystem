using System.Collections.Concurrent;
using RemoteControl.Protocol.Messages;

namespace RemoteControl.ControlServer.Services;

/// <summary>
/// In-memory session service implementation
/// </summary>
public class SessionService : ISessionService
{
    private readonly ILogger<SessionService> _logger;
    private readonly ConcurrentDictionary<string, SessionRequestInfo> _sessionRequests = new();
    private readonly ConcurrentDictionary<string, SessionInfo> _activeSessions = new();

    public SessionService(ILogger<SessionService> logger)
    {
        _logger = logger;
    }

    public async Task<SessionRequestInfo> CreateSessionRequestAsync(SessionRequestInfo requestInfo)
    {
        _sessionRequests[requestInfo.SessionId] = requestInfo;
        
        _logger.LogInformation("Session request created: {SessionId} from viewer {ViewerId} to agent {AgentId}",
            requestInfo.SessionId, requestInfo.ViewerId, requestInfo.AgentId);

        return await Task.FromResult(requestInfo);
    }

    public async Task<SessionRequestInfo?> GetSessionRequestAsync(string sessionId)
    {
        _sessionRequests.TryGetValue(sessionId, out var request);
        return await Task.FromResult(request);
    }

    public async Task StartSessionAsync(string sessionId, SessionSettings? settings = null)
    {
        if (!_sessionRequests.TryGetValue(sessionId, out var request))
        {
            throw new InvalidOperationException($"Session request {sessionId} not found");
        }

        var session = new SessionInfo
        {
            SessionId = sessionId,
            ViewerId = request.ViewerId,
            ViewerConnectionId = request.ViewerConnectionId,
            AgentId = request.AgentId,
            AgentConnectionId = request.AgentConnectionId,
            StartedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            Status = SessionStatus.Active,
            Settings = settings ?? new SessionSettings()
        };

        _activeSessions[sessionId] = session;
        request.Status = SessionRequestStatus.Accepted;

        _logger.LogInformation("Session started: {SessionId} between viewer {ViewerId} and agent {AgentId}",
            sessionId, request.ViewerId, request.AgentId);

        await Task.CompletedTask;
    }

    public async Task RejectSessionAsync(string sessionId, string? reason = null)
    {
        if (_sessionRequests.TryGetValue(sessionId, out var request))
        {
            request.Status = SessionRequestStatus.Rejected;
            
            _logger.LogInformation("Session rejected: {SessionId}, Reason: {Reason}",
                sessionId, reason ?? "No reason provided");
        }

        await Task.CompletedTask;
    }

    public async Task EndSessionAsync(string sessionId, string reason)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            session.Status = SessionStatus.Ended;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = reason;

            _logger.LogInformation("Session ended: {SessionId}, Duration: {Duration}, Reason: {Reason}",
                sessionId, session.EndedAt - session.StartedAt, reason);
        }

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<SessionInfo>> GetActiveSessionsForConnectionAsync(string connectionId)
    {
        var sessions = _activeSessions.Values
            .Where(s => s.Status == SessionStatus.Active && 
                       (s.ViewerConnectionId == connectionId || s.AgentConnectionId == connectionId))
            .ToList();
        
        return await Task.FromResult(sessions);
    }

    public async Task<IEnumerable<SessionInfo>> GetActiveSessionsForUserAsync(string userId)
    {
        var sessions = _activeSessions.Values
            .Where(s => s.Status == SessionStatus.Active && 
                       (s.ViewerId == userId || s.AgentId.StartsWith($"{userId}#")))
            .ToList();

        return await Task.FromResult(sessions);
    }

    public async Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync()
    {
        var sessions = _activeSessions.Values
            .Where(s => s.Status == SessionStatus.Active)
            .ToList();

        return await Task.FromResult(sessions);
    }

    public async Task UpdateSessionActivityAsync(string sessionId)
    {
        if (_activeSessions.TryGetValue(sessionId, out var session))
        {
            session.LastActivity = DateTime.UtcNow;
        }

        await Task.CompletedTask;
    }
} 