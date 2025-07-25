using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Enums;
using RemoteControl.Core.Events;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Agent.Services
{
    public class AgentBackgroundService : BackgroundService
    {
        private readonly ILogger<AgentBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITcpServerService _tcpServerService;
        private readonly IScreenCaptureService _screenCaptureService;
        private readonly ITransportService _transportService;

        public event EventHandler<string>? ActivityLogged;

        public AgentBackgroundService(
            ILogger<AgentBackgroundService> logger,
            IServiceProvider serviceProvider,
            ITcpServerService tcpServerService,
            IScreenCaptureService screenCaptureService,
            ITransportService transportService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _tcpServerService = tcpServerService;
            _screenCaptureService = screenCaptureService;
            _transportService = transportService;

            // Subscribe to events
            _transportService.ConnectionStateChanged += OnConnectionStateChanged;
            _transportService.FrameDataReceived += OnFrameDataReceived;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Agent Background Service starting...");
                ActivityLogged?.Invoke(this, "Agent service starting...");

                // Start the TCP server to listen for connections
                await _tcpServerService.StartAsync(stoppingToken);
                
                ActivityLogged?.Invoke(this, "Agent is ready and listening for connections");
                _logger.LogInformation("Agent Background Service started successfully");

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Agent Background Service cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Agent Background Service");
                ActivityLogged?.Invoke(this, $"Error: {ex.Message}");
                throw;
            }
        }

        private async void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            try
            {
                var message = $"Connection state: {e.NewState}";
                if (!string.IsNullOrEmpty(e.Message))
                {
                    message += $" - {e.Message}";
                }

                _logger.LogInformation("Connection state changed from {OldState} to {NewState}: {Message}",
                    e.OldState, e.NewState, e.Message);

                ActivityLogged?.Invoke(this, message);

                // Handle specific state changes
                if (e.NewState == ConnectionState.Connected)
                {
                    // Start screen capture when a viewer connects
                    if (!_screenCaptureService.IsCapturing)
                    {
                        await _screenCaptureService.StartContinuousCaptureAsync(frame =>
                        {
                            // Frame capture callback - this will be handled by the TCP server
                            _logger.LogTrace("Screen frame captured: {Width}x{Height}, {DataSize} bytes",
                                frame.Width, frame.Height, frame.ImageData.Length);
                        }, 30);
                        
                        ActivityLogged?.Invoke(this, "Screen capture started");
                    }
                }
                else if (e.NewState == ConnectionState.Disconnected)
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
            _logger.LogInformation("Agent Background Service stopping...");
            ActivityLogged?.Invoke(this, "Agent service stopping...");
            
            await _tcpServerService.StopAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
            
            _logger.LogInformation("Agent Background Service stopped");
        }
    }
}
