using Microsoft.EntityFrameworkCore;
using RemoteControl.Core.Data.Entities;

namespace RemoteControl.Core.Data.Repositories;

/// <summary>
/// Repository implementation for session logging
/// </summary>
public class SessionLogRepository : ISessionLogRepository
{
    private readonly RemoteControlDbContext _context;

    public SessionLogRepository(RemoteControlDbContext context)
    {
        _context = context;
    }

    public async Task<SessionLogEntity?> GetByIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs
            .Include(s => s.Viewer)
            .Include(s => s.Agent)
            .Include(s => s.Device)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    public async Task<IEnumerable<SessionLogEntity>> GetByUserIdAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs
            .Include(s => s.Viewer)
            .Include(s => s.Agent)
            .Include(s => s.Device)
            .Where(s => s.ViewerId == userId || s.AgentId == userId)
            .OrderByDescending(s => s.StartTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionLogEntity>> GetByDeviceIdAsync(string deviceId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs
            .Include(s => s.Viewer)
            .Include(s => s.Agent)
            .Include(s => s.Device)
            .Where(s => s.DeviceId == deviceId)
            .OrderByDescending(s => s.StartTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionLogEntity>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs
            .Include(s => s.Viewer)
            .Include(s => s.Agent)
            .Include(s => s.Device)
            .Where(s => s.Status == "Active" && s.EndTime == null)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionLogEntity>> GetRecentSessionsAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs
            .Include(s => s.Viewer)
            .Include(s => s.Agent)
            .Include(s => s.Device)
            .OrderByDescending(s => s.StartTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<SessionLogEntity> CreateAsync(SessionLogEntity sessionLog, CancellationToken cancellationToken = default)
    {
        _context.SessionLogs.Add(sessionLog);
        await _context.SaveChangesAsync(cancellationToken);
        return sessionLog;
    }

    public async Task<SessionLogEntity> UpdateAsync(SessionLogEntity sessionLog, CancellationToken cancellationToken = default)
    {
        _context.SessionLogs.Update(sessionLog);
        await _context.SaveChangesAsync(cancellationToken);
        return sessionLog;
    }

    public async Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.SessionLogs.FindAsync(new object[] { sessionId }, cancellationToken);
        if (session == null) return false;

        _context.SessionLogs.Remove(session);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> EndSessionAsync(string sessionId, DateTime endTime, string? endReason = null, CancellationToken cancellationToken = default)
    {
        var session = await _context.SessionLogs.FindAsync(new object[] { sessionId }, cancellationToken);
        if (session == null) return false;

        session.EndTime = endTime;
        session.Status = "Ended";
        session.EndReason = endReason;
        session.Duration = endTime - session.StartTime;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs.CountAsync(cancellationToken);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs.CountAsync(s => s.Status == "Active" && s.EndTime == null, cancellationToken);
    }

    public async Task<IEnumerable<SessionLogEntity>> GetSessionsInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs
            .Include(s => s.Viewer)
            .Include(s => s.Agent)
            .Include(s => s.Device)
            .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for connection permissions
/// </summary>
public class ConnectionPermissionRepository : IConnectionPermissionRepository
{
    private readonly RemoteControlDbContext _context;

    public ConnectionPermissionRepository(RemoteControlDbContext context)
    {
        _context = context;
    }

    public async Task<ConnectionPermissionEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(p => p.Granter)
            .Include(p => p.Grantee)
            .Include(p => p.Device)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ConnectionPermissionEntity>> GetByGranterIdAsync(string granterId, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(p => p.Grantee)
            .Include(p => p.Device)
            .Where(p => p.GranterId == granterId)
            .OrderByDescending(p => p.GrantedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConnectionPermissionEntity>> GetByGranteeIdAsync(string granteeId, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(p => p.Granter)
            .Include(p => p.Device)
            .Where(p => p.GranteeId == granteeId)
            .OrderByDescending(p => p.GrantedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConnectionPermissionEntity>> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(p => p.Granter)
            .Include(p => p.Grantee)
            .Where(p => p.DeviceId == deviceId)
            .OrderByDescending(p => p.GrantedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConnectionPermissionEntity?> GetPermissionAsync(string granterId, string granteeId, string deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .FirstOrDefaultAsync(p => p.GranterId == granterId && 
                                     p.GranteeId == granteeId && 
                                     p.DeviceId == deviceId, 
                                cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(string granteeId, string deviceId, string permissionType = "View", CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .AnyAsync(p => p.GranteeId == granteeId && 
                          p.DeviceId == deviceId && 
                          p.PermissionType == permissionType &&
                          p.IsActive &&
                          (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow), 
                     cancellationToken);
    }

    public async Task<ConnectionPermissionEntity> CreateAsync(ConnectionPermissionEntity permission, CancellationToken cancellationToken = default)
    {
        _context.ConnectionPermissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task<ConnectionPermissionEntity> UpdateAsync(ConnectionPermissionEntity permission, CancellationToken cancellationToken = default)
    {
        _context.ConnectionPermissions.Update(permission);
        await _context.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var permission = await _context.ConnectionPermissions.FindAsync(new object[] { id }, cancellationToken);
        if (permission == null) return false;

        _context.ConnectionPermissions.Remove(permission);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RevokePermissionAsync(string granterId, string granteeId, string deviceId, CancellationToken cancellationToken = default)
    {
        var permission = await GetPermissionAsync(granterId, granteeId, deviceId, cancellationToken);
        if (permission == null) return false;

        permission.IsActive = false;
        permission.RevokedAt = DateTime.UtcNow;
        permission.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions.CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConnectionPermissionEntity>> GetActivePermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(p => p.Granter)
            .Include(p => p.Grantee)
            .Include(p => p.Device)
            .Where(p => p.IsActive && (p.ExpiresAt == null || p.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(p => p.GrantedAt)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for audit logging
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly RemoteControlDbContext _context;

    public AuditLogRepository(RemoteControlDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLogEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Device)
            .Include(a => a.Session)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AuditLogEntity>> GetByUserIdAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Device)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLogEntity>> GetByActionAsync(string action, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Device)
            .Where(a => a.Action == action)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLogEntity>> GetSecurityEventsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Device)
            .Where(a => a.IsSecurityEvent)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLogEntity>> GetRecentLogsAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Device)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditLogEntity> CreateAsync(AuditLogEntity auditLog, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLogEntity>> GetLogsInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Device)
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLogEntity>> SearchAsync(string searchTerm, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Device)
            .Where(a => a.Action.Contains(searchTerm) ||
                       (a.Summary != null && a.Summary.Contains(searchTerm)) ||
                       (a.Details != null && a.Details.Contains(searchTerm)))
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
