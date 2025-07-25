using RemoteControl.Protocol.Messages;

namespace RemoteControl.ControlServer.Services;

/// <summary>
/// Service for managing remote control sessions
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Create a new session request
    /// </summary>
    Task<SessionRequestInfo> CreateSessionRequestAsync(SessionRequestInfo requestInfo);

    /// <summary>
    /// Get a session request by ID
    /// </summary>
    Task<SessionRequestInfo?> GetSessionRequestAsync(string sessionId);

    /// <summary>
    /// Start a session
    /// </summary>
    Task StartSessionAsync(string sessionId, SessionSettings? settings = null);

    /// <summary>
    /// Reject a session request
    /// </summary>
    Task RejectSessionAsync(string sessionId, string? reason = null);

    /// <summary>
    /// End a session
    /// </summary>
    Task EndSessionAsync(string sessionId, string reason);

    /// <summary>
    /// Get active sessions for a connection
    /// </summary>
    Task<IEnumerable<SessionInfo>> GetActiveSessionsForConnectionAsync(string connectionId);

    /// <summary>
    /// Get active sessions for a user
    /// </summary>
    Task<IEnumerable<SessionInfo>> GetActiveSessionsForUserAsync(string userId);

    /// <summary>
    /// Get all active sessions
    /// </summary>
    Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync();

    /// <summary>
    /// Update session activity
    /// </summary>
    Task UpdateSessionActivityAsync(string sessionId);
}

/// <summary>
/// Session request information
/// </summary>
public class SessionRequestInfo
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string ViewerId { get; set; } = string.Empty;
    public string ViewerConnectionId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string AgentConnectionId { get; set; } = string.Empty;
    public string ViewerUsername { get; set; } = string.Empty;
    public string ViewerMachineName { get; set; } = string.Empty;
    public string? RequestReason { get; set; }
    public ViewerCapabilities ViewerCapabilities { get; set; } = new();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public SessionRequestStatus Status { get; set; } = SessionRequestStatus.Pending;
}

/// <summary>
/// Session information
/// </summary>
public class SessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string ViewerId { get; set; } = string.Empty;
    public string ViewerConnectionId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string AgentConnectionId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public SessionSettings Settings { get; set; } = new();
    public string? EndReason { get; set; }
}

/// <summary>
/// Session request status
/// </summary>
public enum SessionRequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired
}

/// <summary>
/// Session status
/// </summary>
public enum SessionStatus
{
    Active,
    Ended,
    Error
} 