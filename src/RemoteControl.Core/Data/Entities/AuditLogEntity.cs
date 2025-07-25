using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RemoteControl.Core.Data.Entities;

/// <summary>
/// Database entity for comprehensive audit logging
/// </summary>
[Table("AuditLogs")]
[Index(nameof(UserId))]
[Index(nameof(Action))]
[Index(nameof(Timestamp))]
[Index(nameof(EntityType))]
public class AuditLogEntity
{
    [Key]
    public long Id { get; set; }

    [MaxLength(50)]
    public string? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // Login, Logout, Connect, Disconnect, etc.

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty; // User, Device, Session, Permission

    [MaxLength(50)]
    public string? EntityId { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IPAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Info"; // Debug, Info, Warning, Error, Critical

    [MaxLength(200)]
    public string? Summary { get; set; }

    [MaxLength(2000)]
    public string? Details { get; set; }

    // Change tracking for entities
    [MaxLength(4000)]
    public string? OldValues { get; set; } // JSON

    [MaxLength(4000)]
    public string? NewValues { get; set; } // JSON

    // Session context
    [MaxLength(50)]
    public string? SessionId { get; set; }

    [MaxLength(50)]
    public string? DeviceId { get; set; }

    // Security context
    [Required]
    public bool IsSecurityEvent { get; set; } = false;

    [Required]
    public bool IsSuccessful { get; set; } = true;

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    // Additional metadata
    [MaxLength(2000)]
    public string? Metadata { get; set; } // JSON for additional context

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? User { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public virtual DeviceEntity? Device { get; set; }

    [ForeignKey(nameof(SessionId))]
    public virtual SessionLogEntity? Session { get; set; }
}
