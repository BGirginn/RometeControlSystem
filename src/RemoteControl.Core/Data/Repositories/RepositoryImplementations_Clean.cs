using Microsoft.EntityFrameworkCore;
using RemoteControl.Core.Data.Entities;
using RemoteControl.Core.Data.Interfaces;

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
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SessionLogEntity>> GetRecentSessionsAsync(int count = 20, CancellationToken cancellationToken = default)
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
        var entry = await _context.SessionLogs.AddAsync(sessionLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task<SessionLogEntity> UpdateAsync(SessionLogEntity sessionLog, CancellationToken cancellationToken = default)
    {
        _context.SessionLogs.Update(sessionLog);
        await _context.SaveChangesAsync(cancellationToken);
        return sessionLog;
    }

    public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.SessionLogs.FindAsync(new object[] { sessionId }, cancellationToken);
        if (session != null)
        {
            _context.SessionLogs.Remove(session);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task EndSessionAsync(string sessionId, string endReason, CancellationToken cancellationToken = default)
    {
        var session = await _context.SessionLogs.FindAsync(new object[] { sessionId }, cancellationToken);
        if (session != null && session.EndTime == null)
        {
            session.EndTime = DateTime.UtcNow;
            session.Status = "Ended";
            session.EndReason = endReason;
            
            if (session.StartTime != default)
            {
                var duration = session.EndTime.Value - session.StartTime;
                session.Duration = TimeOnly.FromTimeSpan(duration);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<long> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs.CountAsync(cancellationToken);
    }

    public async Task<long> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs.CountAsync(s => s.Status == "Active" && s.EndTime == null, cancellationToken);
    }

    public async Task<IEnumerable<SessionLogEntity>> GetSessionsInDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.SessionLogs
            .Include(s => s.Viewer)
            .Include(s => s.Agent)
            .Include(s => s.Device)
            .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
            .OrderByDescending(s => s.StartTime)
            .Skip(skip)
            .Take(take)
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
            .Include(cp => cp.Granter)
            .Include(cp => cp.Grantee)
            .Include(cp => cp.Device)
            .FirstOrDefaultAsync(cp => cp.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ConnectionPermissionEntity>> GetByGranterIdAsync(string granterId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(cp => cp.Granter)
            .Include(cp => cp.Grantee)
            .Include(cp => cp.Device)
            .Where(cp => cp.GranterId == granterId)
            .OrderByDescending(cp => cp.GrantedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConnectionPermissionEntity>> GetByGranteeIdAsync(string granteeId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(cp => cp.Granter)
            .Include(cp => cp.Grantee)
            .Include(cp => cp.Device)
            .Where(cp => cp.GranteeId == granteeId)
            .OrderByDescending(cp => cp.GrantedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConnectionPermissionEntity>> GetByDeviceIdAsync(string deviceId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(cp => cp.Granter)
            .Include(cp => cp.Grantee)
            .Include(cp => cp.Device)
            .Where(cp => cp.DeviceId == deviceId)
            .OrderByDescending(cp => cp.GrantedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConnectionPermissionEntity?> GetPermissionAsync(string granterId, string granteeId, string deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(cp => cp.Granter)
            .Include(cp => cp.Grantee)
            .Include(cp => cp.Device)
            .FirstOrDefaultAsync(cp => cp.GranterId == granterId && cp.GranteeId == granteeId && cp.DeviceId == deviceId && cp.IsActive, cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(string granteeId, string deviceId, string permissionType, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .AnyAsync(cp => cp.GranteeId == granteeId && 
                           cp.DeviceId == deviceId && 
                           cp.PermissionType == permissionType && 
                           cp.IsActive &&
                           (cp.ExpiresAt == null || cp.ExpiresAt > DateTime.UtcNow), cancellationToken);
    }

    public async Task<ConnectionPermissionEntity> CreateAsync(ConnectionPermissionEntity permission, CancellationToken cancellationToken = default)
    {
        var entry = await _context.ConnectionPermissions.AddAsync(permission, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task<ConnectionPermissionEntity> UpdateAsync(ConnectionPermissionEntity permission, CancellationToken cancellationToken = default)
    {
        _context.ConnectionPermissions.Update(permission);
        await _context.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var permission = await _context.ConnectionPermissions.FindAsync(new object[] { id }, cancellationToken);
        if (permission != null)
        {
            _context.ConnectionPermissions.Remove(permission);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokePermissionAsync(string granterId, string granteeId, string deviceId, string reason, CancellationToken cancellationToken = default)
    {
        var permission = await _context.ConnectionPermissions
            .FirstOrDefaultAsync(cp => cp.GranterId == granterId && cp.GranteeId == granteeId && cp.DeviceId == deviceId && cp.IsActive, cancellationToken);
        
        if (permission != null)
        {
            permission.IsActive = false;
            permission.RevokedAt = DateTime.UtcNow;
            permission.Reason = reason;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<long> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions.CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<ConnectionPermissionEntity>> GetActivePermissionsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionPermissions
            .Include(cp => cp.Granter)
            .Include(cp => cp.Grantee)
            .Include(cp => cp.Device)
            .Where(cp => cp.IsActive && (cp.ExpiresAt == null || cp.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(cp => cp.GrantedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Temporary stub implementation for AuditLogRepository
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    public Task<AuditLogEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => Task.FromResult<AuditLogEntity?>(null);

    public Task<IEnumerable<AuditLogEntity>> GetByUserIdAsync(string userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<AuditLogEntity>());

    public Task<IEnumerable<AuditLogEntity>> GetByEntityAsync(string entityType, string? entityId = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<AuditLogEntity>());

    public Task<IEnumerable<AuditLogEntity>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<AuditLogEntity>());

    public Task<IEnumerable<AuditLogEntity>> GetSecurityEventsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<AuditLogEntity>());

    public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0L);

    public Task<AuditLogEntity> AddAsync(AuditLogEntity auditLog, CancellationToken cancellationToken = default)
        => Task.FromResult(auditLog);

    public Task<IEnumerable<AuditLogEntity>> AddRangeAsync(IEnumerable<AuditLogEntity> auditLogs, CancellationToken cancellationToken = default)
        => Task.FromResult(auditLogs);

    public Task DeleteOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
