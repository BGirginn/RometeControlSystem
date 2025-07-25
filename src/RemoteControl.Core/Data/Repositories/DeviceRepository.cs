using Microsoft.EntityFrameworkCore;
using RemoteControl.Core.Data.Entities;

namespace RemoteControl.Core.Data.Repositories;

/// <summary>
/// Repository implementation for device management
/// </summary>
public class DeviceRepository : IDeviceRepository
{
    private readonly RemoteControlDbContext _context;

    public DeviceRepository(RemoteControlDbContext context)
    {
        _context = context;
    }

    public async Task<DeviceEntity?> GetByIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Include(d => d.Owner)
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId, cancellationToken);
    }

    public async Task<IEnumerable<DeviceEntity>> GetByOwnerIdAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Where(d => d.OwnerId == ownerId)
            .OrderBy(d => d.DeviceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeviceEntity>> GetOnlineDevicesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Include(d => d.Owner)
            .Where(d => d.IsOnline)
            .OrderBy(d => d.DeviceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DeviceEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices
            .Include(d => d.Owner)
            .OrderBy(d => d.DeviceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceEntity> CreateAsync(DeviceEntity device, CancellationToken cancellationToken = default)
    {
        _context.Devices.Add(device);
        await _context.SaveChangesAsync(cancellationToken);
        return device;
    }

    public async Task<DeviceEntity> UpdateAsync(DeviceEntity device, CancellationToken cancellationToken = default)
    {
        _context.Devices.Update(device);
        await _context.SaveChangesAsync(cancellationToken);
        return device;
    }

    public async Task<bool> DeleteAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var device = await _context.Devices.FindAsync(new object[] { deviceId }, cancellationToken);
        if (device == null) return false;

        _context.Devices.Remove(device);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExistsAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _context.Devices.AnyAsync(d => d.DeviceId == deviceId, cancellationToken);
    }

    public async Task<bool> UpdateLastSeenAsync(string deviceId, DateTime lastSeen, bool isOnline, CancellationToken cancellationToken = default)
    {
        var device = await _context.Devices.FindAsync(new object[] { deviceId }, cancellationToken);
        if (device == null) return false;

        device.LastSeen = lastSeen;
        device.IsOnline = isOnline;
        if (isOnline)
        {
            device.LastConnectionAt = lastSeen;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices.CountAsync(cancellationToken);
    }

    public async Task<int> GetOnlineCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Devices.CountAsync(d => d.IsOnline, cancellationToken);
    }

    public async Task<IEnumerable<DeviceEntity>> SearchAsync(string searchTerm, string? ownerId = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var query = _context.Devices
            .Include(d => d.Owner)
            .Where(d => d.DeviceName.Contains(searchTerm) || 
                       d.ComputerName.Contains(searchTerm) ||
                       d.DeviceId.Contains(searchTerm));

        if (ownerId != null)
        {
            query = query.Where(d => d.OwnerId == ownerId);
        }

        return await query
            .OrderBy(d => d.DeviceName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
