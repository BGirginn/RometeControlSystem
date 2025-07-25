using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RemoteControl.Core.Models;

namespace RemoteControl.Services.Interfaces
{
    public interface IAgentService
    {
        Task<AgentInfo> GetLocalAgentInfoAsync(CancellationToken cancellationToken = default);
        Task<AgentInfo?> GetAgentInfoAsync(string agentId, CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentInfo>> GetOnlineAgentsAsync(CancellationToken cancellationToken = default);
        Task RegisterAgentAsync(AgentInfo agentInfo, CancellationToken cancellationToken = default);
        Task UpdateAgentStatusAsync(string agentId, bool isOnline, CancellationToken cancellationToken = default);
        Task<bool> ValidateAgentConnectionAsync(string agentId, string token, CancellationToken cancellationToken = default);
        
        event EventHandler<AgentInfo>? AgentRegistered;
        event EventHandler<AgentInfo>? AgentStatusChanged;
    }
}
