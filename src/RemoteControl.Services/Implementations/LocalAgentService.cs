using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    public class LocalAgentService : IAgentService
    {
        private readonly ILogger<LocalAgentService> _logger;
        private readonly Dictionary<string, AgentInfo> _agents = new();
        private AgentInfo? _localAgent;

        public event EventHandler<AgentInfo>? AgentRegistered;
        public event EventHandler<AgentInfo>? AgentStatusChanged;

        public LocalAgentService(ILogger<LocalAgentService> logger)
        {
            _logger = logger;
        }

        public async Task<AgentInfo> GetLocalAgentInfoAsync(CancellationToken cancellationToken = default)
        {
            if (_localAgent != null)
                return _localAgent;

            await Task.Run(() =>
            {
                var agentId = GetOrCreateAgentId();
                var computerName = Environment.MachineName;
                var userName = Environment.UserName;
                var ipAddress = GetLocalIPAddress();
                var (width, height) = GetScreenDimensions();

                _localAgent = new AgentInfo
                {
                    AgentId = agentId,
                    ComputerName = computerName,
                    UserName = userName,
                    IPAddress = ipAddress,
                    OperatingSystem = GetOperatingSystemInfo(),
                    ScreenWidth = width,
                    ScreenHeight = height,
                    LastSeen = DateTime.UtcNow,
                    IsOnline = true,
                    Version = GetAssemblyVersion()
                };
            }, cancellationToken);

            _logger.LogInformation("Local agent info created: {AgentId} on {ComputerName}", _localAgent?.AgentId, _localAgent?.ComputerName);
            return _localAgent!; // _localAgent is never null here
        }

        public async Task<AgentInfo?> GetAgentInfoAsync(string agentId, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return _agents.TryGetValue(agentId, out var agent) ? agent : null;
        }

        public async Task<IEnumerable<AgentInfo>> GetOnlineAgentsAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return _agents.Values.Where(a => a.IsOnline);
        }

        public async Task RegisterAgentAsync(AgentInfo agentInfo, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            
            _agents[agentInfo.AgentId] = agentInfo;
            _logger.LogInformation("Agent registered: {AgentId} on {ComputerName}", agentInfo.AgentId, agentInfo.ComputerName);
            
            AgentRegistered?.Invoke(this, agentInfo);
        }

        public async Task UpdateAgentStatusAsync(string agentId, bool isOnline, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            
            if (_agents.TryGetValue(agentId, out var agent))
            {
                agent.IsOnline = isOnline;
                agent.LastSeen = DateTime.UtcNow;
                
                _logger.LogDebug("Agent status updated: {AgentId} - Online: {IsOnline}", agentId, isOnline);
                AgentStatusChanged?.Invoke(this, agent);
            }
        }

        public async Task<bool> ValidateAgentConnectionAsync(string agentId, string token, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            
            // Simple validation for now - in production, this would validate against a proper auth system
            var isValid = !string.IsNullOrEmpty(agentId) && !string.IsNullOrEmpty(token) && token.Length >= 8;
            
            _logger.LogInformation("Agent connection validation: {AgentId} - Valid: {IsValid}", agentId, isValid);
            return isValid;
        }

        private string GetOrCreateAgentId()
        {
            var agentIdFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RemoteControl", "agent-id.txt");
            
            try
            {
                if (File.Exists(agentIdFile))
                {
                    var existingId = File.ReadAllText(agentIdFile).Trim();
                    if (!string.IsNullOrEmpty(existingId))
                    {
                        return existingId;
                    }
                }

                // Create new agent ID
                var newId = GenerateAgentId();
                
                Directory.CreateDirectory(Path.GetDirectoryName(agentIdFile)!);
                File.WriteAllText(agentIdFile, newId);
                
                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read/write agent ID file, using temporary ID");
                return GenerateAgentId();
            }
        }

        private static string GenerateAgentId()
        {
            // Generate a human-readable agent ID: AAAA-BBBB-CCCC
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            
            var parts = new string[3];
            for (int i = 0; i < 3; i++)
            {
                var part = new char[4];
                for (int j = 0; j < 4; j++)
                {
                    part[j] = chars[random.Next(chars.Length)];
                }
                parts[i] = new string(part);
            }
            
            return string.Join("-", parts);
        }

        private static string GetLocalIPAddress()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                foreach (var networkInterface in networkInterfaces)
                {
                    var properties = networkInterface.GetIPProperties();
                    var addresses = properties.UnicastAddresses
                        .Where(ua => ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(ua => ua.Address);

                    foreach (var address in addresses)
                    {
                        if (!IPAddress.IsLoopback(address))
                        {
                            return address.ToString();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to localhost if unable to determine IP
            }

            return "127.0.0.1";
        }

        private static (int Width, int Height) GetScreenDimensions()
        {
            try
            {
                var screen = Screen.PrimaryScreen;
                if (screen == null)
                    return (1920, 1080); // Default resolution
                return (screen.Bounds.Width, screen.Bounds.Height);
            }
            catch
            {
                return (1920, 1080); // Default fallback
            }
        }

        private static string GetOperatingSystemInfo()
        {
            try
            {
                var os = Environment.OSVersion;
                return $"{os.Platform} {os.Version}";
            }
            catch
            {
                return "Windows";
            }
        }

        private static string GetAssemblyVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }
    }
}
