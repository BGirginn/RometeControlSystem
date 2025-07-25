using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RemoteControl.Core.Data.Entities;

/// <summary>
/// Database entity for registered devices
/// </summary>
[Table("Devices")]
[Index(nameof(DeviceId), IsUnique = true)]
[Index(nameof(OwnerId))]
public class DeviceEntity
{
    [Key]
    [MaxLength(50)]
    public string DeviceId { get; set; } = string.Empty; // Systematic ID like "RC-001234"

    [Required]
    [MaxLength(200)]
    public string DeviceName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string OwnerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)] // IPv6 support
    public string CurrentIP { get; set; } = string.Empty;

    [Required]
    [Range(1, 65535)]
    public int Port { get; set; } = 7777;

    [Required]
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsOnline { get; set; } = false;

    [Required]
    [MaxLength(100)]
    public string ComputerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string OperatingSystem { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ProcessorArchitecture { get; set; } = string.Empty;

    public int ScreenWidth { get; set; }

    public int ScreenHeight { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastConnectionAt { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // Device capabilities
    [Required]
    public bool SupportsH264 { get; set; } = false;

    [Required]
    public int MaxFrameRate { get; set; } = 30;

    [Required]
    public bool SupportsAudio { get; set; } = false;

    [Required]
    public bool SupportsFileTransfer { get; set; } = false;

    // Connection security
    [Required]
    public bool RequireApproval { get; set; } = true;

    [MaxLength(1000)]
    public string? TrustedViewers { get; set; } // JSON array of trusted user IDs

    // Navigation properties
    [ForeignKey(nameof(OwnerId))]
    public virtual UserEntity Owner { get; set; } = null!;
}
