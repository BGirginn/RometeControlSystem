using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Events;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    /// <summary>
    /// Real TCP-based transport service implementation
    /// </summary>
    public class TcpTransportService : ITransportService, IDisposable
    {
        private readonly ILogger<TcpTransportService> _logger;
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _receiveCancellation;
        private Core.Enums.ConnectionState _currentState = Core.Enums.ConnectionState.Disconnected;

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        public event EventHandler<byte[]>? FrameDataReceived;

        public TcpTransportService(ILogger<TcpTransportService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            _logger.LogInformation("TCP Transport Service started");
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await DisconnectAsync(cancellationToken);
            _logger.LogInformation("TCP Transport Service stopped");
        }

        public async Task ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting connection to {TargetId}", request.TargetId);
                
                ChangeState(Core.Enums.ConnectionState.Resolving, "Resolving target ID...");
                
                // Parse target ID to get host and port
                var (host, port) = ParseTargetId(request.TargetId);
                await Task.Delay(500, cancellationToken); // Brief delay for user feedback
                
                ChangeState(Core.Enums.ConnectionState.Connecting, "Establishing TCP connection...");
                
                _tcpClient = new TcpClient();
                // Set timeout for connection
                var connectTask = _tcpClient.ConnectAsync(host, port, cancellationToken);
                var timeoutTask = Task.Delay(10000, cancellationToken); // 10 second timeout
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException($"Connection to {host}:{port} timed out");
                }
                
                await connectTask; // Re-await to get any exceptions
                _networkStream = _tcpClient.GetStream();
                
                ChangeState(Core.Enums.ConnectionState.Authenticating, "Authenticating...");
                
                // Send authentication request
                await SendAuthenticationAsync(request, cancellationToken);
                
                // Wait for authentication response
                var authResponse = await ReceiveAuthenticationResponseAsync(cancellationToken);
                if (!authResponse.Success)
                {
                    throw new UnauthorizedAccessException(authResponse.Message ?? "Authentication failed");
                }
                
                ChangeState(Core.Enums.ConnectionState.Connected, "Connected successfully");
                
                // Start receiving data
                _ = Task.Run(() => ReceiveDataLoop(cancellationToken), cancellationToken);
                
                _logger.LogInformation("Successfully connected to {TargetId}", request.TargetId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to {TargetId}", request.TargetId);
                ChangeState(Core.Enums.ConnectionState.Disconnected, $"Connection failed: {ex.Message}");
                await DisconnectAsync(cancellationToken);
                throw;
            }
        }
                
                ChangeState(Core.Enums.ConnectionState.Authenticating, "Authenticating...");
                
                // Send authentication request
                var authJson = JsonSerializer.Serialize(request);
                var authData = Encoding.UTF8.GetBytes(authJson);
                await _networkStream.WriteAsync(authData, cancellationToken);
                
                // Wait for auth response
                var buffer = new byte[1024];
                var bytesRead = await _networkStream.ReadAsync(buffer, cancellationToken);
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                if (response != "AUTH_OK")
                {
                    throw new InvalidOperationException($"Authentication failed: {response}");
                }
                
                ChangeState(Core.Enums.ConnectionState.Connected, "Connected successfully");
                ChangeState(Core.Enums.ConnectionState.Streaming, "Streaming started");
                
                // Start receiving frames
                _receiveCancellation = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveFrameLoop(_receiveCancellation.Token), _receiveCancellation.Token);
                
                _logger.LogInformation("Connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection failed");
                ChangeState(Core.Enums.ConnectionState.Error, $"Connection failed: {ex.Message}", ex);
                throw;
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_currentState != Core.Enums.ConnectionState.Disconnected)
                {
                    ChangeState(Core.Enums.ConnectionState.Disconnecting, "Disconnecting...");
                    
                    _receiveCancellation?.Cancel();
                    
                    _networkStream?.Close();
                    _tcpClient?.Close();
                    
                    ChangeState(Core.Enums.ConnectionState.Disconnected, "Disconnected");
                    
                    _logger.LogInformation("Disconnected successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnect");
                ChangeState(Core.Enums.ConnectionState.Error, $"Disconnect error: {ex.Message}", ex);
            }
        }

        public Task<bool> IsConnectedAsync()
        {
            return Task.FromResult(_currentState == Core.Enums.ConnectionState.Connected || 
                                  _currentState == Core.Enums.ConnectionState.Streaming);
        }

        private (string host, int port) ResolveTarget(string targetId)
        {
            // TODO: Implement actual target resolution (could be via discovery service, DNS, etc.)
            // For now, assume targetId is in format "host:port" or use default port
            if (targetId.Contains(':'))
            {
                var parts = targetId.Split(':');
                return (parts[0], int.Parse(parts[1]));
            }
            
            return (targetId, 5900); // Default VNC-like port
        }

        private async Task ReceiveFrameLoop(CancellationToken cancellationToken)
        {
            if (_networkStream == null) return;

            var buffer = new byte[64 * 1024]; // 64KB buffer
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && _networkStream.CanRead)
                {
                    // Read frame header (size)
                    var headerBuffer = new byte[4];
                    var headerBytesRead = 0;
                    
                    while (headerBytesRead < 4)
                    {
                        var read = await _networkStream.ReadAsync(
                            headerBuffer.AsMemory(headerBytesRead, 4 - headerBytesRead), 
                            cancellationToken);
                        
                        if (read == 0) break; // Connection closed
                        headerBytesRead += read;
                    }
                    
                    if (headerBytesRead < 4) break;
                    
                    var frameSize = BitConverter.ToInt32(headerBuffer, 0);
                    if (frameSize <= 0 || frameSize > 10 * 1024 * 1024) // Max 10MB frame
                    {
                        _logger.LogWarning("Invalid frame size: {FrameSize}", frameSize);
                        continue;
                    }
                    
                    // Read frame data
                    var frameData = new byte[frameSize];
                    var frameBytesRead = 0;
                    
                    while (frameBytesRead < frameSize)
                    {
                        var read = await _networkStream.ReadAsync(
                            frameData.AsMemory(frameBytesRead, frameSize - frameBytesRead), 
                            cancellationToken);
                        
                        if (read == 0) break; // Connection closed
                        frameBytesRead += read;
                    }
                    
                    if (frameBytesRead == frameSize)
                    {
                        FrameDataReceived?.Invoke(this, frameData);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in frame receive loop");
                ChangeState(Core.Enums.ConnectionState.Error, $"Frame receive error: {ex.Message}", ex);
            }
        }

        private static (string Host, int Port) ParseTargetId(string targetId)
        {
            // Support formats: "hostname:port", "ip:port", or just "AgentID" (defaults to port 7777)
            if (string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("Target ID cannot be empty", nameof(targetId));

            // If it contains a colon, parse host:port
            if (targetId.Contains(':'))
            {
                var parts = targetId.Split(':');
                if (parts.Length != 2)
                    throw new ArgumentException($"Invalid 