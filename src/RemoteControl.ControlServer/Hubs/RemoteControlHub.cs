using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteControl.ControlServer.Services;
using RemoteControl.Protocol.Messages;
using RemoteControl.Protocol.Serialization;
using System.Security.Claims;

namespace RemoteControl.ControlServer.Hubs;

/// <summary>
/// SignalR hub for remote control communication
/// </summary>
[Authorize]
public class RemoteControlHub : Hub
{
    private readonly ILogger<RemoteControlHub> _logger;
    private readonly IAgentService _agentService;
    private readonly IViewerService _viewerService;
    private readonly ISessionService _sessionService;

    public RemoteControlHub(
        ILogger<RemoteControlHub> logger,
        IAgentService agentService,
        IViewerService viewerService,
        ISessionService sessionService)
    {
        _logger = logger;
        _agentService = agentService;
        _viewerService = viewerService;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        
        _logger.LogInformation("Client connected: {ConnectionId}, User: {Username}", Context.ConnectionId, username);
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Add to user group for direct messaging
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        _logger.LogInformation("Client disconnected: {ConnectionId}, User: {UserId}, Exception: {Exception}", 
            Context.ConnectionId, userId, exception?.Message);

        // Handle cleanup
        if (!string.IsNullOrEmpty(userId))
        {
            // Check if this was an agent
            var agent = await _agentService.GetAgentByConnectionIdAsync(Context.ConnectionId);
            if (agent != null)
            {
                await _agentService.SetAgentOfflineAsync(agent.AgentId);
                _logger.LogInformation("Agent {AgentId} went offline", agent.AgentId);
            }

            // Check if this was a viewer with active sessions
            var activeSessions = await _sessionService.GetActiveSessionsForConnectionAsync(Context.ConnectionId);
            foreach (var session in activeSessions)
            {
                await _sessionService.EndSessionAsync(session.SessionId, "Client disconnected");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{session.SessionId}");
                
                // Notify other participants
                await Clients.Group($"session_{session.SessionId}").SendAsync("SessionEnded", 
                    MessageSerializer.Serialize(new SessionEndedMessage
                    {
                        Payload = new SessionEndedPayload
                        {
                            SessionId = session.SessionId,
                            Reason = "Participant disconnected"
                        }
                    }));
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Register an agent
    /// </summary>
    public async Task RegisterAgent(string messageJson)
    {
        try
        {
            var message = MessageSerializer.Deserialize<RegisterAgentMessage>(messageJson);
            if (message == null)
            {
                _logger.LogWarning("Invalid RegisterAgent message from {ConnectionId}", Context.ConnectionId);
                return;
            }

            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await SendErrorAsync("AUTHENTICATION_REQUIRED", "User authentication required");
                return;
            }

            var agentId = await _agentService.RegisterAgentAsync(new AgentRegistrationInfo
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                Username = message.Payload.Username,
                MachineName = message.Payload.MachineName,
                OperatingSystem = message.Payload.OperatingSystem,
                ScreenWidth = message.Payload.ScreenWidth,
                ScreenHeight = message.Payload.ScreenHeight,
                Capabilities = message.Payload.Capabilities
            });

            // Send response
            var response = new AgentRegisteredMessage
            {
                Payload = new AgentRegisteredPayload
                {
                    AgentId = agentId,
                    Success = true,
                    Message = "Agent registered successfully"
                }
            };

            await Clients.Caller.SendAsync("ReceiveMessage", MessageSerializer.Serialize(response));
            
            _logger.LogInformation("Agent registered: {AgentId} for user {UserId}", agentId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering agent for connection {ConnectionId}", Context.ConnectionId);
            await SendErrorAsync("REGISTRATION_FAILED", $"Agent registration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Request a session with an agent
    /// </summary>
    public async Task RequestSession(string messageJson)
    {
        try
        {
            var message = MessageSerializer.Deserialize<RequestSessionMessage>(messageJson);
            if (message == null)
            {
                _logger.LogWarning("Invalid RequestSession message from {ConnectionId}", Context.ConnectionId);
                return;
            }

            var viewerId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(viewerId))
            {
                await SendErrorAsync("AUTHENTICATION_REQUIRED", "User authentication required");
                return;
            }

            // Get target agent
            var agent = await _agentService.GetAgentAsync(message.Payload.TargetAgentId);
            if (agent == null || !agent.IsOnline)
            {
                await SendErrorAsync("AGENT_NOT_AVAILABLE", "Target agent is not available");
                return;
            }

            // Create session request
            var sessionRequest = await _sessionService.CreateSessionRequestAsync(new SessionRequestInfo
            {
                ViewerId = viewerId,
                ViewerConnectionId = Context.ConnectionId,
                AgentId = message.Payload.TargetAgentId,
                AgentConnectionId = agent.ConnectionId,
                ViewerUsername = message.Payload.ViewerUsername,
                ViewerMachineName = message.Payload.ViewerMachineName,
                RequestReason = message.Payload.RequestReason,
                ViewerCapabilities = message.Payload.ViewerCapabilities
            });

            // Forward request to agent
            await Clients.Client(agent.ConnectionId).SendAsync("ReceiveMessage", messageJson);
            
            _logger.LogInformation("Session request {SessionId} forwarded from viewer {ViewerId} to agent {AgentId}", 
                sessionRequest.SessionId, viewerId, message.Payload.TargetAgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing session request from {ConnectionId}", Context.ConnectionId);
            await SendErrorAsync("SESSION_REQUEST_FAILED", $"Session request failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle session decision from agent
    /// </summary>
    public async Task SessionDecision(string messageJson)
    {
        try
        {
            var message = MessageSerializer.Deserialize<SessionDecisionMessage>(messageJson);
            if (message == null)
            {
                _logger.LogWarning("Invalid SessionDecision message from {ConnectionId}", Context.ConnectionId);
                return;
            }

            var session = await _sessionService.GetSessionRequestAsync(message.Payload.SessionId);
            if (session == null)
            {
                await SendErrorAsync("SESSION_NOT_FOUND", "Session request not found");
                return;
            }

            if (message.Payload.Accepted)
            {
                // Start session
                await _sessionService.StartSessionAsync(message.Payload.SessionId, message.Payload.SessionSettings);
                
                // Add both participants to session group
                await Groups.AddToGroupAsync(session.ViewerConnectionId, $"session_{message.Payload.SessionId}");
                await Groups.AddToGroupAsync(session.AgentConnectionId, $"session_{message.Payload.SessionId}");

                // Notify viewer of session start
                var sessionStarted = new SessionStartedMessage
                {
                    Payload = new SessionStartedPayload
                    {
                        SessionId = message.Payload.SessionId,
                        AgentId = session.AgentId,
                        ViewerId = session.ViewerId,
                        SessionSettings = message.Payload.SessionSettings ?? new SessionSettings()
                    }
                };

                await Clients.Client(session.ViewerConnectionId).SendAsync("ReceiveMessage", 
                    MessageSerializer.Serialize(sessionStarted));
                
                _logger.LogInformation("Session {SessionId} started between viewer {ViewerId} and agent {AgentId}", 
                    message.Payload.SessionId, session.ViewerId, session.AgentId);
            }
            else
            {
                // Session rejected
                await _sessionService.RejectSessionAsync(message.Payload.SessionId, message.Payload.Reason);
                
                // Notify viewer of rejection
                await Clients.Client(session.ViewerConnectionId).SendAsync("ReceiveMessage", messageJson);
                
                _logger.LogInformation("Session {SessionId} rejected: {Reason}", 
                    message.Payload.SessionId, message.Payload.Reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing session decision from {ConnectionId}", Context.ConnectionId);
            await SendErrorAsync("SESSION_DECISION_FAILED", $"Session decision failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Send a message (generic message handler)
    /// </summary>
    public async Task SendMessage(string messageJson)
    {
        // This is called by SignalRClient for general message sending
        await ProcessMessage(messageJson);
    }

    /// <summary>
    /// Send a message to a specific client
    /// </summary>
    public async Task SendMessageToClient(string clientId, string messageJson)
    {
        await Clients.Client(clientId).SendAsync("ReceiveMessage", messageJson);
    }

    /// <summary>
    /// Send a message to a group
    /// </summary>
    public async Task SendMessageToGroup(string groupName, string messageJson)
    {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", messageJson);
    }

    /// <summary>
    /// Join a group
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a group
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    private async Task ProcessMessage(string messageJson)
    {
        try
        {
            if (!MessageSerializer.TryDeserialize(messageJson, out var message) || message == null)
            {
                _logger.LogWarning("Invalid message from {ConnectionId}: {Json}", Context.ConnectionId, messageJson);
                return;
            }

            switch (message.Type)
            {
                case "RegisterAgent":
                    await RegisterAgent(messageJson);
                    break;

                case "RequestSession":
                    await RequestSession(messageJson);
                    break;

                case "SessionDecision":
                    await SessionDecision(messageJson);
                    break;

                case "Frame":
                case "InputEvent":
                case "KeepAlive":
                    // Forward session-related messages to session group
                    await ForwardSessionMessage(messageJson);
                    break;

                default:
                    _logger.LogWarning("Unknown message type: {MessageType} from {ConnectionId}", 
                        message.Type, Context.ConnectionId);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {ConnectionId}", Context.ConnectionId);
            await SendErrorAsync("MESSAGE_PROCESSING_ERROR", $"Error processing message: {ex.Message}");
        }
    }

    private async Task ForwardSessionMessage(string messageJson)
    {
        // Extract session ID and forward to session group
        using var doc = System.Text.Json.JsonDocument.Parse(messageJson);
        if (doc.RootElement.TryGetProperty("payload", out var payload) &&
            payload.TryGetProperty("sessionId", out var sessionIdProperty))
        {
            var sessionId = sessionIdProperty.GetString();
            if (!string.IsNullOrEmpty(sessionId))
            {
                await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", messageJson);
                return;
            }
        }

        _logger.LogWarning("Unable to extract session ID from message: {MessageJson}", messageJson);
    }

    private async Task SendErrorAsync(string errorCode, string message)
    {
        var errorMessage = new ErrorMessage
        {
            Payload = new ErrorPayload
            {
                ErrorCode = errorCode,
                Message = message,
                Severity = ErrorSeverity.Error,
                Category = ErrorCategory.General
            }
        };

        await Clients.Caller.SendAsync("ReceiveMessage", MessageSerializer.Serialize(errorMessage));
    }
} 