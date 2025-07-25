using Microsoft.EntityFrameworkCore;
using RemoteControl.Core.Data.Entities;

namespace RemoteControl.Core.Data.Repositories;

/// <summary>
/// Repository implementation for user management
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly RemoteControlDbContext _context;

    public UserRepository(RemoteControlDbContext context)
    {
        _context = context;
    }

    public async Task<UserEntity?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.OwnedDevices)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.OwnedDevices)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserEntity> CreateAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<UserEntity> UpdateAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, string? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.Username == username);
        if (excludeUserId != null)
        {
            query = query.Where(u => u.UserId != excludeUserId);
        }
        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.Email == email);
        if (excludeUserId != null)
        {
            query = query.Where(u => u.UserId != excludeUserId);
        }
        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserEntity>> SearchAsync(string searchTerm, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Username.Contains(searchTerm) || 
                       u.Email.Contains(searchTerm) ||
                       (u.FirstName != null && u.FirstName.Contains(searchTerm)) ||
                       (u.LastName != null && u.LastName.Contains(searchTerm)))
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
