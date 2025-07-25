using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    public class RegistryClientService : IRegistryClientService, IDisposable
    {
        private readonly ILogger<RegistryClientService> _logger;
        private readonly HttpClient _httpClient;
        private string? _registryServerUrl;
        private string? _currentUserId;
        private string? _authToken;

        public bool IsConnected => !string.IsNullOrEmpty(_registryServerUrl) && _httpClient != null;

#pragma warning disable CS0067 // Event is never used
        public event EventHandler<Device>? DeviceOnline;
        public event EventHandler<Device>? DeviceOffline;
#pragma warning restore CS0067
        public event EventHandler<string>? ConnectionStatusChanged;

        public RegistryClientService(ILogger<RegistryClientService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> ConnectAsync(string registryServerUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                _registryServerUrl = registryServerUrl.TrimEnd('/');
                
                // Test connection to registry server
                var response = await _httpClient.GetAsync($"{_registryServerUrl}/api/health", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Connected to registry server: {ServerUrl}", _registryServerUrl);
                    ConnectionStatusChanged?.Invoke(this, "Connected");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to connect to registry server: {StatusCode}", response.StatusCode);
                    ConnectionStatusChanged?.Invoke(this, "Connection failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to registry server: {ServerUrl}", registryServerUrl);
                ConnectionStatusChanged?.Invoke(this, $"Connection error: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(_currentUserId) && !string.IsNullOrEmpty(_authToken))
            {
                await LogoutAsync(cancellationToken);
            }

            _registryServerUrl = null;
            _currentUserId = null;
            _authToken = null;
            
            ConnectionStatusChanged?.Invoke(this, "Disconnected");
            _logger.LogInformation("Disconnected from registry server");
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to registry server");

            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_registryServerUrl}/api/auth/register", content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (authResponse?.Success == true)
                {
                    _currentUserId = authResponse.UserId;
                    _authToken = authResponse.Token;
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
                    _logger.LogInformation("User registered successfully: {Username}", request.Username);
                }
                
                return authResponse ?? new AuthResponse { Success = false, Message = "Invalid response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Username}", request.Username);
                return new AuthResponse { Success = false, Message = $"Registration error: {ex.Message}" };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to registry server");

            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_registryServerUrl}/api/auth/login", content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (authResponse?.Success == true)
                {
                    _currentUserId = authResponse.UserId;
                    _authToken = authResponse.Token;
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
                    _logger.LogInformation("User logged in successfully: {Username}", request.Username);
                }
                
                return authResponse ?? new AuthResponse { Success = false, Message = "Invalid response from server" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user: {Username}", request.Username);
                return new AuthResponse { Success = false, Message = $"Login error: {ex.Message}" };
            }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected || string.IsNullOrEmpty(_authToken))
                return;

            try
            {
                await _httpClient.PostAsync($"{_registryServerUrl}/api/auth/logout", null, cancellationToken);
                _logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }
            finally
            {
                _currentUserId = null;
                _authToken = null;
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<string> RegisterDeviceAsync(DeviceRegistrationRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || string.IsNullOrEmpty(_authToken))
                throw new InvalidOperationException("Not authenticated");

            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_registryServerUrl}/api/devices/register", content, cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
                    var deviceId = result?["deviceId"]?.ToString() ?? string.Empty;
                    _logger.LogInformation("Device registered with ID: {DeviceId}", deviceId);
                    return deviceId;
                }
                else
                {
                    _logger.LogError("Failed to register device: {StatusCode}", response.StatusCode);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device");
                return string.Empty;
            }
        }

        public async Task UpdateDeviceStatusAsync(string deviceId, bool isOnline, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || string.IsNullOrEmpty(_authToken))
                return;

            try
            {
                var statusData = new { DeviceId = deviceId, IsOnline = isOnline, LastSeen = DateTime.UtcNow };
                var json = JsonSerializer.Serialize(statusData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                await _httpClient.PutAsync($"{_registryServerUrl}/api/devices/{deviceId}/status", content, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device status: {DeviceId}", deviceId);
            }
        }

        public async Task<DeviceListResponse> GetAvailableDevicesAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected || string.IsNullOrEmpty(_authToken))
                return new DeviceListResponse { Success = false, Message = "Not authenticated" };

            try
            {
                var response = await _httpClient.GetAsync($"{_registryServerUrl}/api/devices/available", cancellationToken);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var deviceList = JsonSerializer.Deserialize<DeviceListResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return deviceList ?? new DeviceListResponse { Success = false, Message = "Invalid response" };
                }
                else
                {
                    return new DeviceListResponse { Success = false, Message = $"Server error: {response.StatusCode}" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available devices");
                return new DeviceListResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Device?> ResolveDeviceAsync(string deviceIdOrName, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || string.IsNullOrEmpty(_authToken))
                return null;

            try
            {
                var response = await _httpClient.GetAsync($"{_registryServerUrl}/api/devices/resolve/{Uri.EscapeDataString(deviceIdOrName)}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var device = JsonSerializer.Deserialize<Device>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return device;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving device: {DeviceIdOrName}", deviceIdOrName);
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
