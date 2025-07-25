using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Agent.Services
{
    public class AgentBackgroundService : BackgroundService
    {
        private readonly ILogger<AgentBackgroundService> _logger;
        private readonly ITransportService _transportService;
        private readonly IScreenCaptureService _screenCaptureService;
        private readonly IInputSimulationService _inputSimulationService;
        private readonly IAgentService _agentService;
        private bool _isRunning = false;

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<string>? ConnectionReceived;
        public event EventHandler<string>? ActivityLogged;

        public AgentBackgroundService(
            ILogger<AgentBackgroundService> logger,
            ITransportService transportService,
            IScreenCaptureService screenCaptureService,
            IInputSimulationService inputSimulationService,
            IAgentService agentService)
        {
            _logger = logger;
            _transportService = transportService;
            _screenCaptureService = screenCaptureService;
            _inputSimulationService = inputSimulationService;
            _agentService = agentService;
        }

        public async Task StartAsync(int port, int maxConnections, int frameRate, int imageQuality)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Service is already running");
            }

            try
            {
                // Register agent
                var agentInfo = await _agentService.GetLocalAgentInfoAsync();
                await _agentService.RegisterAgentAsync(agentInfo);

                // Start transport service
                await _transportService.StartAsync(CancellationToken.None);

                // Subscribe to events
                _transportService.ConnectionStateChanged += OnConnectionStateChanged;
                _transportService.FrameDataReceived += OnFrameDataReceived;

                _isRunning = true;
                StatusChanged?.Invoke(this, $"Service running on port {port}");
                ActivityLogged?.Invoke(this, $"Service started - Port: {port}, Max connections: {maxConnections}");
                
                _logger.LogInformation("Agent service started on port {Port}", port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start agent service");
                StatusChanged?.Invoke(this, "Failed to start service");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            try
            {
                // Stop screen capture if running
                if (_screenCaptureService.IsCapturing)
                {
                    await _screenCaptureService.StopContinuousCaptureAsync();
                }

                // Stop transport service
                await _transportService.StopAsync(CancellationToken.None);

                // Unsubscribe from events
                _transportService.ConnectionStateChanged -= OnConnectionStateChanged;
                _transportService.FrameDataReceived -= OnFrameDataReceived;

                _isRunning = false;
                StatusChanged?.Invoke(this, "Service stopped");
                ActivityLogged?.Invoke(this, "Service stopped");
                
                _logger.LogInformation("Agent service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping agent service");
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Agent background service started");
            
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Heartbeat and status monitoring
                    if (_isRunning)
                    {
                        try
                        {
                            // Update agent status
                            var agentInfo = await _agentService.GetLocalAgentInfoAsync();
                            await _agentService.UpdateAgentStatusAsync(agentInfo.AgentId, true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to update agent status");
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent background service");
            }
            finally
            {
                if (_isRunning)
                {
                    await StopAsync();
                }
            }
        }

        private async void OnConnectionStateChanged(object? sender, Core.Events.ConnectionStateChangedEventArgs e)
        {
            try
            {
                var message = $"Connection state changed: {e.NewState}";
                ActivityLogged?.Invoke(this, message);
                
                if (e.NewState == Core.Enums.ConnectionState.Connected)
                {
                    ConnectionReceived?.Invoke(this, "New viewer connected");
                    
                    // Start screen capture when connected
                    if (!_screenCaptureService.IsCapturing)
                    {
                        await _screenCaptureService.StartContinuousCaptureAsync(
                            frame => {
                                // Send frame data to viewer
                                // This would be implemented based on the transport protocol
                                _logger.LogDebug("Captured frame {FrameNumber}", frame.FrameNumber);
                            },
                            frameRate: 30);
                        
                        ActivityLogged?.Invoke(this, "Screen capture started");
                    }
                }
                else if (e.NewState == Core.Enums.ConnectionState.Disconnected)
                {
                    // Stop screen capture when disconnected
                    if (_screenCaptureService.IsCapturing)
                    {
                        await _screenCaptureService.StopContinuousCaptureAsync();
                        ActivityLogged?.Invoke(this, "Screen capture stopped");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling connection state change");
            }
        }

        private async void OnFrameDataReceived(object? sender, byte[] data)
        {
            try
            {
                // This would handle input events from the viewer
                // For now, just log that data was received
                _logger.LogDebug("Received {DataSize} bytes from viewer", data.Length);
                
                // In a real implementation, this would deserialize input events
                // and send them to the input simulation service
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing received data");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await StopAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
