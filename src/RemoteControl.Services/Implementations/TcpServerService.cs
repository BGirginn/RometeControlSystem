using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    public class TcpServerService : ITcpServerService, IDisposable
    {
        private readonly ILogger<TcpServerService> _logger;
        private readonly IAgentService _agentService;
        private readonly IScreenCaptureService _screenCaptureService;
        private readonly ISessionService _sessionService;
        
        private TcpListener? _tcpListener;
        private CancellationTokenSource? _cancellationTokenSource;

        public bool IsListening => _tcpListener?.Server?.IsBound == true;
        public int Port { get; private set; } = 7777;

        public event EventHandler<string>? ClientConnected;
        public event EventHandler<string>? ClientDisconnected;
        public event EventHandler<string>? StatusChanged;
        
        private bool _isRunning;

        public TcpServerService(
            ILogger<TcpServerService> logger,
            IAgentService agentService,
            IScreenCaptureService screenCaptureService,
            ISessionService sessionService)
        {
            _logger = logger;
            _agentService = agentService;
            _screenCaptureService = screenCaptureService;
            _sessionService = sessionService;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return StartAsync(Port, cancellationToken);
        }

        public Task StartAsync(int port, CancellationToken cancellationToken = default)
        {
            if (_isRunning)
                throw new InvalidOperationException("Server is already running");

            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, port);
                _tcpListener.Start();
                
                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;

                _logger.LogInformation("TCP Server started on port {Port}", port);
                StatusChanged?.Invoke(this, $"Server listening on port {port}");

                // Start accepting connections
                _ = Task.Run(async () => await AcceptClientsLoop(_cancellationTokenSource.Token), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start TCP server on port {Port}", port);
                StatusChanged?.Invoke(this, $"Failed to start server: {ex.Message}");
                throw;
            }
            
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!_isRunning)
                return;

            try
            {
                _isRunning = false;
                _cancellationTokenSource?.Cancel();
                _tcpListener?.Stop();

                _logger.LogInformation("TCP Server stopped");
                StatusChanged?.Invoke(this, "Server stopped");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping TCP server");
            }
        }

        private async Task AcceptClientsLoop(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await _tcpListener!.AcceptTcpClientAsync();
                    var clientEndpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
                    
                    _logger.LogInformation("Client connected: {ClientEndpoint}", clientEndpoint);
                    ClientConnected?.Invoke(this, clientEndpoint);

                    // Handle client in separate task
                    _ = Task.Run(async () => await HandleClientAsync(tcpClient, clientEndpoint, cancellationToken), cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // Expected when stopping the server
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning) // Only log if we're supposed to be running
                    {
                        _logger.LogError(ex, "Error accepting client connection");
                    }
                }
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient, string clientEndpoint, CancellationToken cancellationToken)
        {
            NetworkStream? stream = null;
            try
            {
                stream = tcpClient.GetStream();

                // Handle authentication
                var authResult = await HandleAuthenticationAsync(stream, cancellationToken);
                if (!authResult.Success)
                {
                    _logger.LogWarning("Authentication failed for client {ClientEndpoint}: {Message}", clientEndpoint, authResult.Message);
                    return;
                }

                _logger.LogInformation("Client {ClientEndpoint} authenticated successfully", clientEndpoint);

                // Create session
                var agentInfo = await _agentService.GetLocalAgentInfoAsync();
                var session = await _sessionService.CreateSessionAsync(authResult.ViewerId!, agentInfo.AgentId);

                // Start screen streaming
                await StartScreenStreamingAsync(stream, session.SessionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client {ClientEndpoint}", clientEndpoint);
            }
            finally
            {
                try
                {
                    stream?.Close();
                    tcpClient.Close();
                    ClientDisconnected?.Invoke(this, clientEndpoint);
                    _logger.LogInformation("Client {ClientEndpoint} disconnected", clientEndpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing client connection {ClientEndpoint}", clientEndpoint);
                }
            }
        }

        private async Task<(bool Success, string? ViewerId, string? Message)> HandleAuthenticationAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            try
            {
                // Read auth request
                var headerBuffer = new byte[4];
                var bytesRead = 0;
                while (bytesRead < 4)
                {
                    var read = await stream.ReadAsync(headerBuffer.AsMemory(bytesRead, 4 - bytesRead), cancellationToken);
                    if (read == 0) return (false, null, "Connection closed");
                    bytesRead += read;
                }
                
                var requestLength = BitConverter.ToInt32(headerBuffer, 0);

                if (requestLength <= 0 || requestLength > 1024 * 1024)
                    return (false, null, "Invalid request length");

                var requestBuffer = new byte[requestLength];
                bytesRead = 0;
                while (bytesRead < requestLength)
                {
                    var read = await stream.ReadAsync(requestBuffer.AsMemory(bytesRead, requestLength - bytesRead), cancellationToken);
                    if (read == 0) return (false, null, "Connection closed");
                    bytesRead += read;
                }

                var json = Encoding.UTF8.GetString(requestBuffer);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var type = root.GetProperty("Type").GetString();
                if (type != "AUTH_REQUEST")
                    return (false, null, "Invalid request type");

                var targetId = root.GetProperty("TargetId").GetString();
                var token = root.GetProperty("Token").GetString();

                // Validate authentication (simplified for demo)
                var isValid = await _agentService.ValidateAgentConnectionAsync(targetId ?? "", token ?? "");
                
                // Send response
                var response = new
                {
                    Type = "AUTH_RESPONSE",
                    Success = isValid,
                    Message = isValid ? "Authentication successful" : "Invalid credentials",
                    AgentInfo = isValid ? await _agentService.GetLocalAgentInfoAsync() : null
                };

                var responseJson = JsonSerializer.Serialize(response);
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                var responseHeader = BitConverter.GetBytes(responseBytes.Length);

                await stream.WriteAsync(responseHeader, cancellationToken);
                await stream.WriteAsync(responseBytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);

                return (isValid, targetId, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return (false, null, $"Authentication error: {ex.Message}");
            }
        }

        private async Task StartScreenStreamingAsync(NetworkStream stream, string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                await _screenCaptureService.StartContinuousCaptureAsync(frameData =>
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (stream.CanWrite && !cancellationToken.IsCancellationRequested)
                            {
                                var header = BitConverter.GetBytes(frameData.Length);
                                await stream.WriteAsync(header, cancellationToken);
                                await stream.WriteAsync(frameData, cancellationToken);
                                await stream.FlushAsync(cancellationToken);

                                // Update session metrics
                                await _sessionService.UpdateSessionMetricsAsync(sessionId, frameData.Length, 1);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error sending frame to client");
                        }
                    }, cancellationToken);
                }, 30, cancellationToken);

                // Keep connection alive until cancelled
                while (!cancellationToken.IsCancellationRequested && stream.CanWrite)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in screen streaming");
            }
            finally
            {
                await _screenCaptureService.StopContinuousCaptureAsync();
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _tcpListener?.Stop();
        }
    }
}
