using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RemoteControl.Core.Events;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Viewer.ViewModels
{
    public partial class StreamingViewModel : ObservableObject
    {
        private readonly ITransportService _transportService;
        private readonly IVideoRenderer _videoRenderer;
        private readonly IMessenger _messenger;
        private readonly DispatcherTimer _metricsTimer;

        [ObservableProperty]
        private WriteableBitmap? _frameBuffer;

        [ObservableProperty]
        private bool _isConnected = false;

        [ObservableProperty]
        private bool _isFullscreen = false;

        [ObservableProperty]
        private bool _inputEnabled = true;

        [ObservableProperty]
        private bool _showMetrics = false;

        [ObservableProperty]
        private int _quality = 80;

        [ObservableProperty]
        private int _frameRate = 30;

        [ObservableProperty]
        private string _connectionInfo = string.Empty;

        // Metrics Properties
        [ObservableProperty]
        private int _currentFps = 0;

        [ObservableProperty]
        private int _roundTripTime = 0;

        [ObservableProperty]
        private double _bitrateMbps = 0.0;

        [ObservableProperty]
        private int _queueLength = 0;

        [ObservableProperty]
        private double _encodeTimeMs = 0.0;

        [ObservableProperty]
        private double _decodeTimeMs = 0.0;

        [ObservableProperty]
        private string _metricsText = string.Empty;

        // Session information
        public string SessionId { get; private set; } = Guid.NewGuid().ToString();

        public StreamingViewModel(
            ITransportService transportService,
            IVideoRenderer videoRenderer,
            IMessenger messenger)
        {
            _transportService = transportService;
            _videoRenderer = videoRenderer;
            _messenger = messenger;

            // Subscribe to events
            _transportService.ConnectionStateChanged += OnConnectionStateChanged;
            _transportService.FrameDataReceived += OnFrameDataReceived;
            _videoRenderer.MetricsUpdated += OnMetricsUpdated;

            // Setup metrics timer
            _metricsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000) // Update every second
            };
            _metricsTimer.Tick += (s, e) => UpdateMetricsDisplay();
            _metricsTimer.Start();

            // Initialize connection info
            UpdateConnectionInfo();
        }

        [RelayCommand]
        private void Disconnect()
        {
            try
            {
                _transportService.DisconnectAsync();
                _messenger.Send(new NavigationMessage("Connect"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Disconnect error: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ToggleFullscreen()
        {
            IsFullscreen = !IsFullscreen;
        }

        [RelayCommand]
        private void ToggleInputEnabled()
        {
            InputEnabled = !InputEnabled;
        }

        [RelayCommand]
        private void ToggleMetrics()
        {
            ShowMetrics = !ShowMetrics;
        }

        [RelayCommand]
        private void SetQuality(int quality)
        {
            Quality = quality;
            _videoRenderer.SetQualityAsync(quality);
        }

        [RelayCommand]
        private void SetFrameRate(int frameRate)
        {
            FrameRate = frameRate;
            _videoRenderer.SetFrameRateAsync(frameRate);
        }

        [RelayCommand]
        private void ShowSettings()
        {
            // TODO: Show settings dialog or navigate to settings view
        }

        // Input handling methods
        public async Task SendMouseMoveAsync(double relativeX, double relativeY)
        {
            if (!InputEnabled || !IsConnected) 
                return;
            
            try
            {
                await _transportService.SendMouseMoveAsync(SessionId, relativeX, relativeY);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse move error: {ex.Message}");
            }
        }

        public async Task SendMouseDownAsync(string button)
        {
            if (!InputEnabled || !IsConnected) 
                return;
            
            try
            {
                await _transportService.SendMouseDownAsync(SessionId, button);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse down error: {ex.Message}");
            }
        }

        public async Task SendMouseUpAsync(string button)
        {
            if (!InputEnabled || !IsConnected) 
                return;
            
            try
            {
                await _transportService.SendMouseUpAsync(SessionId, button);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse up error: {ex.Message}");
            }
        }

        public async Task SendMouseWheelAsync(int delta)
        {
            if (!InputEnabled || !IsConnected) 
                return;
            
            try
            {
                await _transportService.SendMouseWheelAsync(SessionId, delta);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse wheel error: {ex.Message}");
            }
        }

        public async Task SendKeyDownAsync(int virtualKey)
        {
            if (!InputEnabled || !IsConnected) 
                return;
            
            try
            {
                await _transportService.SendKeyDownAsync(SessionId, virtualKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Key down error: {ex.Message}");
            }
        }

        public async Task SendKeyUpAsync(int virtualKey)
        {
            if (!InputEnabled || !IsConnected) 
                return;
            
            try
            {
                await _transportService.SendKeyUpAsync(SessionId, virtualKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Key up error: {ex.Message}");
            }
        }

        private async void OnFrameDataReceived(object? sender, byte[] frameData)
        {
            try
            {
                // Render frame on UI thread
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        FrameBuffer = await _videoRenderer.RenderFrameAsync(frameData);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Frame render error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Frame processing error: {ex.Message}");
            }
        }

        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            IsConnected = e.NewState == Core.Enums.ConnectionState.Connected ||
                         e.NewState == Core.Enums.ConnectionState.Streaming;

            if (e.NewState == Core.Enums.ConnectionState.Disconnected ||
                e.NewState == Core.Enums.ConnectionState.Error)
            {
                // Navigate back to connect view
                _messenger.Send(new NavigationMessage("Connect"));
            }

            UpdateConnectionInfo();
        }

        private void OnMetricsUpdated(object? sender, MetricsEventArgs e)
        {
            // Update metrics on UI thread
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CurrentFps = e.CurrentFps;
                RoundTripTime = e.RoundTripTime;
                BitrateMbps = e.BitrateMbps;
                QueueLength = e.QueueLength;
                EncodeTimeMs = e.EncodeTimeMs;
                DecodeTimeMs = e.DecodeTimeMs;
            });
        }

        private void UpdateMetricsDisplay()
        {
            MetricsText = $"FPS: {CurrentFps} | RTT: {RoundTripTime}ms | " +
                         $"Bitrate: {BitrateMbps:F1}Mbps | Queue: {QueueLength} | " +
                         $"Encode: {EncodeTimeMs:F1}ms | Decode: {DecodeTimeMs:F1}ms";
        }

        private void UpdateConnectionInfo()
        {
            var status = IsConnected ? "Connected" : "Disconnected";
            var quality = $"Quality: {Quality}% | FPS: {FrameRate}";
            var input = InputEnabled ? "Input: Enabled" : "Input: Disabled";
            
            ConnectionInfo = $"{status} | {quality} | {input}";
        }

        partial void OnQualityChanged(int value)
        {
            UpdateConnectionInfo();
        }

        partial void OnFrameRateChanged(int value)
        {
            UpdateConnectionInfo();
        }

        partial void OnInputEnabledChanged(bool value)
        {
            UpdateConnectionInfo();
        }

        partial void OnIsConnectedChanged(bool value)
        {
            UpdateConnectionInfo();
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }

        public void Dispose()
        {
            _metricsTimer?.Stop();
            _transportService.ConnectionStateChanged -= OnConnectionStateChanged;
            _transportService.FrameDataReceived -= OnFrameDataReceived;
            _videoRenderer.MetricsUpdated -= OnMetricsUpdated;
        }
    }
}
