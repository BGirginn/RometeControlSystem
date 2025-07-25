using System;
using System.Collections.Generic;

namespace RemoteControl.Core.Models
{
    public class User
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string> OwnedDevices { get; set; } = new();
        public List<string> AuthorizedDevices { get; set; } = new();
    }

    public class Device
    {
        public string DeviceId { get; set; } = string.Empty; // Systematic ID like "RC-001234"
        public string DeviceName { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public string CurrentIP { get; set; } = string.Empty;
        public int Port { get; set; } = 7777;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public bool IsOnline { get; set; }
        public string ComputerName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public List<string> AuthorizedUsers { get; set; } = new();
    }

    public class ConnectionPermission
    {
        public string PermissionId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public PermissionLevel Level { get; set; } = PermissionLevel.ViewOnly;
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public enum PermissionLevel
    {
        ViewOnly = 0,
        ViewAndControl = 1,
        FullAccess = 2
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
    }

    public class DeviceRegistrationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string ComputerName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? UserId { get; set; }
        public string? Token { get; set; }
        public User? User { get; set; }
    }

    public class DeviceListResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<Device> Devices { get; set; } = new();
    }
}
