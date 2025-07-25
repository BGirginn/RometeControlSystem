using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RemoteControl.Core.Data.Entities;

/// <summary>
/// Database entity for session history and audit logging
/// </summary>
[Table("SessionLogs")]
[Index(nameof(SessionId), IsUnique = true)]
[Index(nameof(ViewerId))]
[Index(nameof(AgentId))]
[Index(nameof(DeviceId))]
[Index(nameof(StartTime))]
public class SessionLogEntity
{
    [Key]
    [MaxLength(50)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ViewerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string AgentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    public DateTime? EndTime { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Requested"; // Requested, Approved, Active, Ended, Rejected

    [MaxLength(500)]
    public string? EndReason { get; set; }

    // Connection details
    [Required]
    [MaxLength(45)]
    public string ViewerIP { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string AgentIP { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ViewerLocation { get; set; }

    // Session metrics
    public TimeSpan? Duration { get; set; }

    public long? BytesTransferred { get; set; }

    public int? AverageFrameRate { get; set; }

    public int? AverageLatency { get; set; }

    public int? PacketsLost { get; set; }

    [MaxLength(20)]
    public string? VideoCodec { get; set; }

    [MaxLength(20)]
    public string? CompressionLevel { get; set; }

    // Security and approval
    [Required]
    public bool RequiredApproval { get; set; } = true;

    public DateTime? ApprovalTime { get; set; }

    [MaxLength(500)]
    public string? ApprovalReason { get; set; }

    // Additional metadata
    [MaxLength(2000)]
    public string? Metadata { get; set; } // JSON for additional session data

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ViewerId))]
    public virtual UserEntity Viewer { get; set; } = null!;

    [ForeignKey(nameof(AgentId))]
    public virtual UserEntity Agent { get; set; } = null!;

    [ForeignKey(nameof(DeviceId))]
    public virtual DeviceEntity Device { get; set; } = null!;
}
