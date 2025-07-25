using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RemoteControl.Core.Data.Entities;

/// <summary>
/// Database entity for managing connection permissions between users and devices
/// </summary>
[Table("ConnectionPermissions")]
[Index(nameof(GranterId), nameof(GranteeId), nameof(DeviceId), IsUnique = true)]
[Index(nameof(GranterId))]
[Index(nameof(GranteeId))]
[Index(nameof(DeviceId))]
public class ConnectionPermissionEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string GranterId { get; set; } = string.Empty; // User who grants permission

    [Required]
    [MaxLength(50)]
    public string GranteeId { get; set; } = string.Empty; // User who receives permission

    [Required]
    [MaxLength(50)]
    public string DeviceId { get; set; } = string.Empty; // Device that can be accessed

    [Required]
    [MaxLength(20)]
    public string PermissionType { get; set; } = "View"; // View, Control, FileTransfer, Audio

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    // Time-based restrictions
    public TimeOnly? AllowedStartTime { get; set; }

    public TimeOnly? AllowedEndTime { get; set; }

    [MaxLength(50)]
    public string? AllowedDays { get; set; } // JSON array: ["Monday", "Tuesday", ...]

    // Connection restrictions
    public int? MaxSessionDuration { get; set; } // Minutes

    public int? MaxConcurrentSessions { get; set; }

    [Required]
    public bool RequireApproval { get; set; } = true;

    [Required]
    public bool AllowFileTransfer { get; set; } = false;

    [Required]
    public bool AllowAudioStreaming { get; set; } = false;

    [Required]
    public bool AllowInputControl { get; set; } = true;

    // Audit fields
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(50)]
    public string? CreatedBy { get; set; }

    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(GranterId))]
    public virtual UserEntity Granter { get; set; } = null!;

    [ForeignKey(nameof(GranteeId))]
    public virtual UserEntity Grantee { get; set; } = null!;

    [ForeignKey(nameof(DeviceId))]
    public virtual DeviceEntity Device { get; set; } = null!;
}
