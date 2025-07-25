using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RemoteControl.Core.Models;

namespace RemoteControl.Services.Interfaces
{
    /// <summary>
    /// Central registry service for user management and device discovery
    /// </summary>
    public interface IRegistryService
    {
        // User Management
        Task<AuthResponse> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default);
        Task<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default);

        // Device Management
        Task<string> RegisterDeviceAsync(DeviceRegistrationRequest request, CancellationToken cancellationToken = default);
        Task<bool> UpdateDeviceStatusAsync(string deviceId, string currentIP, int port, bool isOnline, CancellationToken cancellationToken = default);
        Task<Device?> GetDeviceAsync(string deviceId, CancellationToken cancellationToken = default);
        Task<DeviceListResponse> GetUserDevicesAsync(string userId, CancellationToken cancellationToken = default);
        Task<DeviceListResponse> GetAuthorizedDevicesAsync(string userId, CancellationToken cancellationToken = default);

        // Permission Management
        Task<bool> GrantPermissionAsync(string deviceId, string userId, PermissionLevel level, DateTime? expiresAt = null, CancellationToken cancellationToken = default);
        Task<bool> RevokePermissionAsync(string deviceId, string userId, CancellationToken cancellationToken = default);
        Task<bool> HasPermissionAsync(string deviceId, string userId, CancellationToken cancellationToken = default);

        // Device Discovery
        Task<DeviceListResponse> SearchDevicesAsync(string searchTerm, string userId, CancellationToken cancellationToken = default);
        Task<Device?> ResolveDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service for connecting to the central registry server
    /// </summary>
    public interface IRegistryClientService
    {
        Task<bool> ConnectAsync(string registryServerUrl, CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        bool IsConnected { get; }

        // User operations
        Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
        Task LogoutAsync(CancellationToken cancellationToken = default);

        // Device operations
        Task<string> RegisterDeviceAsync(DeviceRegistrationRequest request, CancellationToken cancellationToken = default);
        Task UpdateDeviceStatusAsync(string deviceId, bool isOnline, CancellationToken cancellationToken = default);
        Task<DeviceListResponse> GetAvailableDevicesAsync(CancellationToken cancellationToken = default);
        Task<Device?> ResolveDeviceAsync(string deviceIdOrName, CancellationToken cancellationToken = default);

        // Events
        event EventHandler<Device>? DeviceOnline;
        event EventHandler<Device>? DeviceOffline;
        event EventHandler<string>? ConnectionStatusChanged;
    }
}
