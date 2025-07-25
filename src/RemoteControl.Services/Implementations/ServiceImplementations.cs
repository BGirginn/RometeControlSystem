using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Events;
using RemoteControl.Core.Models;
using RemoteControl.Protocol.Messages;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    /// <summary>
    /// Mock implementation of ITransportService for testing UI without actual network
    /// </summary>
    public class MockTransportService : ITransportService
    {
        private readonly System.Timers.Timer _frameTimer;
        private readonly Random _random = new();
        private bool _isConnected = false;

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        public event EventHandler<byte[]>? FrameDataReceived;

        public MockTransportService()
        {
            // Simulate frame data with a timer (30 FPS)
            _frameTimer = new System.Timers.Timer(33); // ~30 FPS
            _frameTimer.Elapsed += (s, e) => GenerateMockFrame(null);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await DisconnectAsync(cancellationToken);
        }

        public async Task ConnectAsync(ConnectionRequest request, CancellationToken cancellationToken = default)
        {
            // Simulate connection process
            OnConnectionStateChanged(Core.Enums.ConnectionState.Disconnected, Core.Enums.ConnectionState.Resolving, "Resolving target ID...");
            await Task.Delay(500, cancellationToken);

            OnConnectionStateChanged(Core.Enums.ConnectionState.Resolving, Core.Enums.ConnectionState.Connecting, "Establishing connection...");
            await Task.Delay(1000, cancellationToken);

            OnConnectionStateChanged(Core.Enums.ConnectionState.Connecting, Core.Enums.ConnectionState.Authenticating, "Authenticating...");
            await Task.Delay(800, cancellationToken);

            OnConnectionStateChanged(Core.Enums.ConnectionState.Authenticating, Core.Enums.ConnectionState.Connected, "Connected successfully");
            await Task.Delay(200, cancellationToken);

            OnConnectionStateChanged(Core.Enums.ConnectionState.Connected, Core.Enums.ConnectionState.Streaming, "Streaming started");
            
            _isConnected = true;
            
            // Start sending mock frames
            _frameTimer.Start();
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (_isConnected)
            {
                OnConnectionStateChanged(Core.Enums.ConnectionState.Streaming, Core.Enums.ConnectionState.Disconnecting, "Disconnecting...");
                await Task.Delay(300, cancellationToken);
                
                _frameTimer.Stop();
                _isConnected = false;
                
                OnConnectionStateChanged(Core.Enums.ConnectionState.Disconnecting, Core.Enums.ConnectionState.Disconnected, "Disconnected");
            }
        }

        public Task<bool> IsConnectedAsync()
        {
            return Task.FromResult(_isConnected);
        }

        private void GenerateMockFrame(object? state)
        {
            if (!_isConnected) return;

            try
            {
                // Generate mock frame data (simple colored bitmap)
                var width = 800;
                var height = 600;
                var bytesPerPixel = 4; // RGBA
                var frameData = new byte[width * height * bytesPerPixel];

                // Fill with a simple pattern that changes over time
                var time = DateTime.Now.Millisecond;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var index = (y * width + x) * bytesPerPixel;
                        
                        // Create a moving gradient pattern
                        var r = (byte)((x + time) % 256);
                        var g = (byte)((y + time) % 256);
                        var b = (byte)((x + y + time) % 256);
                        
                        frameData[index] = b;     // Blue
                        frameData[index + 1] = g; // Green
                        frameData[index + 2] = r; // Red
                        frameData[index + 3] = 255; // Alpha
                    }
                }

                FrameDataReceived?.Invoke(this, frameData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mock frame generation error: {ex.Message}");
            }
        }

        public async Task SendInputEventAsync(InputEventMessage inputEvent, CancellationToken cancellationToken = default)
        {
            // Mock implementation - just log the input event
            await Task.CompletedTask;
        }

        public async Task SendMouseMoveAsync(string sessionId, double relativeX, double relativeY, CancellationToken cancellationToken = default)
        {
            // Mock implementation - just log the mouse move
            await Task.CompletedTask;
        }

        public async Task SendMouseDownAsync(string sessionId, string button, CancellationToken cancellationToken = default)
        {
            // Mock implementation - just log the mouse down
            await Task.CompletedTask;
        }

        public async Task SendMouseUpAsync(string sessionId, string button, CancellationToken cancellationToken = default)
        {
            // Mock implementation - just log the mouse up
            await Task.CompletedTask;
        }

        public async Task SendMouseWheelAsync(string sessionId, int delta, CancellationToken cancellationToken = default)
        {
            // Mock implementation - just log the mouse wheel
            await Task.CompletedTask;
        }

        public async Task SendKeyDownAsync(string sessionId, int virtualKey, CancellationToken cancellationToken = default)
        {
            // Mock implementation - just log the key down
            await Task.CompletedTask;
        }

        public async Task SendKeyUpAsync(string sessionId, int virtualKey, CancellationToken cancellationToken = default)
        {
            // Mock implementation - just log the key up
            await Task.CompletedTask;
        }

        private void OnConnectionStateChanged(Core.Enums.ConnectionState oldState, Core.Enums.ConnectionState newState, string? message = null)
        {
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(oldState, newState, message));
        }

        public void Dispose()
        {
            _frameTimer?.Dispose();
        }
    }

    /// <summary>
    /// File-based implementation of IUserSettingsService
    /// </summary>
    public class FileUserSettingsService : IUserSettingsService
    {
        private readonly string _settingsPath;
        private AppSettings? _settings;

        public FileUserSettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "RemoteControlViewer");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
            
            LoadSettings();
        }

        public Task<string> GetUserTokenAsync()
        {
            return Task.FromResult(_settings?.UserToken ?? string.Empty);
        }

        public async Task SaveUserTokenAsync(string token)
        {
            _settings ??= new AppSettings();
            _settings.UserToken = token;
            await SaveSettingsAsync();
        }

        public async Task AddRecentConnectionAsync(string targetId)
        {
            _settings ??= new AppSettings();
            
            var existing = _settings.RecentConnections.FirstOrDefault(c => c.TargetId == targetId);
            if (existing != null)
            {
                existing.LastConnected = DateTime.UtcNow;
                _settings.RecentConnections.Remove(existing);
                _settings.RecentConnections.Insert(0, existing);
            }
            else
            {
                var newConnection = new RecentConnection
                {
                    TargetId = targetId,
                    DisplayName = targetId,
                    LastConnected = DateTime.UtcNow,
                    Duration = TimeSpan.Zero
                };
                _settings.RecentConnections.Insert(0, newConnection);
            }

            // Keep only last 10 connections
            while (_settings.RecentConnections.Count > 10)
            {
                _settings.RecentConnections.RemoveAt(_settings.RecentConnections.Count - 1);
            }

            await SaveSettingsAsync();
        }

        public Task<IEnumerable<RecentConnection>> GetRecentConnectionsAsync()
        {
            var connections = _settings?.RecentConnections ?? new List<RecentConnection>();
            return Task.FromResult<IEnumerable<RecentConnection>>(connections);
        }

        public async Task RemoveRecentConnectionAsync(string targetId)
        {
            if (_settings?.RecentConnections == null) return;

            var existing = _settings.RecentConnections.FirstOrDefault(rc => rc.TargetId == targetId);
            if (existing != null)
            {
                _settings.RecentConnections.Remove(existing);
                await SaveSettingsAsync();
            }
        }

        public Task<string> GetThemeAsync()
        {
            return Task.FromResult(_settings?.Theme ?? "Light");
        }

        public async Task SetThemeAsync(string theme)
        {
            _settings ??= new AppSettings();
            _settings.Theme = theme;
            await SaveSettingsAsync();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            _settings ??= new AppSettings();
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private class AppSettings
        {
            public string UserToken { get; set; } = string.Empty;
            public string Theme { get; set; } = "Light";
            public List<RecentConnection> RecentConnections { get; set; } = new();
        }
    }

    /// <summary>
    /// WPF implementation of IVideoRenderer
    /// </summary>
    public class WpfVideoRenderer : IVideoRenderer
    {
        private int _quality = 80;
        private int _frameRate = 30;
        private readonly System.Threading.Timer _metricsTimer;
        private int _frameCount = 0;
        private DateTime _lastMetricsUpdate = DateTime.UtcNow;

        public event EventHandler<MetricsEventArgs>? MetricsUpdated;

        public WpfVideoRenderer()
        {
            _metricsTimer = new System.Threading.Timer(UpdateMetrics, null, 1000, 1000);
        }

        public Task<System.Windows.Media.Imaging.WriteableBitmap> RenderFrameAsync(byte[] frameData, CancellationToken cancellationToken = default)
        {
            try
            {
                // Assume frameData is RGBA format
                var width = 800; // These should come from frame header
                var height = 600;

                var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
                    width, height, 96, 96,
                    System.Windows.Media.PixelFormats.Bgra32, null);

                bitmap.WritePixels(
                    new System.Windows.Int32Rect(0, 0, width, height),
                    frameData, width * 4, 0);

                _frameCount++;
                return Task.FromResult(bitmap);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Frame render error: {ex.Message}");
                throw;
            }
        }

        public Task SetQualityAsync(int quality)
        {
            _quality = Math.Clamp(quality, 10, 100);
            return Task.CompletedTask;
        }

        public Task SetFrameRateAsync(int fps)
        {
            _frameRate = Math.Clamp(fps, 5, 60);
            return Task.CompletedTask;
        }

        private void UpdateMetrics(object? state)
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastMetricsUpdate;
            
            if (elapsed.TotalSeconds > 0)
            {
                var currentFps = (int)(_frameCount / elapsed.TotalSeconds);
                
                var metrics = new MetricsEventArgs
                {
                    CurrentFps = currentFps,
                    RoundTripTime = Random.Shared.Next(10, 100),
                    BitrateMbps = Random.Shared.NextDouble() * 10 + 5,
                    QueueLength = Random.Shared.Next(0, 10),
                    EncodeTimeMs = Random.Shared.NextDouble() * 5 + 1,
                    DecodeTimeMs = Random.Shared.NextDouble() * 3 + 0.5,
                    Timestamp = now
                };

                MetricsUpdated?.Invoke(this, metrics);
                
                _frameCount = 0;
                _lastMetricsUpdate = now;
            }
        }

        public void Dispose()
        {
            _metricsTimer?.Dispose();
        }
    }

    /// <summary>
    /// WPF implementation of IThemeService
    /// </summary>
    public class WpfThemeService : IThemeService
    {
        private string _currentTheme = "Light";
        
        public event EventHandler<string>? ThemeChanged;

        public Task<string> GetThemeAsync()
        {
            return Task.FromResult(_currentTheme);
        }

        public Task SetThemeAsync(string theme)
        {
            if (_currentTheme != theme)
            {
                _currentTheme = theme;
                ApplyTheme(theme);
                ThemeChanged?.Invoke(this, theme);
            }
            return Task.CompletedTask;
        }

        private void ApplyTheme(string theme)
        {
            try
            {
                var app = System.Windows.Application.Current;
                if (app?.Resources != null)
                {
                    // Clear existing theme resources
                    var existingThemes = app.Resources.MergedDictionaries
                        .Where(d => d.Source?.OriginalString?.Contains("Theme.") == true)
                        .ToList();

                    foreach (var themeDict in existingThemes)
                    {
                        app.Resources.MergedDictionaries.Remove(themeDict);
                    }

                    // Add new theme
                    var themeUri = theme.ToLower() switch
                    {
                        "dark" => new Uri("Themes/Theme.Dark.xaml", UriKind.Relative),
                        "light" => new Uri("Themes/Theme.xaml", UriKind.Relative),
                        _ => new Uri("Themes/Theme.xaml", UriKind.Relative)
                    };

                    var newTheme = new System.Windows.ResourceDictionary { Source = themeUri };
                    app.Resources.MergedDictionaries.Add(newTheme);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to apply theme {theme}: {ex.Message}");
            }
        }
    }
}
