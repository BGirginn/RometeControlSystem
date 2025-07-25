using Microsoft.EntityFrameworkCore;
using RemoteControl.Core.Data.Entities;

namespace RemoteControl.Core.Data;

/// <summary>
/// Entity Framework DbContext for the Remote Control System
/// </summary>
public class RemoteControlDbContext : DbContext
{
    public RemoteControlDbContext(DbContextOptions<RemoteControlDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<DeviceEntity> Devices { get; set; }
    public DbSet<SessionLogEntity> SessionLogs { get; set; }
    public DbSet<ConnectionPermissionEntity> ConnectionPermissions { get; set; }
    public DbSet<AuditLogEntity> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.UserId);
            
            // Configure relationships
            entity.HasMany(e => e.OwnedDevices)
                  .WithOne(e => e.Owner)
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.GrantedPermissions)
                  .WithOne(e => e.Granter)
                  .HasForeignKey(e => e.GranterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.ReceivedPermissions)
                  .WithOne(e => e.Grantee)
                  .HasForeignKey(e => e.GranteeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ViewerSessions)
                  .WithOne(e => e.Viewer)
                  .HasForeignKey(e => e.ViewerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.AgentSessions)
                  .WithOne(e => e.Agent)
                  .HasForeignKey(e => e.AgentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.AuditLogs)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Device entity
        modelBuilder.Entity<DeviceEntity>(entity =>
        {
            entity.HasKey(e => e.DeviceId);
            
            entity.HasOne(e => e.Owner)
                  .WithMany(e => e.OwnedDevices)
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SessionLog entity
        modelBuilder.Entity<SessionLogEntity>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            
            entity.HasOne(e => e.Viewer)
                  .WithMany(e => e.ViewerSessions)
                  .HasForeignKey(e => e.ViewerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Agent)
                  .WithMany(e => e.AgentSessions)
                  .HasForeignKey(e => e.AgentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Device)
                  .WithMany()
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ConnectionPermission entity
        modelBuilder.Entity<ConnectionPermissionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Granter)
                  .WithMany(e => e.GrantedPermissions)
                  .HasForeignKey(e => e.GranterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Grantee)
                  .WithMany(e => e.ReceivedPermissions)
                  .HasForeignKey(e => e.GranteeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Device)
                  .WithMany()
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.User)
                  .WithMany(e => e.AuditLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Device)
                  .WithMany()
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Session)
                  .WithMany()
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed admin user
        modelBuilder.Entity<UserEntity>().HasData(
            new UserEntity
            {
                UserId = "admin-001",
                Username = "admin",
                Email = "admin@remotecontrol.local",
                PasswordHash = "$2a$11$example.hash.for.admin.password", // Should be properly hashed
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        // Seed demo user
        modelBuilder.Entity<UserEntity>().HasData(
            new UserEntity
            {
                UserId = "demo-001",
                Username = "demo",
                Email = "demo@remotecontrol.local",
                PasswordHash = "$2a$11$example.hash.for.demo.password", // Should be properly hashed
                FirstName = "Demo",
                LastName = "User",
                IsActive = true,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
