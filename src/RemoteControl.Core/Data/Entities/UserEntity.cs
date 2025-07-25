using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RemoteControl.Core.Data.Entities;

/// <summary>
/// Database entity for user accounts
/// </summary>
[Table("Users")]
[Index(nameof(Username), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
public class UserEntity
{
    [Key]
    [MaxLength(50)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLogin { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime? LastPasswordChange { get; set; }

    [Required]
    public bool EmailVerified { get; set; } = false;

    // Navigation properties
    public virtual ICollection<DeviceEntity> OwnedDevices { get; set; } = new List<DeviceEntity>();
    public virtual ICollection<ConnectionPermissionEntity> GrantedPermissions { get; set; } = new List<ConnectionPermissionEntity>();
    public virtual ICollection<ConnectionPermissionEntity> ReceivedPermissions { get; set; } = new List<ConnectionPermissionEntity>();
    public virtual ICollection<SessionLogEntity> ViewerSessions { get; set; } = new List<SessionLogEntity>();
    public virtual ICollection<SessionLogEntity> AgentSessions { get; set; } = new List<SessionLogEntity>();
    public virtual ICollection<AuditLogEntity> AuditLogs { get; set; } = new List<AuditLogEntity>();
}
