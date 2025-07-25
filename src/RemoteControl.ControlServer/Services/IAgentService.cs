using RemoteControl.Protocol.Messages;

namespace RemoteControl.ControlServer.Services;

/// <summary>
/// Service for managing agents
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Register a new agent
    /// </summary>
    Task<string> RegisterAgentAsync(AgentRegistrationInfo agentInfo);

    /// <summary>
    /// Get an agent by ID
    /// </summary>
    Task<AgentInfo?> GetAgentAsync(string agentId);

    /// <summary>
    /// Get an agent by connection ID
    /// </summary>
    Task<AgentInfo?> GetAgentByConnectionIdAsync(string connectionId);

    /// <summary>
    /// Get all online agents
    /// </summary>
    Task<IEnumerable<AgentInfo>> GetOnlineAgentsAsync();

    /// <summary>
    /// Get agents for a specific user
    /// </summary>
    Task<IEnumerable<AgentInfo>> GetUserAgentsAsync(string userId);

    /// <summary>
    /// Set agent offline
    /// </summary>
    Task SetAgentOfflineAsync(string agentId);

    /// <summary>
    /// Update agent heartbeat
    /// </summary>
    Task UpdateAgentHeartbeatAsync(string agentId);
}

/// <summary>
/// Agent registration information
/// </summary>
public class AgentRegistrationInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public AgentCapabilities Capabilities { get; set; } = new();
}

/// <summary>
/// Agent information
/// </summary>
public class AgentInfo
{
    public string AgentId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public AgentCapabilities Capabilities { get; set; } = new();
    public bool IsOnline { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
} 