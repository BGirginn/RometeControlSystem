using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Enums;
using RemoteControl.Core.Events;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    public class TcpTransportService : ITransportService, IDisposable
    {
        private readonly ILogger<TcpTransportService> _logger;
        private readonly IRegistryClientService? _registryClient;
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _receiveCancellation;
        private ConnectionState _currentState = ConnectionState.Disconnected;

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        public event EventHandler<byte[]>? FrameDataReceived;

        public TcpTransportService(ILogger<TcpTransportService> logger, IRegistryClientService? registryClient = null)
        {
            _logger = logger;
            _registryClient = registryClient;
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
                
                ChangeState(ConnectionState.Resolving, "Resolving target ID...");
                
                // Parse target ID to get host and port (now supports device IDs)
                var (host, port) = await ParseTargetIdAsync(request.TargetId, cancellationToken);
                await Task.Delay(500, cancellationToken); // Brief delay for user feedback
                
                ChangeState(ConnectionState.Connecting, "Establishing TCP connection...");
                
                _tcpClient = new TcpClient();
                // Set timeout for connection
                var connectTask = _tcpClient.ConnectAsync(host, port, cancellationToken).AsTask();
                var timeoutTask = Task.Delay(10000, cancellationToken); // 10 second timeout
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException($"Connection to {host}:{port} timed out");
                }
                
                await connectTask; // Re-await to get any exceptions
                _networkStream = _tcpClient.GetStream();
                
                ChangeState(ConnectionState.Authenticating, "Authenticating...");
                
                // Send authentication request
                await SendAuthenticationAsync(request, cancellationToken);
                
                // Wait for authentication response
                var authResponse = await ReceiveAuthenticationResponseAsync(cancellationToken);
                if (!authResponse.Success)
                {
                    throw new UnauthorizedAccessException(authResponse.Message ?? "Authentication failed");
                }
                
                ChangeState(ConnectionState.Connected, "Connected successfully");
                
                // Start receiving data
                _receiveCancellation = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveFrameLoop(_receiveCancellation.Token), _receiveCancellation.Token);
                
                _logger.LogInformation("Successfully connected to {TargetId}", request.TargetId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to {TargetId}", request.TargetId);
                ChangeState(ConnectionState.Disconnected, $"Connection failed: {ex.Message}");
                await DisconnectAsync(cancellationToken);
                throw;
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_tcpClient?.Connected == true || _networkStream != null)
                {
                    ChangeState(ConnectionState.Disconnecting, "Disconnecting...");
                    
                    _receiveCancellation?.Cancel();
                    
                    _networkStream?.Close();
                    _tcpClient?.Close();
                    
                    ChangeState(ConnectionState.Disconnected, "Disconnected");
                    
                    _logger.LogInformation("Disconnected successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnect");
                ChangeState(ConnectionState.Error, $"Disconnect error: {ex.Message}", ex);
            }
            finally
            {
                _networkStream?.Dispose();
                _tcpClient?.Dispose();
                _networkStream = null;
                _tcpClient = null;
            }

            await Task.CompletedTask;
        }

        public Task<bool> IsConnectedAsync()
        {
            var isConnected = _tcpClient?.Connected == true && _networkStream?.CanRead == true;
            return Task.FromResult(isConnected);
        }

        private async Task<(string Host, int Port)> ParseTargetIdAsync(string targetId, CancellationToken cancellationToken)
        {
            // Support formats: 
            // 1. "hostname:port" or "ip:port" - Direct connection
            // 2. "RC-123456" - Device ID from registry
            // 3. "username@devicename" - User device format
            // 4. Just "hostname" or "devicename" - defaults to port 7777
            
            if (string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("Target ID cannot be empty", nameof(targetId));

            // Check if it's a direct IP:port format
            if (targetId.Contains(':') && !targetId.Contains('@'))
            {
                return ParseDirectConnection(targetId);
            }

            // Check if it's a systematic device ID (RC-XXXXXX format)
            if (targetId.StartsWith("RC-", StringComparison.OrdinalIgnoreCase) || targetId.Contains('@'))
            {
                return await ResolveDeviceAsync(targetId, cancellationToken);
            }

            // Assume it's a hostname/IP without port, use default port
            return (targetId, 7777);
        }

        private static (string Host, int Port) ParseDirectConnection(string targetId)
        {
            var parts = targetId.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid target ID format: {targetId}. Expected format: 'host:port'", nameof(targetId));

            if (!int.TryParse(parts[1], out var port) || port <= 0 || port > 65535)
                throw new ArgumentException($"Invalid port number: {parts[1]}", nameof(targetId));

            return (parts[0], port);
        }

        private async Task<(string Host, int Port)> ResolveDeviceAsync(string deviceId, CancellationToken cancellationToken)
        {
            if (_registryClient == null || !_registryClient.IsConnected)
            {
                throw new InvalidOperationException($"Cannot resolve device ID '{deviceId}' - not connected to registry service. Use direct IP:port format instead.");
            }

            try
            {
                var device = await _registryClient.ResolveDeviceAsync(deviceId, cancellationToken);
                if (device == null)
                {
                    throw new ArgumentException($"Device '{deviceId}' not found in registry");
                }

                if (!device.IsOnline)
                {
                    throw new InvalidOperationException($"Device '{deviceId}' is currently offline");
                }

                if (string.IsNullOrEmpty(device.CurrentIP))
                {
                    throw new InvalidOperationException($"Device '{deviceId}' has no current IP address");
                }

                _logger.LogInformation("Resolved device '{DeviceId}' to {IP}:{Port}", deviceId, device.CurrentIP, device.Port);
                return (device.CurrentIP, device.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve device ID: {DeviceId}", deviceId);
                throw new ArgumentException($"Failed to resolve device '{deviceId}': {ex.Message}", nameof(deviceId));
            }
        }

        private async Task SendAuthenticationAsync(ConnectionRequest request, CancellationToken cancellationToken)
        {
            if (_networkStream == null)
                throw new InvalidOperationException("Not connected");

            var authData = new
            {
                Type = "AUTH_REQUEST",
                TargetId = request.TargetId,
                Token = request.UserToken,
                ViewerInfo = new
                {
                    Version = "1.0.0",
                    Capabilities = new[] { "SCREEN_VIEW", "INPUT_CONTROL" }
                }
            };

            var json = JsonSerializer.Serialize(authData);
            var bytes = Encoding.UTF8.GetBytes(json);
            var header = BitConverter.GetBytes(bytes.Length);

            await _networkStream.WriteAsync(header, cancellationToken);
            await _networkStream.WriteAsync(bytes, cancellationToken);
            await _networkStream.FlushAsync(cancellationToken);
        }

        private async Task<(bool Success, string? Message)> ReceiveAuthenticationResponseAsync(CancellationToken cancellationToken)
        {
            if (_networkStream == null)
                throw new InvalidOperationException("Not connected");

            try
            {
                // Read response header (4 bytes for length)
                var headerBuffer = new byte[4];
                var bytesRead = 0;
                while (bytesRead < 4)
                {
                    var read = await _networkStream.ReadAsync(headerBuffer.AsMemory(bytesRead, 4 - bytesRead), cancellationToken);
                    if (read == 0) throw new EndOfStreamException("Connection closed during authentication");
                    bytesRead += read;
                }

                var responseLength = BitConverter.ToInt32(headerBuffer, 0);
                if (responseLength <= 0 || responseLength > 1024 * 1024) // Max 1MB response
                    throw new InvalidDataException($"Invalid response length: {responseLength}");

                // Read response data
                var responseBuffer = new byte[responseLength];
                bytesRead = 0;
                while (bytesRead < responseLength)
                {
                    var read = await _networkStream.ReadAsync(responseBuffer.AsMemory(bytesRead, responseLength - bytesRead), cancellationToken);
                    if (read == 0) throw new EndOfStreamException("Connection closed during authentication");
                    bytesRead += read;
                }

                var json = Encoding.UTF8.GetString(responseBuffer);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var type = root.GetProperty("Type").GetString();
                if (type != "AUTH_RESPONSE")
                    throw new InvalidDataException($"Unexpected response type: {type}");

                var success = root.GetProperty("Success").GetBoolean();
                var message = root.TryGetProperty("Message", out var msgProp) ? msgProp.GetString() : null;

                return (success, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving authentication response");
                return (false, $"Authentication error: {ex.Message}");
            }
        }

        private async Task ReceiveFrameLoop(CancellationToken cancellationToken)
        {
            if (_networkStream == null) return;

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
                ChangeState(ConnectionState.Error, $"Frame receive error: {ex.Message}", ex);
            }
        }

        private void ChangeState(ConnectionState newState, string? message = null, Exception? exception = null)
        {
            var oldState = _currentState;
            _currentState = newState;
            
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(oldState, newState, message, exception));
        }

        public void Dispose()
        {
            _receiveCancellation?.Cancel();
            _receiveCancellation?.Dispose();
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}
