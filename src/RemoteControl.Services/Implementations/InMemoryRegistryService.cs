using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    /// <summary>
    /// In-memory registry service for demo purposes
    /// In production, this would use a database and run on a separate server
    /// </summary>
    public class InMemoryRegistryService : IRegistryService
    {
        private readonly ILogger<InMemoryRegistryService> _logger;
        private readonly ConcurrentDictionary<string, User> _users = new();
        private readonly ConcurrentDictionary<string, Device> _devices = new();
        private readonly ConcurrentDictionary<string, ConnectionPermission> _permissions = new();
        private int _deviceIdCounter = 1000;

        public InMemoryRegistryService(ILogger<InMemoryRegistryService> logger)
        {
            _logger = logger;
            SeedData();
        }

        private void SeedData()
        {
            // Create demo users and devices for testing
            var demoUser = new User
            {
                UserId = "user-001",
                Username = "demo",
                Email = "demo@example.com",
                PasswordHash = HashPassword("demo123"), // Simple demo - use proper hashing in production
                IsActive = true
            };
            _users.TryAdd(demoUser.UserId, demoUser);

            var demoDevice = new Device
            {
                DeviceId = "RC-001000",
                DeviceName = "Demo Computer",
                OwnerId = demoUser.UserId,
                CurrentIP = "127.0.0.1",
                Port = 7777,
                IsOnline = true,
                ComputerName = "DEMO-PC",
                UserName = "DemoUser",
                OperatingSystem = "Windows 11"
            };
            _devices.TryAdd(demoDevice.DeviceId, demoDevice);
            demoUser.OwnedDevices.Add(demoDevice.DeviceId);

            _logger.LogInformation("Demo registry initialized with user '{Username}' and device '{DeviceId}'", 
                demoUser.Username, demoDevice.DeviceId);
        }

        public Task<AuthResponse> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if username already exists
                if (_users.Values.Any(u => u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)))
                {
                    return Task.FromResult(new AuthResponse 
                    { 
                        Success = false, 
                        Message = "Username already exists" 
                    });
                }

                var userId = Guid.NewGuid().ToString();
                var user = new User
                {
                    UserId = userId,
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    IsActive = true
                };

                _users.TryAdd(userId, user);

                _logger.LogInformation("User registered: {Username} ({UserId})", request.Username, userId);

                return Task.FromResult(new AuthResponse
                {
                    Success = true,
                    UserId = userId,
                    Token = GenerateToken(userId),
                    User = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Username}", request.Username);
                return Task.FromResult(new AuthResponse 
                { 
                    Success = false, 
                    Message = $"Registration failed: {ex.Message}" 
                });
            }
        }

        public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = _users.Values.FirstOrDefault(u => 
                    u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) &&
                    VerifyPassword(request.Password, u.PasswordHash) &&
                    u.IsActive);

                if (user == null)
                {
                    return Task.FromResult(new AuthResponse 
                    { 
                        Success = false, 
                        Message = "Invalid username or password" 
                    });
                }

                user.LastLogin = DateTime.UtcNow;

                _logger.LogInformation("User logged in: {Username} ({UserId})", user.Username, user.UserId);

                return Task.FromResult(new AuthResponse
                {
                    Success = true,
                    UserId = user.UserId,
                    Token = GenerateToken(user.UserId),
                    User = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login: {Username}", request.Username);
                return Task.FromResult(new AuthResponse 
                { 
                    Success = false, 
                    Message = $"Login failed: {ex.Message}" 
                });
            }
        }

        public Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("User logged out: {UserId}", userId);
            return Task.FromResult(true);
        }

        public Task<User?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task<string> RegisterDeviceAsync(DeviceRegistrationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var deviceId = $"RC-{Interlocked.Increment(ref _deviceIdCounter):D6}";
                var device = new Device
                {
                    DeviceId = deviceId,
                    DeviceName = request.DeviceName,
                    OwnerId = request.UserId,
                    ComputerName = request.ComputerName,
                    UserName = request.UserName,
                    OperatingSystem = request.OperatingSystem,
                    IsOnline = false // Will be set online when agent starts
                };

                _devices.TryAdd(deviceId, device);

                // Add to user's owned devices
                if (_users.TryGetValue(request.UserId, out var user))
                {
                    user.OwnedDevices.Add(deviceId);
                }

                _logger.LogInformation("Device registered: {DeviceId} for user {UserId}", deviceId, request.UserId);

                return Task.FromResult(deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device for user: {UserId}", request.UserId);
                return Task.FromResult(string.Empty);
            }
        }

        public Task<bool> UpdateDeviceStatusAsync(string deviceId, string currentIP, int port, bool isOnline, CancellationToken cancellationToken = default)
        {
            if (_devices.TryGetValue(deviceId, out var device))
            {
                device.CurrentIP = currentIP;
                device.Port = port;
                device.IsOnline = isOnline;
                device.LastSeen = DateTime.UtcNow;

                _logger.LogDebug("Device status updated: {DeviceId} - {Status}", deviceId, isOnline ? "Online" : "Offline");
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<Device?> GetDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            _devices.TryGetValue(deviceId, out var device);
            return Task.FromResult(device);
        }

        public Task<DeviceListResponse> GetUserDevicesAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var userDevices = _devices.Values.Where(d => d.OwnerId == userId).ToList();
                return Task.FromResult(new DeviceListResponse
                {
                    Success = true,
                    Devices = userDevices
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user devices: {UserId}", userId);
                return Task.FromResult(new DeviceListResponse 
                { 
                    Success = false, 
                    Message = $"Error: {ex.Message}" 
                });
            }
        }

        public Task<DeviceListResponse> GetAuthorizedDevicesAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var authorizedDevices = _devices.Values
                    .Where(d => d.AuthorizedUsers.Contains(userId) || d.OwnerId == userId)
                    .ToList();

                return Task.FromResult(new DeviceListResponse
                {
                    Success = true,
                    Devices = authorizedDevices
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authorized devices: {UserId}", userId);
                return Task.FromResult(new DeviceListResponse 
                { 
                    Success = false, 
                    Message = $"Error: {ex.Message}" 
                });
            }
        }

        public Task<bool> GrantPermissionAsync(string deviceId, string userId, PermissionLevel level, DateTime? expiresAt = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var permission = new ConnectionPermission
                {
                    PermissionId = Guid.NewGuid().ToString(),
                    DeviceId = deviceId,
                    UserId = userId,
                    Level = level,
                    ExpiresAt = expiresAt,
                    IsActive = true
                };

                _permissions.TryAdd(permission.PermissionId, permission);

                // Add user to device's authorized users
                if (_devices.TryGetValue(deviceId, out var device))
                {
                    if (!device.AuthorizedUsers.Contains(userId))
                    {
                        device.AuthorizedUsers.Add(userId);
                    }
                }

                _logger.LogInformation("Permission granted: User {UserId} can access device {DeviceId} with level {Level}", 
                    userId, deviceId, level);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error granting permission: {DeviceId} to {UserId}", deviceId, userId);
                return Task.FromResult(false);
            }
        }

        public Task<bool> RevokePermissionAsync(string deviceId, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var permission = _permissions.Values.FirstOrDefault(p => p.DeviceId == deviceId && p.UserId == userId);
                if (permission != null)
                {
                    permission.IsActive = false;
                }

                // Remove from device's authorized users
                if (_devices.TryGetValue(deviceId, out var device))
                {
                    device.AuthorizedUsers.Remove(userId);
                }

                _logger.LogInformation("Permission revoked: User {UserId} can no longer access device {DeviceId}", userId, deviceId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking permission: {DeviceId} from {UserId}", deviceId, userId);
                return Task.FromResult(false);
            }
        }

        public Task<bool> HasPermissionAsync(string deviceId, string userId, CancellationToken cancellationToken = default)
        {
            var hasPermission = _devices.TryGetValue(deviceId, out var device) &&
                               (device.OwnerId == userId || device.AuthorizedUsers.Contains(userId));

            return Task.FromResult(hasPermission);
        }

        public Task<DeviceListResponse> SearchDevicesAsync(string searchTerm, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var matchingDevices = _devices.Values
                    .Where(d => d.DeviceName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               d.DeviceId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               d.ComputerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .Where(d => d.OwnerId == userId || d.AuthorizedUsers.Contains(userId))
                    .ToList();

                return Task.FromResult(new DeviceListResponse
                {
                    Success = true,
                    Devices = matchingDevices
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching devices: {SearchTerm} for user {UserId}", searchTerm, userId);
                return Task.FromResult(new DeviceListResponse 
                { 
                    Success = false, 
                    Message = $"Search error: {ex.Message}" 
                });
            }
        }

        public Task<Device?> ResolveDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            _devices.TryGetValue(deviceId, out var device);
            return Task.FromResult(device);
        }

        private static string HashPassword(string password)
        {
            // Simple demo hashing - use BCrypt or similar in production
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"salt_{password}"));
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        private static string GenerateToken(string userId)
        {
            // Simple demo token - use JWT or similar in production
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"token_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}"));
        }
    }
}
