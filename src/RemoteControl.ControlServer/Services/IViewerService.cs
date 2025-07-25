namespace RemoteControl.ControlServer.Services;

/// <summary>
/// Service for managing viewers
/// </summary>
public interface IViewerService
{
    /// <summary>
    /// Register a viewer connection
    /// </summary>
    Task RegisterViewerAsync(ViewerInfo viewerInfo);

    /// <summary>
    /// Get a viewer by connection ID
    /// </summary>
    Task<ViewerInfo?> GetViewerByConnectionIdAsync(string connectionId);

    /// <summary>
    /// Get viewers for a user
    /// </summary>
    Task<IEnumerable<ViewerInfo>> GetUserViewersAsync(string userId);

    /// <summary>
    /// Set viewer offline
    /// </summary>
    Task SetViewerOfflineAsync(string connectionId);

    /// <summary>
    /// Update viewer heartbeat
    /// </summary>
    Task UpdateViewerHeartbeatAsync(string connectionId);
}

/// <summary>
/// Viewer information
/// </summary>
public class ViewerInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = true;
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
} 