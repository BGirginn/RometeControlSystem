using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteControl.Transport.Interfaces;
using RemoteControl.Protocol.Messages;
using RemoteControl.Core.Models;
using System.Drawing;
using System.Security.Claims;
using RemoteControl.Protocol.Serialization;
using System.Windows;

namespace RemoteControl.Agent.Services;

public class ModernAgentService : BackgroundService
{
    private readonly ILogger<ModernAgentService> _logger;
    private readonly ISignalRClient _signalRClient;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly IOptions<ControlServerOptions> _serverOptions;
    
    private string? _agentId;
    private string? _authToken;
    private System.Threading.Timer? _heartbeatTimer;
    
    public event EventHandler<string>? ActivityLogged;
    public event EventHandler<RequestSessionMessage>? SessionRequested;
    public event EventHandler<bool>? ConnectionStateChanged;
    
    public bool IsConnected => _signalRClient.IsConnected;
    public string? AgentId => _agentId;

    public ModernAgentService(
        ILogger<ModernAgentService> logger,
        ISignalRClient signalRClient,
        IOptions<AgentOptions> agentOptions,
        IOptions<ControlServerOptions> serverOptions)
    {
        _logger = logger;
        _signalRClient = signalRClient;
        _agentOptions = agentOptions;
        _serverOptions = serverOptions;
        
        // Subscribe to SignalR events
        _signalRClient.MessageReceived += OnMessageReceived;
        _signalRClient.ConnectionStateChanged += OnConnectionStateChanged;
        _signalRClient.ErrorOccurred += OnErrorOccurred;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Modern Agent Service starting...");
            ActivityLogged?.Invoke(this, "Agent service starting...");

            // Start connection to ControlServer
            await ConnectToControlServerAsync(stoppingToken);

            // Start heartbeat timer
            _heartbeatTimer = new System.Threading.Timer(SendHeartbeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            
            // Keep service running
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_signalRClient.IsConnected)
                {
                    _logger.LogWarning("Connection lost, attempting to reconnect...");
                    await ConnectToControlServerAsync(stoppingToken);
                }
                
                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Modern Agent Service cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Modern Agent Service");
            ActivityLogged?.Invoke(this, $"Service error: {ex.Message}");
            throw;
        }
    }

    private async Task ConnectToControlServerAsync(CancellationToken cancellationToken)
    {
        try
        {
            ActivityLogged?.Invoke(this, "Connecting to ControlServer...");
            
            // Authenticate first
            await AuthenticateAsync();
            
            // Connect to SignalR hub
            await _signalRClient.ConnectAsync(_serverOptions.Value.Url, _authToken, cancellationToken);
            
            // Register as agent
            await RegisterAgentAsync();
            
            ActivityLogged?.Invoke(this, $"Connected as Agent: {_agentId}");
            _logger.LogInformation("Successfully connected to ControlServer as Agent: {AgentId}", _agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to ControlServer");
            ActivityLogged?.Invoke(this, $"Connection failed: {ex.Message}");
            throw;
        }
    }

    private async Task AuthenticateAsync()
    {
        // TODO: Implement actual authentication with ControlServer REST API
        // For now, we'll use a placeholder token
        _authToken = "placeholder_jwt_token";
        await Task.CompletedTask;
    }

    private async Task RegisterAgentAsync()
    {
        var agentCapabilities = new AgentCapabilities
        {
            SupportedEncodings = _agentOptions.Value.SupportedCodecs ?? new[] { "h264", "jpeg" },
            MaxFps = _agentOptions.Value.MaxFps,
            H264 = _agentOptions.Value.SupportedCodecs?.Contains("H264") == true,
            HasAudio = false // Not implemented yet
        };

        var registerMessage = new RegisterAgentMessage
        {
            Payload = new RegisterAgentPayload
            {
                Username = Environment.UserName,
                MachineName = Environment.MachineName,
                OperatingSystem = Environment.OSVersion.ToString(),
                ScreenWidth = (int)SystemParameters.PrimaryScreenWidth,
                ScreenHeight = (int)SystemParameters.PrimaryScreenHeight,
                Capabilities = agentCapabilities
            }
        };

        await _signalRClient.SendMessageAsync(registerMessage);
    }

    private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            _logger.LogDebug("Received message: {Type}", e.Message.Type);
            
            switch (e.Message)
            {
                case AgentRegisteredMessage registeredMsg:
                    await HandleAgentRegistered(registeredMsg);
                    break;
                    
                case RequestSessionMessage sessionMsg:
                    await HandleSessionRequest(sessionMsg);
                    break;
                    
                case SessionEndedMessage endedMsg:
                    await HandleSessionEnded(endedMsg);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown message type: {Type}", e.Message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received message");
        }
    }

    private async Task HandleAgentRegistered(AgentRegisteredMessage message)
    {
        _agentId = message.Payload.AgentId;
        ActivityLogged?.Invoke(this, $"Agent registered successfully: {_agentId}");
        await Task.CompletedTask;
    }

    private async Task HandleSessionRequest(RequestSessionMessage message)
    {
        _logger.LogInformation("Session request received from viewer: {ViewerUsername}", 
            message.Payload.ViewerUsername);
        
        ActivityLogged?.Invoke(this, 
            $"Session request from {message.Payload.ViewerUsername} ({message.Payload.ViewerMachineName})");
        
        // Raise event for UI to handle approval
        SessionRequested?.Invoke(this, message);
        
        await Task.CompletedTask;
    }

    public async Task RespondToSessionRequestAsync(string sessionId, bool accepted, string? reason = null)
    {
        var response = new SessionDecisionMessage
        {
            Payload = new SessionDecisionPayload
            {
                SessionId = sessionId,
                Accepted = accepted,
                Reason = reason,
                SessionSettings = accepted ? new SessionSettings
                {
                    Fps = _agentOptions.Value.MaxFps,
                    Quality = _agentOptions.Value.DefaultQuality,
                    Encoding = "h264"
                } : null
            }
        };

        await _signalRClient.SendMessageAsync(response);
        
        var status = accepted ? "accepted" : "rejected";
        ActivityLogged?.Invoke(this, $"Session request {status}");
        _logger.LogInformation("Session request {Status}: {SessionId}", status, sessionId);
    }

    private async Task HandleSessionEnded(SessionEndedMessage message)
    {
        ActivityLogged?.Invoke(this, "Session ended");
        _logger.LogInformation("Session ended: {SessionId}, Reason: {Reason}", 
            message.Payload.SessionId, message.Payload.Reason);
        await Task.CompletedTask;
    }

    private async void SendHeartbeat(object? state)
    {
        try
        {
            if (_signalRClient.IsConnected)
            {
                var keepAlive = new KeepAliveMessage();
                await _signalRClient.SendMessageAsync(keepAlive);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send heartbeat");
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        var isConnected = e.NewState == "Connected";
        ConnectionStateChanged?.Invoke(this, isConnected);
        var status = isConnected ? "Connected" : "Disconnected";
        ActivityLogged?.Invoke(this, $"Connection status: {status}");
    }

    private void OnErrorOccurred(object? sender, TransportErrorEventArgs e)
    {
        _logger.LogError(e.Exception, "SignalR client error: {ErrorCode} - {Message}", e.ErrorCode, e.Message);
        ActivityLogged?.Invoke(this, $"Connection error: {e.Message}");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Modern Agent Service stopping...");
        ActivityLogged?.Invoke(this, "Agent service stopping...");
        
        _heartbeatTimer?.Dispose();
        
        if (_signalRClient.IsConnected)
        {
            await _signalRClient.DisconnectAsync();
        }
        
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Modern Agent Service stopped");
    }
}

public class AgentOptions
{
    public int MaxFps { get; set; } = 30;
    public int DefaultQuality { get; set; } = 85;
    public string[]? SupportedCodecs { get; set; }
    public bool RequireApproval { get; set; } = true;
    public bool AutoStartService { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
}

public class ControlServerOptions
{
    public string Url { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
} 