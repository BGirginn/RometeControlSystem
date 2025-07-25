using System.Collections.Concurrent;

namespace RemoteControl.ControlServer.Services;

/// <summary>
/// In-memory agent service implementation
/// </summary>
public class AgentService : IAgentService
{
    private readonly ILogger<AgentService> _logger;
    private readonly ConcurrentDictionary<string, AgentInfo> _agents = new();
    private readonly ConcurrentDictionary<string, string> _connectionToAgentMap = new();
    private int _agentCounter = 1000;

    public AgentService(ILogger<AgentService> logger)
    {
        _logger = logger;
    }

    public async Task<string> RegisterAgentAsync(AgentRegistrationInfo agentInfo)
    {
        var agentId = GenerateAgentId(agentInfo.Username);
        
        var agent = new AgentInfo
        {
            AgentId = agentId,
            ConnectionId = agentInfo.ConnectionId,
            UserId = agentInfo.UserId,
            Username = agentInfo.Username,
            MachineName = agentInfo.MachineName,
            OperatingSystem = agentInfo.OperatingSystem,
            ScreenWidth = agentInfo.ScreenWidth,
            ScreenHeight = agentInfo.ScreenHeight,
            Capabilities = agentInfo.Capabilities,
            IsOnline = true,
            RegisteredAt = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };

        _agents[agentId] = agent;
        _connectionToAgentMap[agentInfo.ConnectionId] = agentId;

        _logger.LogInformation("Agent registered: {AgentId} ({MachineName}) for user {Username}", 
            agentId, agentInfo.MachineName, agentInfo.Username);

        return await Task.FromResult(agentId);
    }

    public async Task<AgentInfo?> GetAgentAsync(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return await Task.FromResult(agent);
    }

    public async Task<AgentInfo?> GetAgentByConnectionIdAsync(string connectionId)
    {
        if (_connectionToAgentMap.TryGetValue(connectionId, out var agentId))
        {
            return await GetAgentAsync(agentId);
        }
        return null;
    }

    public async Task<IEnumerable<AgentInfo>> GetOnlineAgentsAsync()
    {
        var onlineAgents = _agents.Values.Where(a => a.IsOnline).ToList();
        return await Task.FromResult(onlineAgents);
    }

    public async Task<IEnumerable<AgentInfo>> GetUserAgentsAsync(string userId)
    {
        var userAgents = _agents.Values.Where(a => a.UserId == userId).ToList();
        return await Task.FromResult(userAgents);
    }

    public async Task SetAgentOfflineAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            agent.IsOnline = false;
            agent.LastSeen = DateTime.UtcNow;
            
            // Remove from connection mapping
            _connectionToAgentMap.TryRemove(agent.ConnectionId, out _);

            _logger.LogInformation("Agent {AgentId} set offline", agentId);
        }

        await Task.CompletedTask;
    }

    public async Task UpdateAgentHeartbeatAsync(string agentId)
    {
        if (_agents.TryGetValue(agentId, out var agent))
        {
            agent.LastSeen = DateTime.UtcNow;
        }

        await Task.CompletedTask;
    }

    private string GenerateAgentId(string username)
    {
        var counter = Interlocked.Increment(ref _agentCounter);
        return $"{username}#{counter:X4}";
    }
} 