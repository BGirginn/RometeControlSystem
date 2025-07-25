using RemoteControl.Core.Data.Entities;
using RemoteControl.Core.Models;

namespace RemoteControl.Core.Data.Repositories;

/// <summary>
/// Repository interface for user management
/// </summary>
public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserEntity> CreateAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task<UserEntity> UpdateAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsUsernameAvailableAsync(string username, string? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserEntity>> SearchAsync(string searchTerm, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for device management
/// </summary>
public interface IDeviceRepository
{
    Task<DeviceEntity?> GetByIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeviceEntity>> GetByOwnerIdAsync(string ownerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeviceEntity>> GetOnlineDevicesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DeviceEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DeviceEntity> CreateAsync(DeviceEntity device, CancellationToken cancellationToken = default);
    Task<DeviceEntity> UpdateAsync(DeviceEntity device, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<bool> UpdateLastSeenAsync(string deviceId, DateTime lastSeen, bool isOnline, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetOnlineCountAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DeviceEntity>> SearchAsync(string searchTerm, string? ownerId = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for session logging
/// </summary>
public interface ISessionLogRepository
{
    Task<SessionLogEntity?> GetByIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionLogEntity>> GetByUserIdAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionLogEntity>> GetByDeviceIdAsync(string deviceId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionLogEntity>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionLogEntity>> GetRecentSessionsAsync(int count = 100, CancellationToken cancellationToken = default);
    Task<SessionLogEntity> CreateAsync(SessionLogEntity sessionLog, CancellationToken cancellationToken = default);
    Task<SessionLogEntity> UpdateAsync(SessionLogEntity sessionLog, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> EndSessionAsync(string sessionId, DateTime endTime, string? endReason = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SessionLogEntity>> GetSessionsInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for connection permissions
/// </summary>
public interface IConnectionPermissionRepository
{
    Task<ConnectionPermissionEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConnectionPermissionEntity>> GetByGranterIdAsync(string granterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConnectionPermissionEntity>> GetByGranteeIdAsync(string granteeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConnectionPermissionEntity>> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<ConnectionPermissionEntity?> GetPermissionAsync(string granterId, string granteeId, string deviceId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string granteeId, string deviceId, string permissionType = "View", CancellationToken cancellationToken = default);
    Task<ConnectionPermissionEntity> CreateAsync(ConnectionPermissionEntity permission, CancellationToken cancellationToken = default);
    Task<ConnectionPermissionEntity> UpdateAsync(ConnectionPermissionEntity permission, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> RevokePermissionAsync(string granterId, string granteeId, string deviceId, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ConnectionPermissionEntity>> GetActivePermissionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for audit logging
/// </summary>
public interface IAuditLogRepository
{
    Task<AuditLogEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLogEntity>> GetByUserIdAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLogEntity>> GetByActionAsync(string action, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLogEntity>> GetSecurityEventsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLogEntity>> GetRecentLogsAsync(int count = 100, CancellationToken cancellationToken = default);
    Task<AuditLogEntity> CreateAsync(AuditLogEntity auditLog, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLogEntity>> GetLogsInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLogEntity>> SearchAsync(string searchTerm, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
}
